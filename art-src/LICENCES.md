# Licences des ressources externes

Toute ressource qui n'est pas faite dans le projet entre ici **avant** d'entrer dans le dépôt : sa source exacte, sa licence et l'empreinte SHA-256 du fichier téléchargé (ARB-97, `docs/SECURITY.md` §4). Une ressource sans ligne ici n'a rien à faire dans le jeu.

## Polices (SIL Open Font License 1.1)

Téléchargées depuis le dépôt officiel Google Fonts, [`google/fonts`](https://github.com/google/fonts), au commit épinglé `23e54b51ddffbc7713c583748e3bd86f62b1fa4a` (dossier `ofl/`), le 2026-09-26. Les originaux sont dans `art-src/fonts/`, les fichiers livrés dans `unity/Assets/_Vortex/Art/Fonts/`, chacun à côté de son `OFL.txt`.

**Obligation** : l'OFL demande que la licence accompagne les polices distribuées. Un écran « Crédits et licences » devra reprendre ces textes avant toute publication.

| Police | Fichier | SHA-256 | Usage (bible §6.2) |
|---|---|---|---|
| Anton | `anton/Anton-Regular.ttf` | `a4ba3a92350ebb031da0cb47630ac49eb265082ca1bc0450442f4a83ab947cab` | Titres, affiches, gros chiffres |
| Big Shoulders Stencil Display | `bigshouldersstencildisplay/BigShouldersStencilDisplay[wght].ttf` (police variable) | `93bbefb9f3a497fcfeec5d1ac85c4a947a32b8e5bcaf3b7b5efff52f1b11b0e4` | Pochoirs, marquages |
| ↳ graisse 900 figée | `Art/Fonts/BigShouldersStencil/BigShouldersStencilDisplay-Black.ttf`, produite par `tools/fonts/instance_font.py … 900` (fontTools 4.58.5). Modification permise par l'OFL ; la famille n'a pas de nom réservé. | `14d7f30d6b08a0750dbf1c4996536cad0c330e70226be50fc0eddcafa48a9819` | |
| IM Fell English | `imfellenglish/IMFeENrm28P.ttf` | `fe9705bbde51af802719246d4608d08d37bde956ab99d9a590da996a5221a24c` | Épitaphes, citations |
| IM Fell English Italic | `imfellenglish/IMFeENit28P.ttf` | `47cd75dce54b1f2e0831359d22d5e688f519d68ae45706b664fd310fd0e3ccf7` | Épitaphes, textes d'ambiance |
| Barlow Regular | `barlow/Barlow-Regular.ttf` | `95aa02c7c43096e0dd44d787ba6216864a67157e402adab59b35572e0c1577ea` | Texte courant, règles des cartes |
| Barlow SemiBold | `barlow/Barlow-SemiBold.ttf` | `86577cb32f8abe3673db53ca0f4221e6856751a4f6730c867e00f720f8bb1fc5` | Gras du texte courant |
| Barlow Italic | `barlow/Barlow-Italic.ttf` | `70cf45c354af39e55082fd506e748cc6a0a1812949875f99ded3f76bf691e4ca` | Italique du texte courant |
| Barlow Condensed SemiBold | `barlowcondensed/BarlowCondensed-SemiBold.ttf` | `7b619d14bc2327509a9ef32b0890f709626f7ecc9ff61191c2a4314c5499d2d9` | Libellés, boutons |
| Barlow Condensed Bold | `barlowcondensed/BarlowCondensed-Bold.ttf` | `e476562ec9c1e16cf16475895b511f08c804f438cc9a9f80a44ea50a0eeb5b65` | Libellés en gras |
| Share Tech Mono | `sharetechmono/ShareTechMono-Regular.ttf` | `9ceab1f87414829af259c0f537573ae03ef7dd3147c0b27a36a1a0beb6732677` | Terminal : chiffres de bord, journal. Nom réservé « Share » : fichier utilisé tel quel, jamais modifié |

Textes de licence (même contenu OFL 1.1, en-têtes de copyright propres à chaque famille) :

Les empreintes sont celles des fichiers tels que téléchargés. Git normalise les fins de ligne des fichiers texte : `anton/OFL.txt` et `imfellenglish/OFL.txt`, servis en CRLF, sont stockés en LF, et leur empreinte dans le dépôt diffère donc de celle-ci ; les polices, binaires, sont stockées telles quelles.

| Fichier | SHA-256 |
|---|---|
| `anton/OFL.txt` | `ee67e6ee22790b7929f1a3769ca2801d565c64b5a9096942c1adf5596de9c9e4` |
| `bigshouldersstencildisplay/OFL.txt` | `338f9c050f19daeda1d597243faf79f3a3d437c338af58cb7047617d0ce08771` |
| `imfellenglish/OFL.txt` | `2a3ca501fc4d5efcad9798531e3e06962b1e20c60e464f6cbd6c17630112c773` |
| `barlow/OFL.txt`, `barlowcondensed/OFL.txt` | `186d750eb496a4c17a76385f82be6aea2ac1cf2de074a811d63786cf374ea73f` |
| `sharetechmono/OFL.txt` | `9d96f445b6e9c701428811d0177f894874f8d6f07ecc30d568c506542368f3ff` |

## Textures

Aucune pour l'instant. Banques permises (ARB-97) : Poly Haven, ambientCG, en CC0 uniquement.
