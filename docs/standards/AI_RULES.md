# AI Development Rules — EZPos

> Rules for AI assistants (GitHub Copilot, etc.) working on this codebase.
> Read this before generating or modifying any code.

---

## Project Identity

- **App:** EZPos Point of Sale System
- **Stack:** WPF + .NET 6 (`net6.0-windows7.0`), C#, SQLite
- **Architecture:** 4-layer (View → Service → Repository → DB) with PosStateStore
- **Branding:** Catalysm Inc / Zarif El-Mansour / WhatsApp 019-5778954
- **Current version:** see `<Version>` in `EZPos.csproj`

---

## Architecture Rules (ENFORCE STRICTLY)

1. **Pages and Dialogs must NEVER call repositories directly.** Always go through a Service.
2. **Services must NEVER import `System.Windows` or any WPF namespace.**
3. **Repositories must NEVER contain business logic** — only SQL + row mapping.
4. **`PosStateStore` must NEVER be mutated directly from UI code-behind.** Always go through a Service which updates the store.
5. **New features follow the checklist in `docs/standards/PROJECT_STRUCTURE.md`.**

---

## Licensing Architecture (DO NOT BREAK)

- `ILicenseService` is the contract. `TrialLicenseService` is the active implementation.
- `App.xaml.cs` does the license check on startup — do not move or bypass it.
- To add real licensing: replace the one line `new TrialLicenseService()` with the real implementation. Do not restructure the startup flow.
- `TrialExpiredWindow` shows Catalysm Inc branding — do not alter without explicit instruction.

---

## Auto-Update (DO NOT BREAK)

- `UpdaterService` runs on `MainWindow_Loaded` silently. Do not block the UI thread.
- Version is read from assembly at runtime via `AssemblyInformationalVersionAttribute`. Never hardcode version strings in UI.
- `App:UpdateManifestUrl` in `config.ini` controls the manifest endpoint. It must be seeded by the installer.

---

## Barcode Scanner (UNDERSTAND BEFORE TOUCHING)

- `SalesKeyboardInputService` distinguishes HID scanner from human typing using timing thresholds (60ms inter-key, 150ms total).
- It is wired via `PreviewTextInput` / `PreviewKeyDown` on the **host window**, not individual controls.
- When wiring scanner to a new page/dialog: follow the same `AttachWindowInputHandlers` / `DetachWindowInputHandlers` pattern used in `SalesPage.xaml.cs`.

---

## Code Style

- Follow existing patterns — don't introduce new patterns without necessity.
- No docstrings on unchanged methods.
- No `var` for types that aren't obvious from the right-hand side.
- Null safety: use `?.`, `??`, and nullable reference types as per existing code.
- Never add `try/catch` that swallows exceptions silently without at least a `Debug.WriteLine`.

---

## What NOT to do

- Do not add features, refactor, or "improve" things that weren't asked for.
- Do not change `TrialExpiredWindow` branding without explicit instruction.
- Do not change the startup sequence in `App.xaml.cs` without explicit instruction.
- Do not hardcode version strings — always read from assembly.
- Do not use `Set-Content` for writing files with Unicode — use `[System.IO.File]::WriteAllText` with explicit UTF-8 encoding.

---

## Documentation Location

When adding a new system or feature:

| What | Where |
|---|---|
| Feature behavior & rules | `docs/features/{module}/` |
| System-level flow | `docs/systems/` |
| Progress update | `docs/tracking/FEATURE_STATUS.md` |
| Near-term plan item | `docs/tracking/TODO.md` |
| Long-term plan | `docs/planning/` |
