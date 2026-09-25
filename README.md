# Vortex

Jeu de combat de vaisseaux spatiaux au tour par tour, en chacun pour soi, de 2 à 5 joueurs (5 en mode standard). Chaque vaisseau a des points de vie, un bouclier et deux modificateurs (attaque et défense) achetés dans des « marchés noirs » communs.

Adaptation numérique d'un jeu de société. Cibles : **Android** et **Windows**. Style 3D simple, pensé pour le mobile.

> État : **M4, prototype Unity**. M0 à M3 et l'équilibrage des règles de base sont terminés : moteur complet, 54 modificateurs, 8 événements, 4 technologies, bots, simulateur, mode standard à 5 joueurs réglé. La table de jeu est jouable en **mode test** contre des bots (M4.4), avec zoom sur les cartes et dés animés. Prochaines étapes : les gestes (M4.5), les menus (M4.6), puis l'atelier Blender (M5). La feuille de route est plus bas.

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

Ouvrir le dossier `unity/` dans Unity Hub. Pour **jouer** : ouvrir la scène `Assets/_Vortex/Scenes/Menu.unity`, lancer le mode Play, puis « Partie locale » : choisir le nombre de joueurs, qui est humain et qui est bot, et les noms. La scène `Game.unity` ouverte seule lance directement une partie de test contre quatre bots. Pendant votre tour :
- toucher ou glisser les actions autour du vaisseau ; pendant la visée d'une attaque, l'aperçu près de la cible annonce les dés, les bonus, les dégâts et les chances de toucher ;
- glisser une carte du marché vers votre vaisseau pour l'acheter ;
- glisser une de vos cartes au centre pour l'utiliser ;
- « Fin de tour » en bas à droite.

Le panneau qui liste tous les coups se réactive dans l'objet `Partie` (*Show Command Panel*).

Sans ouvrir l'éditeur (il doit être fermé) :

```bash
powershell -ExecutionPolicy Bypass -File tools/Test-Unity.ps1
```

```bash
powershell -ExecutionPolicy Bypass -File tools/Update-UnityAssets.ps1
```

```bash
powershell -ExecutionPolicy Bypass -File tools/Capture-Unity.ps1 -Scene Game -Round 5 -Out captures/table.png
```

Le premier lance les tests Unity. Le deuxième crée les assets de base qui manquent (il ne remplace jamais un asset existant). Le troisième enregistre une image de la table ou de la galerie. Pour les modèles 3D, voir [`CONTRIBUTING.md`](docs/CONTRIBUTING.md#blender-modèles-3d) et [`ASSETS.md`](docs/ASSETS.md).

## Où trouver quoi

| Je cherche… | Document |
|---|---|
| Comment se joue une partie | [`RULES.md`](docs/RULES.md), partie A |
| Pourquoi une règle ou une carte fonctionne ainsi | [`ARBITRAGES.md`](docs/ARBITRAGES.md) |
| Les visuels à créer et leur format | [`ASSETS.md`](docs/ASSETS.md) |
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
| **M5** | Finitions : modèles 3D (Blender, ADR-0016), fond stellaire animé, audio, animations, builds Android et Windows |
| Phase 2 | Serveur autoritaire, lobby, jeu en ligne |

Étapes du jalon M4 :

| Étape | Contenu | État |
|---|---|---|
| M4.1 | Socle du projet Unity, moteur exécuté dans Unity | Fait |
| M4.2 | Session de jeu et lecture des événements (ADR-0014) | Fait |
| M4.3 | Habillage : thème, catalogues d'illustrations, visuels provisoires générés, import automatique, scène Galerie | Fait |
| M4.4 | Table et affichage : scène de jeu, adversaires, vaisseau, marché, informations de partie ; les bots jouent et tout se voit | Fait |
| M4.5 | Interactions : zoom sur les cartes, dés animés, cartes en 3D, premiers gestes, aperçus calculés par le moteur (faits) ; recyclage au niveau du marché ; actions d'équipage au niveau du vaisseau, avec des pictogrammes ; informations d'un adversaire au survol ; glisser-déposer et aperçus calculés par le moteur ; décisions, combo, fin de tour ; temps de tour limité et désactivable (ARB-70, ARB-71) | En cours |
| M4.6 | Menus : accueil, partie locale, options, pause, fin de partie, menu de développement ; vue qui pivote entre humains | Fait |
| M4.7 | Vérification : parties complètes à 2, 3 et 5 joueurs, sans erreur ; remplacer une image change le visuel sans code | À faire |

## Licence

À définir. Tous droits réservés tant qu'aucune licence n'est ajoutée.

Composant tiers livré avec le jeu : la police LiberationSans, fournie avec TextMeshPro, sous licence SIL Open Font License 1.1 (`unity/Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`).
