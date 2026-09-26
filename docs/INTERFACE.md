# Interface du prototype (M4)

Ce document décrit ce que le joueur voit et comment il agit, dans une partie et hors partie. Il fixe les décisions du game designer (ARB-59 à ARB-75 et ARB-80 à ARB-82 dans [`ARBITRAGES.md`](ARBITRAGES.md)) ; la manière de le construire est dans [ADR-0014](adr/0014-presentation-par-evenements.md) et [ADR-0015](adr/0015-scene-de-jeu.md).

Il décrit une **disposition et des comportements**, pas un style : couleurs, formes, polices et animations restent libres et se règlent dans Unity, sans code (voir [`CONTRIBUTING.md`](CONTRIBUTING.md#ajouter-ou-modifier-un-visuel-à-partir-du-jalon-m4)).

## 1. Principes

- **L'interface ne connaît aucune règle.** Ce qu'on peut faire, et sur qui, vient des commandes légales que donne le moteur. Un aperçu (dés d'une attaque, bouclier effectif) est calculé par le moteur avec le même code que l'action réelle. Sinon l'aperçu mentirait dès qu'une carte modifie une attaque.
- **L'interface ne voit que la vue publique** de la partie : jamais l'ordre des paquets ni les dés à venir.
- **Souris et doigt font les mêmes gestes.** Un écran tactile n'a pas de survol : l'appui long le remplace.

| Geste | Souris | Tactile |
|---|---|---|
| Voir en grand, lire une info-bulle | survol | appui long |
| Sélectionner | clic | toucher |
| Agir sur une cible | glisser-déposer | glisser-déposer |

## 2. Forme générale

- **Une scène 3D vue par une caméra fixe** pour les vaisseaux, le dé, le fond et les effets, et **une interface 2D** pour le marché, les jauges, les actions et les info-bulles (ADR-0015).
- **Les cartes sont des objets 3D** (ARB-73, ADR-0017) : chacune suit sa place dans l'interface, face à la caméra, et passe devant les panneaux. Elles peuvent ainsi être animées dans l'espace, et leur aspect se travaille comme un modèle.
- **Paysage uniquement.** Résolution de référence 1920×1080. L'interface respecte les zones sûres des téléphones à encoche.
- **Cible minimale : un téléphone de 6 pouces.** Le texte des cartes se lit en zoom (survol ou appui long).
- **Fond** : un fond spatial (étoiles qui scintillent, planètes à l'arrière-plan) est prévu, mais pas nécessaire au prototype.

## 3. L'écran de jeu

Disposition à 5 joueurs, vue du joueur dont c'est le tour :

```text
+----------------------------------------------------------------------+
|                       [ Manche 7 - Événement ]              [Pause]  |
|                       [ Fin des temps : 9    ]                       |
|                 Joueur 3  [marché réduit]  Joueur 4                  |
|                           [ Marché ]                                 |
|   Joueur 2          [ question d'une décision ]           Joueur 5   |
|   joue après moi          [ dés du lancer ]          joue avant moi  |
|                                                                      |
|                        (SUR)          (REP)                          |
|                    (ATQ)                  (SAB)                      |
|                             VAISSEAU     o surcharge                 |
|               [ATK]         [Combo]          [DEF]    [temps: 1:30]  |
|  [Journal]           PV 30 - Bouclier 5 - ( )( )( )    [Fin de tour] |
+----------------------------------------------------------------------+
```

Pendant ma phase de marché, le marché noir s'ouvre au centre de la table (3.3). La Posture défensive, quand l'option est activée, prend place dans l'arc entre Reparamétrage et Sabotage (3.4).

### 3.1 Adversaires

- Ils sont **face au joueur**, en arc en haut de l'écran, **dans l'ordre du tour**, comme autour d'une vraie table en sens horaire : le joueur qui joue après moi est à gauche, celui qui joue avant moi à droite. Les deux extrémités de l'arc sont donc mes voisins, ce qui rend lisibles les cartes qui visent les joueurs adjacents.
- Au repos, chaque adversaire montre :
  - ses PV et son bouclier ;
  - son jeton de surcharge ;
  - ses technologies obtenues (les trois ronds, en petit, voir 3.2) ;
  - ses deux modificateurs en miniature, avec leurs jetons de Tourment ;
  - ses effets temporaires en icônes : bouclier désactivé, Intouchable, Ordre, posture défensive…
- Au survol, sa fiche s'agrandit et ses cartes deviennent lisibles.
- **Joueur éliminé** : une épave grisée qui reste à sa place, pour garder les positions et les voisins. Si l'option « fantômes » est activée, elle porte un marqueur de fantôme.
- **Leader en PV** : un marqueur n'apparaît que si l'option « prime sur le leader » est activée. Il accompagne l'effet visuel de la prime (ARB-44).

### 3.2 Mon vaisseau

- **Devant moi**, en bas au centre. Le vaisseau est statique : pas d'interaction, seulement des animations et de la cosmétique, à voir plus tard.
- **Mes deux modificateurs** sont de part et d'autre du vaisseau : ATK à gauche, DEF à droite, comme dans le marché. Ils portent leurs jetons de Tourment.
- **Sous le vaisseau** : PV, bouclier, et **trois ronds** qui sont les technologies obtenues vers l'Élection galactique (il en faut 3 pour gagner). Chaque rond s'allume à la couleur de la technologie obtenue.
- **Surcharge** : un petit jeton près du vaisseau, allumé quand je l'ai. Le toucher l'**arme** : il brille, et la prochaine attaque ou le prochain reparamétrage le dépense. Le toucher de nouveau le désarme (ARB-67).
- **Effets temporaires** : en icônes, avec leur info-bulle.
- **Bouton de combo** : au centre, par-dessus le bas du vaisseau, au premier plan (ARB-89). Il n'apparaît que lorsque le combo est jouable (ARB-87), à la couleur de la technologie. Au survol, il montre l'effet de la technologie.
- **Cockpit** (ARB-90, construit) : sous le vaisseau, il remplace le panneau de chiffres ; ses sockets reçoivent les deux cartes du joueur. Il comprend :
  - une jauge de PV qui se remplit et se vide, avec le chiffre dessus ;
  - une jauge de bouclier en manomètre, avec une aiguille ;
  - deux emplacements de modificateurs en creux, avec le symbole de l'emplacement ;
  - un interrupteur pour la surcharge, levé quand le jeton est armé, son bouton allumé tant que le vaisseau en a un ; le toucher arme ou désarme le jeton ;
  - trois diodes pour les technologies obtenues ;
  - une étiquette au nom du joueur.

  Le vaisseau a été avancé vers la table pour lui laisser la place (ARB-89). Toucher le cockpit répond à une décision qui propose son propre vaisseau. Les effets en jeu et le marqueur du leader passent au-dessus de lui. Sans modèle dans le thème, le panneau de chiffres de l'interface reste.

### 3.3 Marché noir

- **Au centre de l'écran** : marché ATK à gauche, marché DEF à droite. Zoom sur une carte au survol.
- **Acheter** : glisser une carte du marché vers mon vaisseau. Pendant le glisser, on voit la carte que je vais perdre.
- **Recycler** : un bouton dans l'en-tête de chaque moitié (ARB-71).
- **Passer le marché** : un bouton au-dessus du marché.
- **Réduit** (ARB-81) : en dehors de ma phase de marché, le marché est réduit en une bande de miniatures sous le bandeau de manche. Il s'ouvre tout seul au début de ma phase de marché et se réduit à sa fin.
  - Le bouton « Marché », sous la bande, l'ouvre pour le consulter ; « Réduire le marché » le replie. Ce choix tient jusqu'au prochain changement de phase.
  - Le zoom au survol fonctionne dans les deux états.

### 3.4 Actions d'équipage

- Elles apparaissent **en arc de cercle centré au-dessus de mon vaisseau** (ARB-74), chacune avec une icône simple : à gauche les actions d'attaque (Attaque à l'extrémité, Surcharge), à droite les actions de bouclier (Reparamétrage, Posture défensive si l'option est activée, Sabotage à l'extrémité).
- **Seules les actions possibles apparaissent** (ARB-87) : aucune pendant le marché ; ensuite, celles que le moteur autorise. Chacune garde sa place sur l'arc, même quand ses voisines sont cachées.
- **Au survol**, une fenêtre précise l'effet de l'action, tel qu'il s'appliquerait maintenant (par exemple : Reparamétrage à 2 dés avec la surcharge).
- **Actions avec une cible** (Attaque, Sabotage) : glisser l'icône vers un adversaire.
- **Actions sans cible** (Reparamétrage, Surcharge, Posture défensive) : un simple toucher.
- **Aperçu pendant le glisser**, calculé par le moteur (ADR-0018) : il s'affiche dans la bulle d'aide, près de la cible survolée. Il est calculé en jouant le coup un grand nombre de fois sur des parties supposées, sans jamais utiliser le hasard de la partie. Il donne :
  - pour une attaque : les dés (par exemple « 2d8, avantage »), chaque bonus ou malus avec sa source, le bouclier effectif de la cible, les dégâts (fourchette et moyenne), les chances de toucher, de critique et de détruire la cible ; il signale aussi une déviation possible et les PV que l'attaquant peut perdre en retour. Si le jeton de surcharge est armé, c'est l'attaque surchargée ;
  - pour un sabotage : le bouclier de la cible maintenant et après la relance.

  Un bonus qui dépend du jet est affiché comme une fourchette (« −1 à +3 »), et non par sa condition. Les chances sont des estimations, arrondies à 5 %.
- **Cibles interdites** (protégées par une carte, ou imposées par une autre) : grisées pendant le glisser, avec la raison au survol.
- **Deux actions dans le tour** (technologie Casino Cosmique) : l'action déjà faite s'éteint, les autres restent disponibles.
- **Action imposée** (Mutinerie) : seules l'action et la cible imposées restent allumées. Un cadenas viendra avec les icônes (M5).

### 3.5 Cartes à utiliser

- **Utiliser une carte** (usage unique, ou déclenchement manuel) : la glisser vers le centre de l'écran.
- Si la carte demande un choix (une cible, une valeur, une annonce de 1 à 8…), une décision suit (voir 3.6).
- Une carte qu'on ne peut pas utiliser maintenant ne se glisse pas, et dit pourquoi au survol.
- **Combo** : le bouton de combo (3.2), pas un glisser.

### 3.6 Décisions

Une décision se prend **sur la table, sans bouton** (ARB-82). La question s'affiche en bandeau au-dessus du centre (« Joueur X : … »), et ce qui y répond s'allume :
- **un joueur** : les fiches des joueurs possibles s'allument, les autres s'estompent ; on touche une fiche. Pour un **sens de rotation**, on touche le voisin qui reçoit son bouclier ;
- **une carte** de la table (un modificateur, une carte du marché) : les cartes possibles ont un cadre allumé ; on touche la carte ;
- **un nombre** (annonce d'une valeur de dé, points à convertir) : une rangée de faces de d8 au centre, seules les valeurs permises actives ;
- **un événement** (fantômes) : les deux événements sont montrés au centre ; on touche celui qu'on garde ;
- **une action d'équipage imposée** : les actions permises de l'arc s'allument ; un toucher, ou un glisser vers la fiche visée ;
- **voler ou détruire une carte** : la carte s'allume ; on la glisse vers son vaisseau pour la voler, au centre de la table pour la détruire.

**Passer** un choix facultatif : la carte qui propose le choix s'allume en gris, et la toucher veut dire « je passe ». Si elle n'est pas sur la table, on touche sa propre fiche (pour une déviation : garder l'attaque) ; si sa fiche est déjà une réponse, la carte est montrée en gris au centre. Le bandeau dit où toucher.

Seule une décision que la table ne sait pas montrer ouvrirait encore la fenêtre de réponses ; aucune n'est dans ce cas aujourd'hui.

**Quand un autre joueur doit décider pendant mon tour** (il choisit le modificateur détruit par mon critique, il dévie mon attaque…) : en partie locale, le bandeau le nomme et ses réponses s'allument, sans faire pivoter la vue. Si ce joueur est un bot, il choisit seul et on voit le résultat.

### 3.7 Déroulé du tour

- Le tour suit les phases des règles (RULES A5) : marché, puis actions (cartes, combo, action d'équipage), puis fin de tour.
- **Fin de tour** : un bouton à droite, qui s'allume dès qu'il ne reste plus d'action d'équipage possible (ARB-87), même s'il reste une carte ou un combo qu'on peut choisir de garder. Pas de fenêtre de confirmation.

### 3.8 Informations de partie

- **En haut au centre** : le numéro de manche, l'événement en cours (agrandi au survol) et le compte à rebours de la Fin des temps.
- **Journal** : un panneau repliable, fermé par défaut, qui dit qui a fait quoi et avec quels jets de dés. On peut le remonter (molette, doigt ou barre) ; il ne suit les nouvelles lignes que si l'on est en bas (ARB-75).
- **Animations** : chaque événement du moteur est montré à son tour (ADR-0014). Un bouton « accélérer / passer » est disponible pendant qu'elles se jouent, et la vitesse par défaut se règle dans les options.

### 3.9 Temps de tour limité (ARB-70)

- Le tour d'un joueur a une **durée limitée**, que l'on peut **désactiver** (ARB-80) :
  - la durée se choisit dans le menu de partie locale : illimitée (par défaut), 60, 90 ou 120 secondes ;
  - le temps ne court que lorsque le joueur peut agir : ni pendant les animations, ni pendant la pause.
- Le temps qui reste se voit au-dessus du bouton « Fin de tour » : les secondes et une barre qui se vide. Dans les dix dernières secondes, il passe à la couleur d'alerte et fait entendre un tic chaque seconde.
- **À l'expiration**, le tour s'arrête simplement : fin du marché, puis fin de tour. Seule une action imposée par une carte est jouée, car le tour ne peut pas finir sans elle.
- **Une décision demandée pendant le tour d'un autre joueur** a 15 secondes. Ensuite, le choix qu'un bot juge le meilleur pour ce joueur est pris à sa place.

## 4. Point de vue

Toutes les informations sont publiques : aucun écran de passage de l'appareil n'est nécessaire.

- **Un humain et des bots** : la vue reste toujours celle de l'humain.
- **Plusieurs humains sur le même appareil** : la vue pivote au début du tour de chaque humain, avec un bandeau « Tour de X ».
- **Pendant le tour d'un bot**, la vue ne bouge pas.

## 5. Hors partie

L'interface est simple au début et pourra s'enrichir.

- **Accueil** : Partie locale ; Trouver une partie (grisé, « bientôt ») ; Options ; Social (grisé) ; Quitter (sous Windows seulement).
- **Partie locale** : le nombre de joueurs, de 2 à 5, avec **5 par défaut** (le mode standard, ARB-52). Pour chaque siège : humain ou bot (et le niveau du bot), et un nom.
- **Menu de développement**, absent des builds publiés : les options de règles à essayer (posture défensive, prime sur le leader, fantômes) et la graine de la partie.
- **Options** : vitesse des animations ; plein écran et résolution sous Windows.
- **Langue** : français seulement. Tous les textes de l'interface passent quand même par une table de textes, pour pouvoir traduire un jour sans reprendre l'interface.
- **Pause** : reprendre, recommencer, options, quitter.
- **Fin de partie** : le vainqueur et le type de victoire (Domination, Élection galactique, ou égalité), avec « Rejouer » et « Menu ».
- **Plus tard** : sauvegarder et reprendre une partie en cours (M5), trouver une partie en ligne et social (phase 2).

## 6. Gestes et commandes du moteur

Chaque geste envoie une commande du moteur à la session. Le moteur la valide : une commande refusée ne change rien et renvoie une erreur typée.

| Geste | Commande |
|---|---|
| Glisser une carte du marché vers mon vaisseau | `PickMarket(marché, position)` |
| Bouton « Recycler » d'un marché | `RecycleMarket(marché)` |
| Bouton « Passer le marché » | `EndMarket` |
| Glisser une carte vers le centre | `ActivateCard(carte)` |
| Bouton de combo | `ActivateTechnology` |
| Toucher le jeton de surcharge | Rien : il s'arme ou se désarme, et sera dépensé par la prochaine attaque ou le prochain reparamétrage |
| Glisser l'Attaque vers un adversaire | `Attack(cible, surcharge armée)` |
| Glisser le Sabotage vers un adversaire | `Sabotage(cible)` |
| Toucher Reparamétrage | `RerollShield(surcharge armée)` |
| Toucher Surcharge | `Overcharge` |
| Toucher Posture défensive (option) | `DefensivePosture` |
| Toucher une fiche, une carte, une face de d8 ou un événement allumés par une décision (3.6) | `AnswerDecision(décision, choix)` |
| Glisser la carte d'une décision « voler ou détruire » vers son vaisseau ou au centre | `AnswerDecision(décision, voler ou détruire)` |
| Bouton « Fin de tour » | `EndTurn` |

## 7. Questions ouvertes

Aucune pour l'instant. La dernière, sur l'animation d'une attaque déviée, est tranchée : ARB-88.

## 8. Ce qui est construit

Tout ce qui précède est construit, sauf ce que la dernière ligne annonce pour M5.

| Étape | Ce qui marche |
|---|---|
| M4.3, habillage | Cartes, visuels provisoires générés, galerie. |
| M4.4, table | La scène `Game` : bandeau, adversaires en arc dans l'ordre du tour, marché noir, vaisseau et cartes du joueur, jetons de Tourment, technologies, surcharge, effets temporaires, tour en cours, épave d'un joueur éliminé, marqueur du leader (option), journal, vitesse de lecture. Le panneau qui liste tous les coups autorisés reste un outil de développement, désactivé par défaut (objet `Partie`, *Show Command Panel*). |
| M4.5, lisibilité | Zoom sur toute carte de la table ; dés animés sur les valeurs du moteur ; cartes en objets 3D (ARB-73) ; appui long au doigt à la place du survol ; événement de la manche agrandi au survol du bandeau ; carte qu'un achat remplacerait marquée en rouge ; mention « Fantôme » sur l'épave (option). |
| M4.5, gestes | Actions d'équipage en arc centré sur le vaisseau, attaque à gauche et bouclier à droite, visibles seulement quand elles sont possibles (ARB-71, ARB-74, ARB-87) ; combo sous le vaisseau, visible seulement quand il est jouable ; achat et utilisation d'une carte par glisser ; combo ; jeton de surcharge armé d'un toucher (ARB-67) ; « Fin de tour » qui s'allume quand aucune action d'équipage ne reste ; aperçus calculés par le moteur (ADR-0018) ; raison de chaque refus, donnée par le moteur ; fiche d'un adversaire agrandie au survol, sans clignotement. |
| M4.5, journal | Panneau repliable qu'on peut remonter ; il ne suit les nouvelles lignes que si l'on est en bas (ARB-75). |
| M4.5, marché réduit | Bande sous le bandeau en dehors de ma phase de marché, ouverte d'elle-même à ma phase de marché, bouton « Marché » pour la consulter (ARB-81). |
| M4.5, décisions | Prises sur la table, sans bouton (ARB-82) : question en bandeau, fiches et cartes allumées, faces de d8, événements au centre, actions imposées sur l'arc, voler ou détruire en glissant, passer en touchant la carte grise ou sa fiche. |
| M4.5, temps de tour | Durée réglable (illimitée par défaut), barre et secondes au-dessus de « Fin de tour », alerte et tic dans les dix dernières secondes, fin de tour à l'expiration, 15 secondes pour une décision hors de son tour (ARB-80). |
| M4.6, menus | Scène `Menu` ouverte en premier (ADR-0019) : accueil, partie locale, menu de développement (absent des builds publiés), options ; en partie, pause, fin de partie et pivot de la vue vers chaque humain (« Tour de X »). |
| M4.7, vérification | Parties complètes à 2, 3, 4 et 5 joueurs, une personne aux gestes contre des bots, sans erreur ; une image déposée change la carte sans code (`MilestoneTests`). |
| M5, animations (lot 1) | Balancement des vaisseaux, rayon d'attaque et recul, projection aux dégâts, flambée des réacteurs au combo, explosion à l'élimination, d8 en 3D qui tournent : effets provisoires en place ([`ANIMATIONS.md`](ANIMATIONS.md) §5). |
| M5, à venir | Animations et modèles : lots 2 et 3 dans [`ANIMATIONS.md`](ANIMATIONS.md). Modèles 3D (Blender, ADR-0016, [`ASSETS.md`](ASSETS.md)), icônes (effets temporaires, texte des cartes, cadenas de l'action imposée), fond stellaire animé, audio (le tic du temps de tour est aujourd'hui un bip généré), builds Android et Windows (ARB-72). |
