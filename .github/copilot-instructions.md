# EZPos Desktop (WPF) Repository Instructions

This repository contains the main EZPos desktop application built with C# and WPF.

## Scope
- Desktop app source code and UI: `App.xaml`, `MainWindow.xaml`, `*.cs`
- Configuration and packaging: `Config/`, `InnoSetup-EZPos.iss`, `publish/`
- Project docs and architecture notes in `docs/`

## Development Guidelines
- Preserve existing WPF patterns and naming conventions.
- Keep UI behavior and business logic separated when possible.
- Prefer small, targeted changes over broad refactors.
- Avoid modifying generated build artifacts in `bin/` and `obj/` unless explicitly requested.

## Build & Run
- Build with Visual Studio or `dotnet build EZPos.sln`
- Run from Visual Studio or `dotnet run --project EZPos.csproj`

## Copilot Working Rule
Before starting implementation, confirm task scope is for this repository and not `EZPos-Web` or `CrossxPos`.
