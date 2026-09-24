# Changelog

Format : [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/).

## [Non publié]
### Ajouté
- Passe d'équilibrage, étape 1 (ADR-0011) : **variantes** de contenu (`docs/balance/variants/*.json`, correctifs JSON revalidés par le chargeur du jeu, jamais écrits dans le contenu) et commande `compare` du simulateur. Elle joue la référence et les variantes sur les mêmes parties et donne chaque écart avec sa marge d'erreur à 95 %.
- Simulateur : commandes `run` et `compare`, quatre niveaux de bot (`--bot random|naive|normal|strong`, `--skill hero,others`), commande complète et empreinte SHA-256 du contenu simulé en tête de rapport. Nouvelles mesures : écart maximal de position, première élimination, victoire du meneur à mi-partie, attaques sur le meneur ou le plus faible, taux de victoire par couleur dominante, combos par partie, effet de chaque événement sur le meneur.
- Options de règles ⚙ dans `config.json` (format de contenu v4) : `roundStartRotation` (premier joueur de chaque manche fixe, tournant dans le sens horaire ou anti-horaire, RULES A4.4) et `technologiesToWin` (technologies nécessaires à l'Élection galactique, RULES A9). Les valeurs par défaut gardent les règles actuelles.
- `docs/ARBITRAGES.md` : **journal des arbitrages** de game design (ARB-01 à ARB-49), avec la question, la décision, sa raison et l'endroit où elle est appliquée. Plan d'équilibrage et objectifs chiffrés dans `docs/balance/README.md`.
- Tests : options de règles, variantes (fusion, refus, fichiers du dépôt), options du simulateur, statistiques et rapport de comparaison.
- M3 : bots génériques (`RandomBot`, `HeuristicBot`) qui simulent chaque coup sans connaître les cartes ni voir l'information cachée (ADR-0010). Simulateur d'équilibrage `Vortex.Simulator`, parallèle et reproductible, avec un rapport en français : durée, avantage de position, écart de niveau, combats, cartes (écart neutralisant le biais de survie), événements, anomalies. Rapport de référence de 8 000 parties dans `docs/balance/`, avec ses premiers constats.
- M2 : **briques d'effets** (ADR-0007). Catalogue fermé de 63 briques paramétrées (`BrickCatalog`), aux paramètres typés et bornés, et statuts génériques. Les 54 cartes, 8 événements et 4 technologies sont décrits en données (`effects`), sans classe spécifique. Format de contenu v3, schémas d'édition à jour, `docs/BRICKS.md` généré, effets affichés dans `CARDS.md`.
- M2 : tests de chaque brique sur contenu factice, intégrité du catalogue (alignement avec le schéma, rejets de paramètres invalides, cohérence avec l'usage des cartes) et parties aléatoires avec le vrai contenu, invariants vérifiés après chaque commande. Couverture de `Vortex.Core` à 91 %, seuil CI relevé à 90 %.
- M1 : moteur de règles de base. RNG PCG32 déterministe, état sérialisable, mise en place, manches, tours, marchés, activations, technologies, 4 actions d'équipage, pipeline d'attaque (RULES A6), Tourment, statuts, élimination et victoires. Infrastructure d'effets (points d'interception B2, actions élémentaires B3, empilement B4, ordre B7). Décisions par re-exécution déterministe (ADR-0009). Projection publique, validation des invariants, `config.json` et son schéma.
- M1 : tests du moteur sur contenu factice. Dés imposés, décisions, ordre de résolution, boucles de réactions, parties aléatoires avec contrôle des invariants, déterminisme des rejeux, complétude des clones, absence de fuite dans la vue publique.
- M0 : solution .NET 10 (`Vortex.Core` en netstandard2.1/C# 9, `Vortex.ContentTool`, `Vortex.Core.Tests`), avec warnings bloquants, analyseurs, versions centralisées et fichiers de verrouillage NuGet, et SDK épinglé (`global.json`).
- M0 : **le JSON devient la source de vérité** des cartes (ADR-0008) : `cards.json`, `events.json`, `technologies.json` (54 cartes ATK, 50 DEF, 15 événements, 4 technologies), migrés depuis `Vortex.xlsx`, puis classeur retiré. Chaque carte porte son texte et son arbitrage. JSON Schemas pour l'édition dans VS Code.
- M0 : `Vortex.ContentTool` (validate, format, docs) et `docs/CARDS.md` généré.
- M0 : RULES.md restructuré : partie A (règles de base, sans aucune carte), partie B (modèle d'effets : points d'interception, actions élémentaires, empilement, durées, choix, ordre de résolution). ADR-0007 (moteur indépendant des cartes) et ADR-0008.
- M0 : chargement JSON durci (pas de métadonnées de type, membres inconnus rejetés, profondeur et taille bornées).
- M0 : tests du contenu, du durcissement du chargement et de l'outil de contenu ; garde-fous « aucun id de carte dans le moteur » et « aucun arbitrage ne cite une autre carte » ; hygiène des sources (caractères invisibles et bidi).
- M0 : CI GitHub Actions (build, tests et couverture, format, audit des vulnérabilités, cohérence des données générées, gitleaks) et CodeQL (C# + workflows) ; actions épinglées par SHA ; modèle de PR.
- M0 : structure du dépôt, règles consolidées (`docs/RULES.md`), architecture, modèle de sécurité, guide de contribution, ADR 0001 à 0006, configuration Git/LFS/EditorConfig, Dependabot.

### Modifié
- `RULES.md` v0.3 : options ⚙ de rotation du premier joueur et de nombre de technologies, questions ouvertes réorganisées (règles et équilibrage) avec renvoi au journal des arbitrages.
- Le rapport d'équilibrage identifie le contenu par l'empreinte des quatre fichiers de contenu, et non plus du seul `cards.json`. L'option `--samples` est remplacée par `--bot`.
