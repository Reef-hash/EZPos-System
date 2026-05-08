# EZPos System

**WPF .NET 6 Point of Sale System** — single-store, Windows desktop, SQLite.

> v1.1.x | net6.0-windows7.0 | Catalysm Inc

---

## Documentation

All project knowledge lives in [`docs/`](docs/README.md).

| Folder | Purpose |
|---|---|
| [`docs/architecture/`](docs/architecture/) | Layered architecture, dependency rules, data storage |
| [`docs/features/`](docs/features/) | Feature behavior — sales, inventory, reporting |
| [`docs/systems/`](docs/systems/) | End-to-end system flows — licensing, auto-update |
| [`docs/standards/`](docs/standards/) | Project structure rules, UI guidelines, AI rules |
| [`docs/planning/`](docs/planning/) | Future features and SaaS roadmap |
| [`docs/tracking/`](docs/tracking/) | Feature status and TODO list |

---

## Quick Reference

**Stack:** WPF + C# + SQLite + FontAwesome.Sharp + ClosedXML + PdfSharpCore

**Data location:** `C:\ProgramData\EZPos\` — database, config, trial file, backups

**Release:** Bump `<Version>` in `EZPos.csproj` → push to `main` → CI builds installer + updates manifest automatically

**Build:**
```bash
dotnet build --configuration Release
dotnet publish -c Release -o publish
ISCC.exe InnoSetup-EZPos.iss
```

---

## Legacy Docs

- `ARCHITECTURE.md` — early architecture sketches (superseded by `docs/architecture/`)
- `GUIDE-MY.md` — usage guide in Bahasa Malaysia (early version, may be outdated)
- `README.md` — this file