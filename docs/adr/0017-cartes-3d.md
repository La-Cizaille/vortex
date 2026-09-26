# ADR-0017 : Les cartes sont des objets 3D qui suivent la disposition de l'interface

- **Statut** : accepté, 2026-09-25. Remplace, pour les cartes seulement, le choix de l'ADR-0015 de les dessiner dans l'interface 2D.

## Contexte
L'ADR-0015 plaçait les cartes dans l'interface 2D, pour un texte net et un glisser-déposer simple. Le game designer a demandé que **les cartes soient, elles aussi, des assets 3D** (ARB-73), comme les vaisseaux :
- pour pouvoir les animer dans l'espace (retournement, vol vers un vaisseau, dépôt au centre) ;
- pour que leur aspect (forme, épaisseur, dos) se travaille comme un modèle, et plus tard dans Blender.

Contraintes :
- le texte des cartes est long : il doit rester lisible, y compris agrandi ;
- la disposition (marché, panneaux, galerie) est déjà réglée dans l'interface, et le designer la modifie dans Unity ;
- le survol et le glisser-déposer doivent marcher sur une carte, à la souris comme au doigt.

## Options
1. **Cartes posées à plat sur la table**, à leur place dans le monde 3D. C'est le plus « physique », mais la caméra inclinée les écrase en perspective et leur texte devient illisible.
2. **Cartes 3D à des positions calculées à la main dans la scène.** Elles sont lisibles, mais toute la mise en page serait à refaire en coordonnées 3D, loin des outils d'interface du designer.
3. **Cartes 3D qui suivent une place de l'interface.** L'interface garde la disposition. Chaque carte est un objet 3D posé devant la caméra, face à elle, à la position et à la hauteur de sa place.

## Décision
L'option 3.

- **La carte** est un prefab 3D (`Card.prefab`) :
  - un corps de 1 × 1,4 unité et 0,02 d'épaisseur, teinté de la couleur de sa technologie ;
  - une face : fond, illustration, textes TextMeshPro 3D, badge de Tourment ;
  - un dos.

  Le corps est une boîte pour l'instant ; un modèle (coins arrondis, biseau) pourra la remplacer dans le prefab. Les matériaux sont dans `Theme/Materials/`, en *Unlit* pour que la carte se lise de la même façon quel que soit l'éclairage ; le designer peut passer au shader *Lit*.
- **Placement** (`CardAnchor`) : la carte suit sa place de l'interface, à une distance fixe de la caméra (6 unités), tournée comme elle, et à la hauteur de la place. Une carte sortie d'une zone de défilement est cachée.
- **Couches d'affichage** :
  - l'interface est dessinée par la caméra à 10 unités (mode *Screen Space - Camera*) : les cartes, plus proches, passent devant ses panneaux ;
  - un calque de **premier plan** (*overlay*) reste au-dessus des cartes, pour les dés et les futures fenêtres (ancre de retour visuel `Foreground`) ;
  - la carte agrandie est à 3 unités, devant tout le reste de la table.
- **Pointeur** : la caméra porte un `PhysicsRaycaster`, et chaque carte une boîte de collision. Le survol et, plus tard, le glisser-déposer passent par le même système d'événements que l'interface. La copie agrandie ne répond jamais au pointeur.

## Conséquences
- (+) Les cartes peuvent être animées en 3D, et leur aspect se travaille comme un modèle (prefab, matériaux, puis maillage Blender).
- (+) La disposition reste dans l'interface : les réglages déjà faits (marché, panneaux, galerie) sont conservés, et le designer continue de les éditer avec les outils d'interface.
- (+) Le texte reste net : les textes TextMeshPro 3D sont vectoriels et toujours face à la caméra.
- (−) Deux couches à garder cohérentes : l'interface doit rester plus loin que les cartes (plan à 10, cartes à 6, carte agrandie à 3), et ce qui doit passer devant les cartes va dans le calque de premier plan.
- (−) Une carte à cheval sur le bord d'une zone de défilement s'affiche entière, faute de découpe 3D. C'est accepté pour la galerie, un outil de réglage.

## Complément (2026-09-25, modèle de carte, ARB-85)
- **Le corps est un modèle** (`tools/blender/build_card.py`, `Art/Cards3D/Card.fbx`) : titane brossé sombre, panneaux d'aluminium clair en léger relief, fenêtre sertie pour l'illustration, disque de l'emplacement, gemme, onglet de l'usage ; 0,04 d'épaisseur hors tout.
- **La mise en page vient du modèle.** Des repères `Zone_*` y disent où poser l'illustration et chaque texte. `CardPrefabSync` place les parties du prefab dessus à chaque changement du modèle ; sans modèle, la boîte garde la première mise en page. La disposition se règle donc dans le script du modèle, plus dans le prefab.
- **Matériaux éclairés.** Les métaux sont en *Lit* (métal, brillance, relief brossé) : c'est un retour sur le choix *Unlit* de départ, car le designer veut un métal qui reflète la lumière. Pour que la carte reste lisible partout, les panneaux sont à demi métalliques (ils restent clairs quel que soit leur reflet), et les métaux de la carte ne prennent **pas le reflet direct de la lampe** : plate et face à la caméra, une carte entière s'allumait d'un coup et déclenchait le Bloom. L'illustration reste *Unlit*.
- **Couleur de la technologie** : sur les liserés et la gemme, par un *property block* par matériau du corps (`CardDisplay`). La gemme d'une carte de technologie rayonne au même niveau pour toutes les couleurs (`ThemeSettings.CardGemGlow`, au-dessus du seuil du Bloom) ; celle d'une carte neutre est éteinte. Une carte marquée (achat, décision) prend la couleur de marquage sur ses liserés, là où la boîte changeait de couleur en entier.

## Complément (2026-09-26, la carte devient un module, ARB-96)
- Le modèle de carte est repris en **module de vaisseau branchable** (bible §6.5 bis) : boîtier d'acier, poignée, connecteur à broches, plaque du nom, écran de l'illustration, terminal de la règle, voyants. Les zones nommées et les quatre matériaux ne changent pas : le jeu le prend sans modification de code.
- Les couleurs sont dans **une texture peinte à la disposition de la face** ; les matériaux du jeu restent blancs. Le texte est **clair sur des faces sombres** (encre `CardInk` du thème, os) ; ces faces ne reflètent ni la lampe ni l'environnement, pour que la règle reste nette.
- Le **dos** prend la taille du boîtier (`Zone_Dos`), pour ne pas couvrir la poignée ni le connecteur.
