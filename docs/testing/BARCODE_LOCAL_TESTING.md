# Barcode Management Module — Local Windows Testing Handoff

> Written for whoever (human or agent) picks this up next on a real Windows machine.
> Phase 1 (MVP) and Phase 2 (Production Ready) of the Barcode Management Module were both
> implemented in a **Linux sandbox with no `dotnet` SDK and no Windows** available. Every line
> was written by hand against the codebase's existing conventions and general knowledge of the
> libraries involved (ZXing.Net, PdfSharpCore, WPF printing) — **none of it has been compiled,
> let alone run.** Treat this as a first draft that needs a real build+test pass, not as
> verified-working code.

Related docs: [barcode-module.md](../features/barcodes/barcode-module.md) (full design/roadmap),
[FEATURE_STATUS.md](../tracking/FEATURE_STATUS.md), [TODO.md](../tracking/TODO.md).

---

## 1. Build it first

```powershell
git checkout claude/barcode-beta-adjust-36g299   # or whatever branch/PR this landed on
dotnet restore
dotnet build
```

Fix compile errors before doing anything else. The sections below list the specific spots most
likely to break, in rough order of risk.

### 1.1 NuGet packages that need to resolve

`EZPos.csproj` now references:

- `ZXing.Net` (0.16.9)
- `ZXing.Net.Bindings.Windows.Compatibility` (0.16.12) — added because plain `ZXing.Net` does not
  ship a `System.Drawing.Bitmap`-based `BarcodeWriter` for non-`net4x` target frameworks anymore;
  the compatibility package is what supplies `ZXing.Windows.Compatibility.BarcodeWriter`. **If
  restore fails or the version doesn't exist, search NuGet for the current version and update
  both the `<PackageReference>` in `EZPos.csproj` and the `using ZXing.Windows.Compatibility;` in
  `src/Business/Services/BarcodeService.cs` accordingly.**

### 1.2 `BarcodeService.cs` — ZXing API shape

`src/Business/Services/BarcodeService.cs` assumes:

```csharp
var writer = new BarcodeWriter
{
    Format = ZXing.BarcodeFormat.CODE_128, // etc.
    Options = new ZXing.Common.EncodingOptions { Width = ..., Height = ..., Margin = 4, PureBarcode = false }
};
using var bitmap = writer.Write(data); // System.Drawing.Bitmap
```

If the compatibility package's `BarcodeWriter` has a different shape (e.g. requires
`BarcodeWriterPixelData` or a different namespace), fix `GenerateImageBytes()` — everything else
(`GenerateImage()`, `LabelPrintService`) calls through that one method, so a fix there should
cascade cleanly.

### 1.3 FontAwesome icon names

`FontAwesome.Sharp` 6.3.0 is already a dependency. New/changed XAML uses these `Icon="..."` values
that were **not** previously used elsewhere in this codebase, so they're unverified:

| File | Icon | Risk |
|---|---|---|
| `src/UI/Dialogs/ProductDialog.xaml` | `WandMagicSparkles` | Medium — FA6 solid icon, name should match, but double-check casing |
| `src/UI/Pages/BarcodesPage.xaml` | `FilePdf` | Low — very standard FA icon |

If either name doesn't exist in the installed `FontAwesome.Sharp` version, IntelliSense/build
will point at the exact line — swap in any other confirmed-working icon from the same file's
neighbours (e.g. `Barcode`, `Print`, `MagnifyingGlass`, `Xmark`, `TrashAlt`, `Plus`, `FloppyDisk`
are all already used successfully elsewhere in this repo).

### 1.4 PdfSharpCore API in `LabelPrintService.ExportToPdf()`

This is the riskiest piece of new code. It was modelled on the *existing* working PDF export in
`src/UI/Pages/ReportsPage.xaml.cs` (`ExportToPdf` method around line 606) for `PdfDocument`,
`XGraphics.FromPdfPage`, `XFont`, `XBrushes`, `XRect`, `DrawString(text, font, brush, rect, format)`
— that part should be solid since it mirrors code that's presumably already shipping.

**Unverified additions** (not present anywhere else in this codebase):

- `page.Width = <double points>` / `page.Height = <double points>` — setting a **custom** page
  size via direct property assignment (relying on `XUnit`'s implicit `double` conversion,
  treating the value as points) instead of `page.Size = PageSize.A4`. If this doesn't compile or
  silently produces a wrong-size PDF, look for the correct way to set a custom `PdfPage` size in
  the installed PdfSharpCore version (possibly `XUnit.FromPoint(value)`, or a `PageSize.Undefined`
  + explicit `Width`/`Height` combo).
- `XImage.FromStream(() => stream)` — drawing the barcode PNG bytes into the PDF. PdfSharpCore
  historically wants a `Func<Stream>` factory (not a raw `Stream`) because it may need to re-read
  the image data. If this overload doesn't exist, try `XImage.FromStream(stream)` instead, or
  check whether the installed version wants the stream kept alive for the lifetime of the
  `PdfDocument` (currently the `using var stream = new MemoryStream(bytes);` is scoped to one
  `DrawLabelPdf()` call — if PdfSharpCore defers reading the image until `document.Save()`, the
  stream will already be disposed and this needs restructuring, e.g. don't dispose until after
  `document.Save()`, or use `XImage.FromStream(() => new MemoryStream(bytes))` so a **fresh**
  stream is created each time it's invoked instead of reusing/disposing one).

Test this specifically: generate a few labels, export to PDF, open the PDF, and confirm (a) the
page size actually matches the label dimensions and (b) the barcode image actually renders (not a
blank box).

### 1.5 `System.Printing` / `System.Drawing.Printing` availability

`LabelPrintService.cs` uses `System.Printing.LocalPrintServer`/`PrintQueue` and
`System.Drawing.Printing.PrinterSettings.InstalledPrinters`. These should already be available
transitively (WPF's `Microsoft.NET.Sdk.WindowsDesktop` framework reference covers
`System.Printing`; `System.Drawing.Printing` comes from `System.Drawing.Common`, which
`ZXing.Net.Bindings.Windows.Compatibility` should pull in). If either fails to resolve, add an
explicit `<PackageReference Include="System.Drawing.Common" .../>` or a `<FrameworkReference>` as
needed.

---

## 2. Manual test checklist

Run through these in order — later ones assume earlier ones work. Use the Dashboard →
**Barcodes** page (has a "BETA" sidebar tag) unless noted otherwise.

### 2.1 Barcode generation basics
- [ ] Open Products → Add Product → click the wand icon next to Barcode → a value like
      `EZP000001` appears
- [ ] Save the product, confirm it persists (re-open Edit — barcode unchanged)
- [ ] Open Products → select a product → "Print Label..." → QuickPrintDialog opens with the
      product's name/barcode shown, a Template/Format/Printer dropdown populated, and Quantity
      defaulting to 1

### 2.2 BarcodesPage — bulk flow
- [ ] Navigate to Barcodes — product list loads (search box + category filter both work)
- [ ] Check a few products → they appear in the Quantity list on the right, with a live barcode
      preview showing the first checked product
- [ ] "Select All" and "Select by Category" both populate the Quantity list correctly
- [ ] Change quantity for one item in the list, remove another via the ✕ button — the print job
      list updates correctly and the preview stays in sync
- [ ] Change Format dropdown (Code128 → EAN13 etc.) — preview barcode image changes
- [ ] Click **Preview** — a separate window opens showing the actual print layout via
      `DocumentViewer` (page count, label positions, fields all matching the selected template's
      toggles)
- [ ] Click **Print Labels** with a real (or virtual/PDF) printer selected — confirm it actually
      prints/generates something sane, and a "Sent N label(s) to print" message appears
- [ ] Click **Export PDF** — SaveFileDialog appears, resulting PDF opens correctly (see §1.4 above
      for what to scrutinize)
- [ ] Expand "Print History" — the print/export you just did appears at the top with correct
      product name, quantity, template, and timestamp

### 2.3 Template editor
- [ ] Click "Edit Templates..." next to the Template dropdown — dialog opens with the 4 seeded
      templates (Standard 40x30, Shelf Label 100x50, Price Tag 58x40, A4 Sheet)
- [ ] Edit a template's fields (name, dimensions, toggles, font sizes) → Save → confirm it's
      actually persisted (`%ProgramData%\EZPos\label-templates.json` — inspect the file directly)
- [ ] Click "New Template" → edit its fields → Save → close dialog → confirm the new template now
      appears in BarcodesPage's Template dropdown
- [ ] Try deleting a template when only one remains — should be blocked (`Templates.Count > 1`
      guard); delete a non-last template — should succeed and disappear from both dialogs
- [ ] Check "Set as default template" on one template, Save — confirm only one template has
      `IsDefault: true` in the JSON file afterward (the Save logic un-sets all others)

### 2.4 Damaged label replacement (scanner flow)
- [ ] On BarcodesPage, with a barcode scanner (or simulated fast keystrokes — see
      `SalesKeyboardInputService`'s ~150ms threshold used elsewhere in this codebase), scan a
      barcode that belongs to a registered product → QuickPrintDialog should open automatically
      for that product
- [ ] Scan a barcode that does **not** match any product → a "Barcode not registered: ..." message
      should appear instead

### 2.5 Stock-receive hook
- [ ] Go to Stock page → select a product → "Stock In" → complete the adjustment → a Yes/No prompt
      "Stock received for '...'. Print label(s) now?" should appear
- [ ] Clicking Yes opens QuickPrintDialog for that product; clicking No does nothing further
- [ ] Confirm "Stock Out" does **not** trigger this prompt (it's Stock In only)

### 2.6 EAN-13 warning
- [ ] Pick a product whose barcode is not a valid 13-digit EAN-13 (e.g. the default `EZPxxxxxx`
      internal codes), set Format to EAN13 in BarcodesPage or QuickPrintDialog, and print/export —
      a "Warning: not a valid 13-digit EAN-13 barcode..." message should appear (non-blocking —
      the print/export should still proceed)
- [ ] Confirm a syntactically valid EAN-13 (13 digits, correct check digit — you can construct one
      or use `BarcodeService.ValidateEan13()` in a scratch test) does **not** trigger the warning

---

## 3. Known gaps (intentionally out of scope)

These were **not** implemented — don't be surprised if they're missing, they're tracked
separately in [TODO.md](../tracking/TODO.md) Backlog / [barcode-module.md](../features/barcodes/barcode-module.md) Phase 3:

- QR code format is wired end-to-end (generation, printing, PDF) but has no real use case yet —
  there's no web product catalogue for a QR to point to
- Inventory count mode, PO barcode receiving, price-change reprint triggers, barcode CSV
  import/export — all Phase 3, not started
- No automated/unit tests were added for any of this — all verification is manual per §2 above

---

## 4. If you're an agent picking this up

1. Read this file fully before touching code.
2. Run `dotnet build` first — don't assume anything compiles.
3. Work through §1's risk list in order; each fix is usually isolated to one file.
4. Once it builds, run the app and work through §2's checklist top to bottom, fixing issues as you
   find them (small, targeted fixes — don't refactor working code you're not testing).
5. When Phase 2 is confirmed working end-to-end, update `FEATURE_STATUS.md`'s Barcode Management
   Module status line from "CORE COMPLETE (BETA — needs a Windows build/test pass...)" to
   something reflecting verified status, and note what (if anything) you had to fix here or in
   `TODO.md`.
