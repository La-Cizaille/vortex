# Vortex

Jeu de combat de vaisseaux spatiaux au tour par tour, en chacun pour soi, de 2 à 5 joueurs. Chaque vaisseau a des points de vie, un bouclier et deux modificateurs (attaque et défense) achetés dans des « marchés noirs » communs.

Adaptation numérique d'un jeu de société. Cibles : **Android** et **Windows**. Style 3D simple, pensé pour le mobile.

> État : **M0, socle du projet**. La feuille de route est plus bas.

## Structure du dépôt

| Dossier | Contenu |
|---|---|
| [`docs/`](docs/) | [Règles](docs/RULES.md), [catalogue des cartes](docs/CARDS.md) (généré), [architecture](docs/ARCHITECTURE.md), [sécurité](docs/SECURITY.md), [contribution](docs/CONTRIBUTING.md), [décisions (ADR)](docs/adr/). |
| `core/` | Moteur de règles en C# pur, partagé entre Unity, les tests, le simulateur et le futur serveur. `core/Runtime/Data/*.json` est la **source de vérité** des cartes. |
| `dotnet/` | Solution .NET : tests, outil de contenu (validation, format, catalogue), simulateur d'équilibrage (M3), serveur (phase 2). |
| `unity/` | Client Unity 6.3 LTS (URP). |

## Pré-requis

- [SDK .NET 10](https://dotnet.microsoft.com/download)
- [Unity Hub](https://unity.com/download) + **Unity 6.3 LTS**, avec les modules *Android Build Support* et *Windows Build Support (IL2CPP)*
- Git + Git LFS (`git lfs install`), utilisé pour les assets graphiques
- (Outils IA, optionnel) [uv](https://docs.astral.sh/uv/) pour Unity MCP

## Démarrage

```bash
git clone <url> && cd vortex
git lfs pull
dotnet test dotnet/Vortex.sln        # à partir de M0
```

Ouvrir le dossier `unity/` dans Unity Hub (à partir de M4).

## Feuille de route

| Jalon | Contenu |
|---|---|
| **M0** | Socle : dépôt, documentation, CI de qualité et de sécurité, import des cartes |
| **M1** | Moteur de base : tours, marchés, attaques, Tourment, événements, combos, victoire |
| **M2** | Les 54 modificateurs, 8 événements et 4 technologies |
| **M3** | Simulateur bot contre bot et rapport d'équilibrage |
| **M4** | Client Unity jouable en hot-seat (visuels provisoires, habillage modifiable) |
| **M5** | Finitions : modèles 3D, animations, builds Android et Windows |
| Phase 2 | Serveur autoritaire, lobby, jeu en ligne |

## Licence

À définir. Tous droits réservés tant qu'aucune licence n'est ajoutée.
