# Vortex

Jeu de combat de vaisseaux spatiaux au tour par tour, en chacun pour soi, de 2 à 5 joueurs (5 en mode standard). Chaque vaisseau a des points de vie, un bouclier et deux modificateurs (attaque et défense) achetés dans des « marchés noirs » communs.

Adaptation numérique d'un jeu de société. Cibles : **Android** et **Windows**. Style 3D simple, pensé pour le mobile.

> État : **M4, prototype Unity**. M0 à M3 et l'équilibrage des règles de base sont terminés : moteur complet, 54 modificateurs, 8 événements, 4 technologies, bots, simulateur, mode standard à 5 joueurs réglé. La feuille de route est plus bas.

## Structure du dépôt

| Dossier | Contenu |
|---|---|
| [`docs/`](docs/) | [Règles](docs/RULES.md), [journal des arbitrages](docs/ARBITRAGES.md), [catalogue des cartes](docs/CARDS.md) (généré), [équilibrage](docs/balance/README.md), [architecture](docs/ARCHITECTURE.md), [sécurité](docs/SECURITY.md), [contribution](docs/CONTRIBUTING.md), [décisions techniques (ADR)](docs/adr/). |
| `core/` | Moteur de règles en C# pur, partagé entre Unity, les tests, le simulateur et le futur serveur. `core/Runtime/Data/*.json` est la **source de vérité** des cartes. |
| `dotnet/` | Solution .NET : tests, outil de contenu (validation, format, catalogue), simulateur d'équilibrage, serveur (phase 2). |
| `unity/` | Client Unity 6 (URP). Version : [ADR-0013](docs/adr/0013-version-unity.md). |

## Pré-requis

- [SDK .NET 10](https://dotnet.microsoft.com/download)
- [Unity Hub](https://unity.com/download) + **Unity 6000.6.3f1**, la version indiquée dans `unity/ProjectSettings/ProjectVersion.txt` (politique de version : ADR-0013), avec les modules *Android Build Support* et *Windows Build Support (IL2CPP)*. Détails et pièges connus : [`CONTRIBUTING.md`](docs/CONTRIBUTING.md#installer-unity)
- Git + Git LFS (`git lfs install`), utilisé pour les assets graphiques
- (Outils IA, optionnel) [uv](https://docs.astral.sh/uv/) pour Unity MCP

## Démarrage

```bash
git clone <url> && cd vortex
git lfs pull
dotnet test dotnet/Vortex.sln
```

Valider le contenu après une modification de carte :

```bash
dotnet run --project dotnet/Vortex.ContentTool -- validate core/Runtime/Data
```

Simuler des parties et comparer une variante de règles à la référence (mode d'emploi : [`docs/balance/README.md`](docs/balance/README.md)) :

```bash
dotnet run -c Release --project dotnet/Vortex.Simulator -- compare --games 2000 --variant docs/balance/variants/rotation-antihoraire.json
```

Ouvrir le dossier `unity/` dans Unity Hub. Pour **jouer une partie de test** contre quatre bots : ouvrir la scène `Assets/_Vortex/Scenes/Game.unity`, puis lancer le mode Play. Vous jouez le siège 1 avec le panneau des coups, en bas à droite.

Pour lancer les tests Unity sans ouvrir l'éditeur :

```bash
powershell -ExecutionPolicy Bypass -File tools/Test-Unity.ps1
```

## Où trouver quoi

| Je cherche… | Document |
|---|---|
| Comment se joue une partie | [`RULES.md`](docs/RULES.md), partie A |
| Pourquoi une règle ou une carte fonctionne ainsi | [`ARBITRAGES.md`](docs/ARBITRAGES.md) |
| Ce que le joueur voit et comment il agit | [`INTERFACE.md`](docs/INTERFACE.md) |
| Le texte et l'interprétation d'une carte | [`CARDS.md`](docs/CARDS.md) (généré depuis `core/Runtime/Data/`) |
| Les objectifs et l'avancement de l'équilibrage | [`balance/README.md`](docs/balance/README.md) |
| Pourquoi le code est organisé ainsi | [`adr/`](docs/adr/) et [`ARCHITECTURE.md`](docs/ARCHITECTURE.md) |
| Les questions encore ouvertes | [`RULES.md`](docs/RULES.md#questions-ouvertes-à-trancher-par-le-game-designer), en fin de fichier |

## Feuille de route

| Jalon | Contenu |
|---|---|
| **M0** | Socle : dépôt, documentation, CI de qualité et de sécurité, import des cartes |
| **M1** | Moteur de base : tours, marchés, attaques, Tourment, événements, combos, victoire |
| **M2** | Les 54 modificateurs, 8 événements et 4 technologies |
| **M3** | Simulateur bot contre bot et rapport d'équilibrage |
| **Équilibrage** | Règles de base : fait. Revue des cartes avec un éditeur visuel après le prototype (plan : [`docs/balance/README.md`](docs/balance/README.md)) |
| **M4** | Client Unity jouable en hot-seat (visuels provisoires, habillage modifiable), selon [`INTERFACE.md`](docs/INTERFACE.md) |
| **M5** | Finitions : modèles 3D, animations, builds Android et Windows |
| Phase 2 | Serveur autoritaire, lobby, jeu en ligne |

Étapes du jalon M4 :

| Étape | Contenu | État |
|---|---|---|
| M4.1 | Socle du projet Unity, moteur exécuté dans Unity | Fait |
| M4.2 | Session de jeu et lecture des événements (ADR-0014) | Fait |
| M4.3 | Habillage : thème, catalogues d'illustrations, visuels provisoires générés, import automatique, scène Galerie | Fait |
| M4.4 | Table et affichage : scène de jeu, adversaires, vaisseau, marché, informations de partie ; les bots jouent et tout se voit | Fait |
| M4.5 | Interactions : glisser-déposer, aperçus calculés par le moteur, décisions, marché, combo, fin de tour | À faire |
| M4.6 | Menus : accueil, partie locale, options, pause, fin de partie, menu de développement | À faire |
| M4.7 | Vérification : parties complètes à 2, 3 et 5 joueurs, sans erreur ; remplacer une image change le visuel sans code | À faire |

## Licence

À définir. Tous droits réservés tant qu'aucune licence n'est ajoutée.

Composant tiers livré avec le jeu : la police LiberationSans, fournie avec TextMeshPro, sous licence SIL Open Font License 1.1 (`unity/Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`).
