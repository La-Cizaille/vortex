# Changelog

Format : [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/).

## [Non publié]
### Ajouté
- M0 : solution .NET 10 (`Vortex.Core` en netstandard2.1/C# 9, `Vortex.ContentTool`, `Vortex.Core.Tests`), avec warnings bloquants, analyseurs, versions centralisées et fichiers de verrouillage NuGet, et SDK épinglé (`global.json`).
- M0 : **le JSON devient la source de vérité** des cartes (ADR-0008) : `cards.json`, `events.json`, `technologies.json` (54 cartes ATK, 50 DEF, 15 événements, 4 technologies), migrés depuis `Vortex.xlsx`, puis classeur retiré. Chaque carte porte son texte et son arbitrage. JSON Schemas pour l'édition dans VS Code.
- M0 : `Vortex.ContentTool` (validate, format, docs) et `docs/CARDS.md` généré.
- M0 : RULES.md restructuré : partie A (règles de base, sans aucune carte), partie B (modèle d'effets : points d'interception, actions élémentaires, empilement, durées, choix, ordre de résolution). ADR-0007 (moteur indépendant des cartes) et ADR-0008.
- M0 : chargement JSON durci (pas de métadonnées de type, membres inconnus rejetés, profondeur et taille bornées).
- M0 : tests du contenu, du durcissement du chargement et de l'outil de contenu ; garde-fous « aucun id de carte dans le moteur » et « aucun arbitrage ne cite une autre carte » ; hygiène des sources (caractères invisibles et bidi).
- M0 : CI GitHub Actions (build, tests et couverture, format, audit des vulnérabilités, cohérence des données générées, gitleaks) et CodeQL (C# + workflows) ; actions épinglées par SHA ; modèle de PR.
- M0 : structure du dépôt, règles consolidées (`docs/RULES.md`), architecture, modèle de sécurité, guide de contribution, ADR 0001 à 0006, configuration Git/LFS/EditorConfig, Dependabot.
