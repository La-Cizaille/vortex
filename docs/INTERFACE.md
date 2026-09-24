# Interface du prototype (M4)

Ce document décrit ce que le joueur voit et comment il agit, dans une partie et hors partie. Il fixe les décisions du game designer (ARB-59 à ARB-66 dans [`ARBITRAGES.md`](ARBITRAGES.md)) ; la manière de le construire est dans [ADR-0014](adr/0014-presentation-par-evenements.md) et [ADR-0015](adr/0015-scene-de-jeu.md).

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

- **Une scène 3D vue par une caméra fixe** pour les vaisseaux, le dé, le fond et les effets, et **une interface 2D par-dessus** pour les cartes, le marché, les jauges, les actions et les info-bulles (ADR-0015).
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
- **Surcharge** : un petit jeton près du vaisseau, allumé quand je l'ai.
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
  - Attaque : les dés lancés (par exemple « 2d8, avantage »), le détail des bonus et malus, le bouclier effectif de la cible et la fourchette de dégâts. Un bonus qui dépend du jet (par exemple « +3 si la somme est paire ») est affiché comme conditionnel.
  - Sabotage : le bouclier actuel de la cible.
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
| Glisser l'Attaque vers un adversaire | `Attack(cible, surcharge)` |
| Glisser le Sabotage vers un adversaire | `Sabotage(cible)` |
| Toucher Reparamétrage | `RerollShield(surcharge)` |
| Toucher Surcharge | `Overcharge` |
| Toucher Posture défensive (option) | `DefensivePosture` |
| Choisir dans une décision | `AnswerDecision(décision, choix)` |
| Bouton « Fin de tour » | `EndTurn` |

## 7. Questions ouvertes

- **Dépenser ou garder la surcharge.** Quand j'ai un jeton de surcharge, une attaque ou un reparamétrage peut le dépenser (un dé de plus) ou le garder. Proposition : toucher le jeton pour l'« armer » ; armé, il brille et l'aperçu montre l'attaque surchargée. La prochaine attaque ou le prochain reparamétrage le dépense.
