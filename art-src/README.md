# Sources des visuels

Les fichiers de travail des visuels : scènes Blender (`.blend`), images en calques. Ce qui entre dans le jeu est **exporté** vers `unity/Assets/_Vortex/Art/`. Le jeu ne lit jamais ce dossier.

| Dossier | Contenu | Exporté vers |
|---|---|---|
| `ships/` | Un fichier `.blend` par vaisseau, nommé comme son export (`Ship_Faucon.blend`), et ses textures (`Ship_Faucon_BaseColor.png`, `Ship_Faucon_Normal.png`). `Ship_Carcasse` est construit par `tools/blender/build_ship_carcasse.py` | `unity/Assets/_Vortex/Art/Ships/Ship_Faucon.fbx` |
| `cards/` | Le modèle des cartes (`Card.blend`) et ses textures de métal brossé, construits par `tools/blender/build_card.py` | `unity/Assets/_Vortex/Art/Cards3D/Card.fbx` |
| `cockpit/` | Le cockpit du joueur (`Cockpit.blend`, ARB-90, ARB-92, lot B3) et ses textures d'acier noirci et d'acier nu (`Cockpit_*.png`), construits par `tools/blender/build_cockpit.py` | `unity/Assets/_Vortex/Art/Cockpit/Cockpit.fbx` |
| `dice/` | Le dé à 8 faces (`D8.blend`) et ses textures, construits par `tools/blender/build_die.py` | `unity/Assets/_Vortex/Art/Dice/D8.fbx` |
| `backgrounds/` | Le fond de la table (`Fond.blend` : toile de fond et vortex) et ses textures, construits par `tools/blender/build_background.py` | `unity/Assets/_Vortex/Art/Backgrounds/Fond.fbx` |
| `effects/` | La matière des effets (`Effets.blend` : éclats, onde, trait du tir) et leurs planches d'images, construits par `tools/blender/build_effects.py` | `unity/Assets/_Vortex/Art/Effects/Effets.fbx` |
| `market/` | Le rack du marché noir (`Marche.blend`) et ses textures, construits par `tools/blender/build_market.py` | `unity/Assets/_Vortex/Art/Market/Marche.fbx` |
| `props/` | Décor : planètes, station… | `unity/Assets/_Vortex/Art/Props/` |
| `images/` | Sources des illustrations et icônes (calques, formats de travail) | `unity/Assets/_Vortex/Art/Cards/`, `Art/Icons/` |

- La liste des visuels à créer et leurs formats sont dans [`docs/ASSETS.md`](../docs/ASSETS.md). La chaîne de production est décrite dans [ADR-0016](../docs/adr/0016-modeles-3d-blender.md).
- **Export** d'un modèle : `blender --background --disable-autoexec art-src/ships/Ship_Faucon.blend --python tools/blender/export_unity.py -- unity/Assets/_Vortex/Art/Ships/Ship_Faucon.fbx`. Le script vérifie l'échelle et le budget de triangles avant d'exporter. Les textures partent dans `Ship_Faucon.fbm/`, à côté du FBX.
- Les `.blend` sont stockés avec Git LFS. Les copies de sauvegarde de Blender (`.blend1`, `.blend2`…) sont ignorées.
- **Licences** : seulement nos propres créations, ou des ressources libres dont la source et la licence sont notées dans `LICENCES.md` (à créer avec la première ressource externe).
