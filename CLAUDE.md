# CLAUDE.md — working agreement for AI-assisted sessions

## Project
Vortex: turn-based FFA (2–5 players) space-ship card game. Unity 6.3 LTS client (Android + Windows), rules engine in pure C#.
The user is a security professional: security, optimisation, and clean documentation are non-negotiable. Explain *what* and *why*.

## Language
- Code, identifiers, code comments, commit messages: **English**.
- `docs/`, PR descriptions, and explanations to the user: **French**.

## Source of truth
- Game rules: `docs/RULES.md` - part A base rules, part B effect model (engine behaviour must match it; cite sections like `RULES A6` / `RULES B4` in code).
- Card data: `core/Runtime/Data/{cards,events,technologies}.json` is the **source of truth** (ADR-0008): printed text + ruling per card. `docs/CARDS.md` is generated from it (never hand-edit).
- Decisions: `docs/adr/`. Add an ADR for any structural decision.
- Rulings not covered by RULES.md: **ask the user**, do not guess; then record the answer in the card's `ruling` (card-specific) or RULES.md part A/B (generic).

## Hard rules
- Engine is card-agnostic (ADR-0007): base rules and engine code never name a card; cards act only through RULES B2 interception points and B3 elementary actions. A missing hook is added generically, never as a card exception. Rulings never name another card.
- `core/` : C# 9 / netstandard2.1, no `UnityEngine`, no I/O, no statics with mutable state, no runtime reflection, no `System.Random` (use `Pcg32`).
- Newtonsoft: `TypeNameHandling.None` only; polymorphism via whitelisted discriminators.
- Illegal commands return typed errors and leave state untouched; every rule/card gets tests (use `ScriptedDice`).
- Unity code never references assets directly: use theme catalogs; missing art → generated placeholder.
- Never commit secrets/keystores. Pin dependency versions. Update `docs/SECURITY.md` when adding an input surface or dependency.
- Work on feature branches; `main` only via PR with green CI. PR template: Quoi / Pourquoi / Risques / Vérification.

## Commands
- Tests: `dotnet test dotnet/Vortex.sln`
- Format check: `dotnet format dotnet/Vortex.sln --verify-no-changes`
- Balance: `dotnet run -c Release --project dotnet/Vortex.Simulator -- --games 1000 --seed 1 --out docs/balance/<date>-<topic>.md` (reports are generated, never hand-edited; bots never name a card, ADR-0010)
- Content: `dotnet run --project dotnet/Vortex.ContentTool -- validate core/Runtime/Data` (also `format <dir> [--check]`, `docs core/Runtime/Data docs [--check]`)
