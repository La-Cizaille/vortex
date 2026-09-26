# CLAUDE.md — working agreement for AI-assisted sessions

## Project
Vortex: turn-based FFA (2–5 players) space-ship card game. Unity 6 client (Android + Windows; version policy ADR-0013: latest Update release in development, LTS before any release), rules engine in pure C#.
The user is a security professional: security, optimisation, and clean documentation are non-negotiable. Explain *what* and *why*.

## Language
- Code, identifiers, code comments, commit messages: **English**.
- `docs/`, PR descriptions, and explanations to the user: **French**.

## Source of truth
- Game rules: `docs/RULES.md` - part A base rules, part B effect model (engine behaviour must match it; cite sections like `RULES A6` / `RULES B4` in code).
- Card data: `core/Runtime/Data/{cards,events,technologies}.json` is the **source of truth** (ADR-0008): printed text + ruling per card. `docs/CARDS.md` is generated from it (never hand-edit).
- Decisions: `docs/adr/`. Add an ADR for any structural decision.
- Art direction and lore: `docs/DIRECTION_ARTISTIQUE.md` (ADR-0020, ARB-93) is the reference for look, tone and writing; every asset and interface text follows it. Rule texts stay neutral: humour never replaces game information.
- Art: `docs/ASSETS.md` lists every visual to create with its format (scale, orientation, budgets, names); pipeline in ADR-0016.
- Client interface: `docs/INTERFACE.md` (screen layout, gestures, menus, decisions on the table; designer rulings ARB-59 to ARB-75 and ARB-80 to ARB-82). The interface holds no rules: legal targets come from the session's legal commands, previews are computed by the engine (ADR-0015).
- Rulings not covered by RULES.md: **ask the user**, do not guess; then record the answer in the card's `ruling` (card-specific) or RULES.md part A/B (generic), **and** log it in `docs/ARBITRAGES.md` (new `ARB-xx` entry: question, decision, reason, where applied; entries are never rewritten). Unanswered questions live in RULES.md "Questions ouvertes".
- Rule changes under study: generic `config.json` options whose default keeps the current rule (ADR-0011); tried through variant files, never by editing content.

## Hard rules
- Engine is card-agnostic (ADR-0007): base rules and engine code never name a card; cards act only through RULES B2 interception points and B3 elementary actions. A missing hook is added generically, never as a card exception. Rulings never name another card.
- `core/` : C# 9 / netstandard2.1, no `UnityEngine`, no I/O, no statics with mutable state, no runtime reflection, no `System.Random` (use `Pcg32`).
- Newtonsoft: `TypeNameHandling.None` only; polymorphism via whitelisted discriminators.
- Illegal commands return typed errors and leave state untouched; every rule/card gets tests (use `ScriptedDice`).
- Unity code never references assets directly: use theme catalogs; missing art → generated placeholder.
- Unity presentation (ADR-0014): the client sees only `GameView` through `IGameSession`; engine events are played one by one by `EventPlayer`, and what they look like lives in `FeedbackProfile` assets, never in code. Interface texts come from the `TextTable` asset by `TextKeys` key, never as literals in views (ADR-0015). Components showing an engine `*View` are named `*Display`. Text that does not come from the content files (player names, log lines) is shown with rich text off. One `ScriptableObject`/`MonoBehaviour` class per file, named after it.
- Never commit secrets/keystores. Pin dependency versions. Update `docs/SECURITY.md` when adding an input surface or dependency.
- Security doctrine (`docs/SECURITY.md` §0): **no shortcut on the shipped product** (builds, server, network, saves, runtime dependencies); **pragmatic shortcuts allowed for dev tooling** (editor tools, MCP, simulator, scripts) if nothing reaches a build, no secret is exposed, and the shortcut is documented.
- Work on feature branches; `main` only via PR with green CI. PR template: Quoi / Pourquoi / Risques / Vérification.

## Commands
- Tests: `dotnet test dotnet/Vortex.sln`
- Unity tests (batch mode, no editor window): `powershell -ExecutionPolicy Bypass -File tools/Test-Unity.ps1 [-Platform PlayMode]`
- Unity base assets (batch mode; never overwrites, delete an asset to rebuild it): `powershell -ExecutionPolicy Bypass -File tools/Update-UnityAssets.ps1`
- Unity capture to PNG (batch mode): `powershell -ExecutionPolicy Bypass -File tools/Capture-Unity.ps1 -Scene Game|Gallery -Out <file.png> [-Round N]`. Batch scripts refuse to run while the editor has the project open.
- Unity MCP (dev only): project `.mcp.json`, pinned server `mcpforunityserver==10.2.0` over stdio; the editor bridge listens on 127.0.0.1:6400. Release builds refuse dev-only packages (`ReleaseBuildGuard`).
- Blender MCP (dev only): `.mcp.json`, pinned `mcp-for-blender==2.0.4`, telemetry off, safe mode on unless the machine sets `BLENDER_MCP_SAFE_MODE=0` (ARB-83); the addon listens on 127.0.0.1:9876. Model export: `blender --background --disable-autoexec <art-src/...blend> --python tools/blender/export_unity.py -- <unity/Assets/_Vortex/Art/...fbx> [--budget N]`; models are built by script (`tools/blender/build_card.py`, `build_ship_carcasse.py`, `build_cockpit.py`, `build_die.py`, `build_background.py`, `build_market.py`; front-view parts and texture painting shared in `front_view.py`; ADR-0016). Blender 5.2 LTS (ARB-78).
- Fonts (dev only): OFL files pinned in `art-src/fonts/` (sources, SHA-256 in `art-src/LICENCES.md`); a variable font's weight is frozen by `python tools/fonts/instance_font.py <variable.ttf> <wght> <out.ttf>`; the asset tool builds static TextMeshPro atlases in `Theme/Fonts/` (delete one to rebuild it).
- Format check: `dotnet format dotnet/Vortex.sln --verify-no-changes`
- Balance report: `dotnet run -c Release --project dotnet/Vortex.Simulator -- run --games 1000 --seed 1 --out docs/balance/<date>-<topic>.md`
- Balance grid: `dotnet run -c Release --project dotnet/Vortex.Simulator -- grid --grid docs/balance/grids/<file>.json --games 1000 --seed 1 --out docs/balance/<date>-<topic>.md` (5 players by default, the standard table)
- Balance comparison: `dotnet run -c Release --project dotnet/Vortex.Simulator -- compare --games 2000 --seed 1 --variant docs/balance/variants/<file>.json [--variant …] --out docs/balance/<date>-<topic>.md` (reports are generated, never hand-edited; bots never name a card, ADR-0010; plan and targets in `docs/balance/README.md`)
- Content: `dotnet run --project dotnet/Vortex.ContentTool -- validate core/Runtime/Data` (also `format <dir> [--check]`, `docs core/Runtime/Data docs [--check]`)
