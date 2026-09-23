# Changelog

Format : [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/).

## [Non publié]
### Ajouté
- M0 : solution .NET 10 (`Vortex.Core` en netstandard2.1/C# 9, `Vortex.CardImporter`, `Vortex.Core.Tests`), avec warnings bloquants, analyseurs, versions centralisées et fichiers de verrouillage NuGet, et SDK épinglé (`global.json`).
- M0 : modèle de contenu (`core/Runtime/Content`) et chargement JSON durci ; `gamedata.json` généré depuis `Vortex.xlsx` (54 cartes ATK, 50 DEF, 15 événements, 4 technologies).
- M0 : importeur xlsx sans dépendance, protégé contre XXE, bombes zip et traversée de chemin.
- M0 : 40 tests (contenu, durcissement du chargement, importeur, hygiène des sources).
- M0 : CI GitHub Actions (build, tests et couverture, format, audit des vulnérabilités, cohérence des données générées, gitleaks) et CodeQL (C# + workflows) ; actions épinglées par SHA ; modèle de PR.
- M0 : structure du dépôt, règles consolidées (`docs/RULES.md`), architecture, modèle de sécurité, guide de contribution, ADR 0001 à 0006, configuration Git/LFS/EditorConfig, Dependabot.
