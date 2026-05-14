# Near-Term Planned Features — EZPos

> Items planned for the next release cycle. Move to `docs/tracking/FEATURE_STATUS.md` when implementation starts.

---

## Priority 1 — Operational Impact

### Low Stock Warning During Sale
**What:** After checkout, if any sold product drops to or below its `ReorderLevel`, show a non-blocking notice on the receipt screen.
**Where to implement:** `ReceiptDialog.xaml.cs` — after `_result.Lines` is processed, check each product's current stock vs reorder level.
**Business value:** Prevents stockouts during busy periods without interrupting the cashier flow.

### Unsaved Cart Protection
**What:** If the cashier navigates away from Sales page with items in the cart, show a confirmation dialog: "You have X items in your cart. Leave anyway?"
**Where to implement:** `SalesPage.xaml.cs` — handle `UserControl.Unloaded` or intercept navigation in `NavigationService`.
**Business value:** Prevents accidental loss of in-progress sales.

---

## Priority 2 — Data Integrity

### Opening Balance Already Implemented ✅
`StockMovement` with `Reason = OPENING_BALANCE` is now written when a product is registered with stock > 0. No further work needed.

---

## Priority 3 — UX Improvement

### Barcode Scan Auto-fills ProductDialog (Add Mode) — Already Implemented ✅
`SalesKeyboardInputService` is now wired to `ProductDialog` in Add mode. Scanning auto-fills `BarcodeBox`. No further work needed.

### Auto-Print Receipt — Already Implemented ✅
`AutoPrint` toggle in Settings → Receipt Printer section. `ReceiptDialog_Loaded` fires print if enabled. No further work needed.

---

## Priority 4 — Payment Integration

### QR Payment Auto-Detection (DuitNow / Billplz / TnG)
**What:** When customer pays via QR, the POS generates a payment request, displays the QR code, and automatically confirms checkout when the customer completes payment — no cashier button press needed.

**How it works:**
1. Cashier selects QR payment method in `PaymentDialog`
2. POS calls payment gateway API → receives a `payment_id` + QR code image/string
3. POS displays QR in `PaymentDialog` (replace static QR image with live generated one)
4. POS polls gateway every 2–3 seconds: `GET /payments/{id}/status`
5. When status = `PAID` → auto-close dialog → proceed to checkout

**Recommended gateway (Malaysia):** [Billplz](https://www.billplz.com/api) — DuitNow QR supported, clean REST API, no physical hardware needed. Alternative: [iPay88](https://ipay88.com.my/), [Curlec](https://curlec.com/)

**Where to implement:**
- New service: `src/Business/Services/PaymentGatewayService.cs`
- New interface: `src/Business/Interfaces/IPaymentGatewayService.cs`
- Modified: `src/UI/Dialogs/PaymentDialog.xaml` + `.xaml.cs` — add QR display panel + polling loop
- Config keys to add: `Gateway:Provider`, `Gateway:ApiKey`, `Gateway:CollectionId`

**Dependencies:** Internet connection + merchant account with chosen gateway

**Business value:** Hands-free checkout for QR payments — cashier doesn't need to manually confirm. Critical for busy single-operator setups.

---

### Card Payment Auto-Detection (EDC Terminal Integration)
**What:** When customer pays by card (chip/tap/swipe), the POS sends the amount to the physical card terminal, and automatically confirms checkout when the terminal returns an approval code.

**How it works:**
1. Cashier selects Card payment in `PaymentDialog`
2. POS sends payment request to EDC terminal via SDK (USB/serial/LAN)
3. Customer taps/inserts/swipes card on terminal
4. Terminal returns approval code → POS auto-closes dialog → proceeds to checkout

**Recommended terminal (Malaysia):** PAX terminals (A920, A80) — most common in Malaysia, PAX has a `.NET` SDK (`PaxStoreSdk`). Alternatives: Ingenico, Verifone (older, require serial port integration).

**Where to implement:**
- New service: `src/Business/Services/EdcTerminalService.cs`
- New interface: `src/Business/Interfaces/IEdcTerminalService.cs`
- Modified: `src/UI/Dialogs/PaymentDialog.xaml` + `.xaml.cs` — add "Waiting for card..." status panel
- Config keys to add: `EDC:TerminalPort`, `EDC:TerminalIp`, `EDC:Provider`

**Dependencies:** Physical EDC terminal + manufacturer SDK + LAN/USB/serial connection

**Business value:** Eliminates manual approval entry. Required for unattended or self-checkout setups.

---

### Implementation Order
If implementing both, do QR first — no hardware dependency, faster to test, higher ROI for Malaysian market (DuitNow penetration is very high).

---

## Implementation Notes

When picking up any item above:
1. Check `docs/tracking/TODO.md` for current status
2. Follow the feature checklist in `docs/standards/PROJECT_STRUCTURE.md`
3. Document behavior in the relevant `docs/features/` file
4. Update `docs/tracking/FEATURE_STATUS.md` when done
