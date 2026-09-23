# CLAUDE.md — working agreement for AI-assisted sessions

## Project
Vortex: turn-based FFA (2–5 players) space-ship card game. Unity 6.3 LTS client (Android + Windows), rules engine in pure C#.
The user is a security professional: security, optimisation, and clean documentation are non-negotiable. Explain *what* and *why*.

## Language
- Code, identifiers, code comments, commit messages: **English**.
- `docs/`, PR descriptions, and explanations to the user: **French**.

## Source of truth
- Game rules: `docs/RULES.md` (engine behaviour must match it; cite sections like `RULES.md §6` in code).
- Card data: `design/Vortex.xlsx` → generated JSON in `core/Runtime/Data/` (never hand-edit generated JSON).
- Decisions: `docs/adr/`. Add an ADR for any structural decision.
- Rulings not covered by RULES.md: **ask the user**, do not guess; then record the answer in RULES.md §10/§11.

## Hard rules
- `core/` : C# 9 / netstandard2.1, no `UnityEngine`, no I/O, no statics with mutable state, no runtime reflection, no `System.Random` (use `Pcg32`).
- Newtonsoft: `TypeNameHandling.None` only; polymorphism via whitelisted discriminators.
- Illegal commands return typed errors and leave state untouched; every rule/card gets tests (use `ScriptedDice`).
- Unity code never references assets directly: use theme catalogs; missing art → generated placeholder.
- Never commit secrets/keystores. Pin dependency versions. Update `docs/SECURITY.md` when adding an input surface or dependency.
- Work on feature branches; `main` only via PR with green CI. PR template: Quoi / Pourquoi / Risques / Vérification.

## Commands
- Tests: `dotnet test dotnet/Vortex.sln`
- Format check: `dotnet format dotnet/Vortex.sln --verify-no-changes`
- Import cards: `dotnet run --project dotnet/Vortex.CardImporter -- design/Vortex.xlsx core/Runtime/Data`
