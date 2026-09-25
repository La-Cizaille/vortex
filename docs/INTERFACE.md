# Interface du prototype (M4)

Ce document décrit ce que le joueur voit et comment il agit, dans une partie et hors partie. Il fixe les décisions du game designer (ARB-59 à ARB-67 dans [`ARBITRAGES.md`](ARBITRAGES.md)) ; la manière de le construire est dans [ADR-0014](adr/0014-presentation-par-evenements.md) et [ADR-0015](adr/0015-scene-de-jeu.md).

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
|                 Joueur 3    [ Manche 7 ]    Joueur 4                 |
|   Joueur 2                  [ Événement ]                 Joueur 5   |
|   joue après moi            [ Fin des temps : 9 ]    joue avant moi  |
|                                                                      |
|            +---------------- Marché noir ----------------+           |
|            |   ATK : 5 cartes      |     DEF : 5 cartes  |           |
|            |     [Recycler]        |       [Recycler]    |           |
|            +---------------------------------------------+           |
|                         (SAB)  (REP)  (SUR)                          |
|                     (ATQ)                  (POS)                     |
|  [Journal]     [ATK]   [Combo]   VAISSEAU   o surcharge  [DEF]       |
|                          PV 30 - Bouclier 5 - ( )( )( )              |
|                                                  [Accélérer / passer]|
|                                                  [Fin de tour]       |
+----------------------------------------------------------------------+
```

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
- **Bouton de combo** : à côté de mes cartes, puisqu'il les réunit. Il est allumé, à la couleur de la technologie, quand mes deux modificateurs ont la même couleur non neutre et que le combo est jouable. Au survol, il montre l'effet de la technologie.

### 3.3 Marché noir

- **Au centre de l'écran** : marché ATK à gauche, marché DEF à droite. Zoom sur une carte au survol.
- **Acheter** : glisser une carte du marché vers mon vaisseau. Pendant le glisser, on voit la carte que je vais perdre.
- **Recycler** : un bouton sous chaque moitié.
- **Passer le marché** : un bouton.
- **Réduit** : une fois la phase de marché terminée, le marché se réduit tout seul en une bande de miniatures. On peut le rouvrir pour le consulter, et le zoom au survol fonctionne toujours.

### 3.4 Actions d'équipage

- Elles apparaissent **en demi-cercle au-dessus de mon vaisseau**, chacune avec une icône simple : Attaque, Sabotage, Reparamétrage, Surcharge, et Posture défensive si l'option est activée.
- **Au survol**, une fenêtre précise l'effet de l'action, tel qu'il s'appliquerait maintenant (par exemple : Reparamétrage à 2 dés avec la surcharge).
- **Actions avec une cible** (Attaque, Sabotage) : glisser l'icône vers un adversaire.
- **Actions sans cible** (Reparamétrage, Surcharge, Posture défensive) : un simple toucher.
- **Aperçu pendant le glisser**, calculé par le moteur :
  - Attaque : les dés lancés (par exemple « 2d8, avantage »), le détail des bonus et malus, le bouclier effectif de la cible et la fourchette de dégâts. Un bonus qui dépend du jet (par exemple « +3 si la somme est paire ») est affiché comme conditionnel. Si le jeton de surcharge est armé, l'aperçu montre l'attaque surchargée.
  - Sabotage : le bouclier actuel de la cible.
  - Réalisation (M4.5, ADR-0018) : l'aperçu s'affiche dans la bulle d'aide, près de la cible survolée. Il est calculé en jouant le coup un grand nombre de fois sur des parties supposées, sans jamais utiliser le hasard de la partie. Il donne :
    - pour une attaque : les dés, chaque bonus ou malus avec sa source, le bouclier effectif de la cible, les dégâts (fourchette et moyenne), les chances de toucher, de critique et de détruire la cible ; il signale aussi une déviation possible et les PV que l'attaquant peut perdre en retour ;
    - pour un sabotage : le bouclier de la cible maintenant et après la relance.

    Un bonus qui dépend du jet est affiché comme une fourchette (« −1 à +3 »), et non par sa condition. Les chances sont des estimations, arrondies à 5 %.
- **Cibles interdites** (protégées par une carte, ou imposées par une autre) : grisées pendant le glisser, avec la raison au survol.
- **Deux actions dans le tour** (technologie Casino Cosmique) : l'action déjà faite s'éteint, les autres restent disponibles.
- **Action imposée** (Mutinerie) : seules l'action et la cible imposées restent allumées, avec un cadenas.

### 3.5 Cartes à utiliser

- **Utiliser une carte** (usage unique, ou déclenchement manuel) : la glisser vers le centre de l'écran.
- Si la carte demande un choix (une cible, une valeur, une annonce de 1 à 8…), une décision suit (voir 3.6).
- Une carte qu'on ne peut pas utiliser maintenant ne se glisse pas, et dit pourquoi au survol.
- **Combo** : le bouton de combo (3.2), pas un glisser.

### 3.6 Décisions

- Quand le choix porte sur des **joueurs ou des cartes visibles**, on les touche directement sur la table, où ils sont surlignés.
- Sinon (une valeur, oui ou non, un sens de rotation), une petite fenêtre s'ouvre au centre.
- **Quand un autre joueur doit décider pendant mon tour** (il choisit le modificateur détruit par mon critique, il dévie mon attaque…) : une fenêtre « Joueur X doit choisir » s'affiche, sans faire pivoter la vue. Si ce joueur est un bot, il choisit seul et on voit le résultat.

### 3.7 Déroulé du tour

- Le tour suit les phases des règles (RULES A5) : marché, puis actions (cartes, combo, action d'équipage), puis fin de tour.
- **Fin de tour** : un bouton à droite, qui change de couleur quand il ne reste rien d'utile à faire. Pas de fenêtre de confirmation.

### 3.8 Informations de partie

- **En haut au centre** : le numéro de manche, l'événement en cours (agrandi au survol) et le compte à rebours de la Fin des temps.
- **Journal** : un panneau repliable, fermé par défaut, qui dit qui a fait quoi et avec quels jets de dés.
- **Animations** : chaque événement du moteur est montré à son tour (ADR-0014). Un bouton « accélérer / passer » est disponible pendant qu'elles se jouent, et la vitesse par défaut se règle dans les options.

### 3.9 Temps de tour limité (ARB-70)

- Le tour d'un joueur a une **durée limitée**, que l'on peut **désactiver** pour les tests.
- Le temps qui reste se voit, par exemple un anneau qui se vide autour du vaisseau ou du bouton « Fin de tour ». Il s'entend aussi : un signal sonore dans les dernières secondes.
- Durée, comportement à l'expiration et temps de réponse aux décisions : questions ouvertes (§7).

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
| Choisir dans une décision | `AnswerDecision(décision, choix)` |
| Bouton « Fin de tour » | `EndTurn` |

## 7. Questions ouvertes

Sur le temps de tour limité (§3.9, ARB-70) :
1. **Durée** d'un tour : fixe (par exemple 60 secondes) ou réglable dans le menu de la partie ?
2. **À l'expiration** : le tour se termine simplement (fin du marché, puis fin de tour), ou un bot joue le reste du tour à la place du joueur ?
3. **Décisions** demandées pendant le tour d'un autre (coup critique, déviation…) : un délai plus court, par exemple 15 secondes, puis un choix par défaut ?
4. **Réglage** : activé par défaut dans une partie normale, désactivé dans le mode test ?

## 8. Ce qui est construit

| Étape | Ce qui marche |
|---|---|
| M4.3 | Cartes, visuels provisoires, galerie. |
| M4.4 | La table (scène `Game`) : bandeau, adversaires en arc dans l'ordre du tour, marché noir, vaisseau et cartes du joueur, jetons de Tourment, technologies, surcharge, effets temporaires, tour en cours, épave d'un joueur éliminé, marqueur du leader (option), journal, vitesse de lecture. Des bots jouent une partie entière qu'on regarde. |
| M4.4, mode test | Le siège 1 est joué par une personne, les autres par des bots (niveau « normal » par défaut). Un panneau en bas à droite liste les coups que le moteur autorise, un bouton par coup, ainsi que les réponses quand une carte demande un choix. C'est un outil provisoire : il disparaîtra quand les gestes seront là. Réglages : objet `Partie` de la scène `Game`, rubrique *Partie de test*. |
| M4.5, lisibilité | Après le premier playtest : **zoom** sur toute carte de la table (marché, panneaux, cartes du joueur). À la souris, la carte s'agrandit au survol ; sur écran tactile, tant que le doigt est posé dessus (l'appui long viendra avec le glisser-déposer). **Dés animés** : chaque lancer (attaque, ou dé d'un effet) s'affiche au centre, roule puis s'arrête sur les valeurs du moteur, avec le total gardé. |
| M4.5, gestes (première partie) | **Fait** : les actions d'équipage en demi-cercle au-dessus du vaisseau, avec leur pictogramme (un nom court en attendant l'icône) ; un toucher pour Reparamétrage, Surcharge et Posture, un glisser vers un adversaire pour Attaque et Sabotage, avec les cibles permises allumées et les autres estompées ; une aide au survol de chaque action ; « Recycler » dans l'en-tête de chaque marché et « Passer le marché » au-dessus ; l'achat en glissant une carte du marché vers son vaisseau, l'utilisation en glissant une de ses cartes au centre ; le bouton de combo ; le jeton de surcharge armé d'un toucher (ARB-67) ; « Fin de tour », qui s'allume quand il ne reste rien d'autre à faire ; une fenêtre pour chaque décision ; la fiche d'un adversaire agrandie au survol. Le panneau du mode test reste disponible, désactivé par défaut. **Reste à faire** : les choix faits directement sur la table, le temps de tour limité (en attente des réponses d'INTERFACE §7). |
| M4.5, appui long | **Fait** : sur écran tactile, l'appui long remplace le survol (§1) : carte agrandie, aide d'une action, fiche d'un adversaire agrandie. Un simple toucher ne montre rien de tout cela, et relâcher le doigt après un appui long ne déclenche pas le bouton. Le jeton de surcharge et le bouton de combo s'expliquent aussi au survol ou à l'appui long ; le combo, une fois jouable, nomme la technologie obtenue et son effet (§3.2). |
| M4.5, aperçus | **Fait** : l'aperçu d'une attaque ou d'un sabotage pendant la visée, calculé par le moteur sur des parties supposées (§3.4, ADR-0018). |
| M4.5, gestes (suite) | Priorités retenues après le premier playtest (ARB-71) : boutons de recyclage au niveau du marché (§3.3) ; actions d'équipage au niveau du vaisseau, avec des pictogrammes reconnaissables (§3.4) ; informations d'un adversaire au survol (§3.1) ; glisser-déposer et aperçus (§3.4 à §3.7) ; temps de tour limité (§3.9). |
| M4.6, menus | **Fait** (ADR-0019) : scène `Menu`, ouverte en premier, avec l'accueil (« Trouver une partie » et « Social » grisés, « Quitter » hors téléphone), la partie locale (2 à 5 joueurs, 5 par défaut ; chaque siège humain ou bot, avec son niveau et un nom), le menu de développement (options de règles et graine, absent des builds publiés) et les options (vitesse des animations ; plein écran et résolution sous Windows). En partie : la pause (reprendre, recommencer, options, quitter), la fenêtre de fin de partie (« Rejouer », « Menu ») et le pivot de la vue vers chaque humain au début de son tour, avec le bandeau « Tour de X » (§4). |
| M5 | À venir (ARB-72) : modèles 3D (Blender, ADR-0016, [`ASSETS.md`](ASSETS.md)), fond stellaire animé (étoiles qui scintillent, planètes), audio (effets, signaux du temps de tour, musique). |
