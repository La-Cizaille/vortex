# Animations et modèles de M5 : plan

Ce document fixe **ce qui s'anime, quand, et avec quoi**. Il sert aux deux chantiers qui avancent en parallèle :
- **moteur et client** : l'événement qui déclenche chaque animation, et ce qu'il faut ajouter au moteur ou au code de la table ;
- **assets 3D** (Blender, [ADR-0016](adr/0016-modeles-3d-blender.md), [`ASSETS.md`](ASSETS.md)) : le modèle ou l'effet à créer.

Les animations demandées au troisième playtest viennent en premier (§2), les suggestions ensuite (§3).

## 1. Deux sortes d'animations

- **Réaction à un événement** : le moteur raconte chaque partie par une suite d'événements (`GameEventType`), joués un par un par `EventPlayer` (ADR-0014). Ce que chaque événement montre est un *retour visuel* rangé dans le profil `Presentation/Feedback/FeedbackProfile`, **jamais dans le code** : changer une animation, c'est changer un asset. Un retour visuel suit la vitesse de lecture, et l'accélération le raccourcit.
- **État qui dure** : le balancement d'un vaisseau, sa contamination, un halo de statut. Rien ne se passe à un instant précis : un composant du vaisseau lit ce que la table montre (`TableModel`) et règle l'effet en continu.

Une animation ne décide jamais de rien : le résultat est déjà calculé par le moteur, elle le montre. Elle ne connaît aucune carte (ADR-0007) : elle réagit à ce qui arrive (dégâts, bouclier, Tourment), pas à la carte qui l'a provoqué.

## 2. Animations demandées

| Animation | Déclencheur | À ajouter (moteur et client) | Modèle ou effet (assets) | Lot |
|---|---|---|---|---|
| **Balancement du vaisseau** : léger roulis et cahots, pour montrer qu'il vole sur place | État permanent | Un composant de vaisseau qui anime une petite oscillation, décalée d'un siège à l'autre pour que la flotte ne bouge pas d'un bloc | Aucun : le modèle actuel suffit. Un pivot au centre de gravité | 1 |
| **Attaque** : un rayon à la couleur de l'attaquant part vers la cible ; l'attaquant recule légèrement | `AttackDeclared` (attaquant, cible, surchargée ou non) | Un retour visuel « rayon » entre deux ancrages (vaisseau de l'attaquant, vaisseau de la cible) ; un recul court de l'émetteur | Effet de rayon (shader ou particules), plus épais et plus lumineux si l'attaque est surchargée ; point de départ sur le modèle (un repère vide `Canon`) | 1 |
| **Dégâts** : le vaisseau est projeté en arrière, d'autant plus loin que les dégâts sont forts, puis revient | `HpLost` de cause `Attack` (et `Reflect`), avec le montant | Un retour visuel de recul proportionnel au montant, plafonné | Étincelles ou débris à l'impact | 1 |
| **Bouclier qui pare** : une sphère de plasma semi-transparente devant le vaisseau encaisse le coup | `AttackResolved` | **Moteur** : l'événement doit dire quelle part de l'attaque le bouclier a arrêtée (le bouclier effectif). Aujourd'hui il ne donne que la valeur d'attaque et les dégâts | Sphère de plasma (shader à effet de Fresnel, ondulation au point d'impact), à la couleur du siège | 2 |
| **Esquive** : le vaisseau glisse de côté, le rayon passe | Une attaque qui ne fait aucun dégât **sans que le bouclier y soit pour quelque chose** : un effet qui annule les dégâts | **Moteur** : un événement qui dit qu'un dégât a été empêché et par quoi (bouclier ou effet), pour choisir entre parade et esquive. Une attaque déviée n'est pas une esquive : elle a son animation dédiée (ARB-88) | Aucun modèle ; traînée des réacteurs pendant l'écart | 2 |
| **Combo** : les réacteurs flambent fort | `TechnologyActivated` (couleur) | Un retour visuel sur les réacteurs du vaisseau | Repères vides `Reacteur_*` sur le modèle, flamme (particules ou mesh émissif) qui s'intensifie à la couleur de la technologie ; le Bloom la fait rayonner | 1 |
| **Tourment** : le jeton se pose, le vaisseau souffre | `TormentPlaced` (carte, jetons sur la carte), puis `HpLost` de cause `Torment` | Un retour visuel sur la carte touchée et le vaisseau | Effet de spores ou d'infection (particules vertes, violettes) | 2 |
| **Vaisseau contaminé** : son aspect change avec les Tourments qu'il porte | État : total des jetons sur ses modificateurs | Le modèle de table doit suivre les jetons pendant la lecture (`TormentPlaced`, `TormentsRemoved`) ; un composant règle l'intensité | Une variante du matériau (veines, taches) dont l'intensité se règle de 0 à 1 ; superficielle suffit | 3 |
| **Dé à 8 faces qui tourne** : il tourne sur lui-même à l'écran et s'arrête sur sa valeur, sans être lancé | `DiceRolled`, `DieRolled` (valeurs déjà connues) | Remplacer les dés 2D de `DiceTray` par le modèle ; la rotation finit sur la face donnée par le moteur (pas de physique) | Modèle d8 (ASSETS §2), faces numérotées, et l'orientation de chaque face notée pour que le jeu sache la tourner vers la caméra | 1 |
| **Fond spatial**, avec des effets ponctuels : un effet par événement de manche au moment où il est révélé | Permanent ; `EventRevealed` (identifiant de l'événement), `RoundStarted` | Un catalogue « événement → effet de fond », rempli dans le thème ; sans entrée, un effet générique. Le catalogue est rangé par identifiant de contenu, comme les illustrations : le code ne nomme aucun événement | Panorama (ASSETS §3), étoiles qui scintillent, planètes lentes ; un effet par événement (tempête électromagnétique, trou noir, nuée…) | 2 |
| **Marché noir** : un paquet posé face cachée par marché, d'où les cartes sont tirées une à une vers leur place | `MarketCardRevealed` (place de la carte), `MarketRecycled`, `MarketCardTaken` | Le paquet est un objet de la table ; une carte révélée part du paquet et rejoint sa place ; une carte prise vole vers le vaisseau de l'acheteur ; un recyclage balaie les cartes vers la défausse. Les événements du marché donnent déjà la place de chaque carte | Un paquet qui ne soit pas une vraie pile : un bloc avec le dos des cartes et une tranche, dont l'épaisseur suit le nombre de cartes restantes | 2 |

## 3. Suggestions

| Animation ou modèle | Déclencheur | Pourquoi | Lot |
|---|---|---|---|
| **Coup critique** : éclair blanc, légère secousse de la caméra | `CriticalHit` | Un critique décide souvent la partie ; il doit se remarquer | 2 |
| **Modificateur détruit** : la carte se brise ou brûle | `CardDiscarded` pendant une attaque | Rend lisible la perte d'une carte au critique | 2 |
| **Élimination** : explosion, puis l'épave dérive légèrement | `PlayerEliminated` | Moment fort ; l'épave existe déjà (grisée et inclinée) | 1 |
| **Surcharge** : arcs électriques sur la coque tant que le jeton est présent ; plus forts quand il est armé | `OverchargeChanged`, état | Le jeton est petit : la coque le montre de loin | 2 |
| **Sabotage** : onde de brouillage sur le bouclier de la cible | `CrewActionPerformed` (sabotage), `ShieldChanged` | Distingue le sabotage d'une attaque | 2 |
| **Reparamétrage** : le bouclier se recharge (anneau qui se remplit) | `ShieldChanged` sur soi | Même famille visuelle que la parade | 2 |
| **Réparation** : nanites ou lueur verte | `HpGained` | Les gains de PV passent aujourd'hui inaperçus | 3 |
| **Achat, vol et activation de carte** : la carte vole vers le vaisseau, d'un vaisseau à l'autre, ou se consume au centre | `MarketCardTaken`, `CardStolen`, `CardActivated` | Montre d'où vient et où va chaque carte | 2 |
| **Dégâts renvoyés** : le rayon revient vers l'attaquant | `HpLost` de cause `Reflect` | Rend compréhensibles les renvois de dégâts | 2 |
| **Déviation** : le rayon bifurque vers la nouvelle cible, animation dédiée (ARB-88) | `AttackRedirected` | Distingue la déviation de l'esquive | 2 |
| **Halos de statut** : un halo discret par famille d'effet (protégé, bouclier désactivé…) | `StatusAdded`, `StatusEnded`, état | En attendant les icônes des effets temporaires, se voit de loin | 3 |
| **Vaisseau endommagé** : fumée, étincelles quand les PV sont bas | État : PV sous un seuil | Montre qui est en danger sans lire les chiffres | 2 |
| **Tour en cours** : réacteurs plus vifs, ou un faisceau sur le vaisseau actif | `TurnStarted` | Complète le cadre de la fiche | 3 |
| **Fin des temps** : onde de choc qui balaie la table | `EventRevealed` de la Fin des temps (par l'effet de fond du catalogue) | Événement unique et dramatique | 2 |
| **Victoire** : la caméra tourne autour du vainqueur ; l'Élection a son propre effet | `GameOver` (type de victoire) | Clôture la partie | 3 |
| **Initiative** : chaque vaisseau lance son d8 | `InitiativeRolled` | Ouvre la partie avec le modèle de dé | 3 |
| **Modèles** : jetons de Tourment et de surcharge en 3D (ASSETS §3 les prévoit en 2D), épave dédiée, d8 | — | Cohérence avec les cartes et les vaisseaux 3D | 2 |

## 4. Ordre proposé

- **Lot 1**, le plus visible pour le moins d'effort : balancement, rayon d'attaque, recul aux dégâts, flambée de combo, d8 qui tourne, élimination. Aucun ajout au moteur. **Fait côté jeu**, avec des effets provisoires (§5) ; restent les modèles et les effets définitifs.
- **Lot 2** : parade et esquive (après l'ajout au moteur), Tourment, fond et ses effets, paquets du marché, critique, surcharge, sabotage, trajets des cartes, dégâts renvoyés, déviation, fin des temps, vaisseau endommagé.
- **Lot 3** : contamination, réparation, halos, tour en cours, victoire, initiative.

Chaque lot se fait des deux côtés : les modèles et effets par l'atelier Blender, les retours visuels et les ajouts au moteur par le développement. Un retour visuel sans son modèle utilise un effet provisoire, comme les cartes et les vaisseaux l'ont fait.

## 5. Lot 1 : ce qui est en place

Chaque animation marche déjà avec un effet provisoire fait de formes simples, sans matériau ni texture, comme le vaisseau provisoire. Un effet définitif le remplace sans code : il suffit de le déposer dans le retour visuel correspondant (`Presentation/Feedback/`), ou dans le thème pour le dé.

| Animation | Retour visuel ou composant | Événement | Pour la remplacer |
|---|---|---|---|
| Balancement | `ShipMotion`, ajouté à chaque vaisseau | État permanent | Réglages *Animations* de `Theme/ThemeSettings` (hauteur, roulis, tangage, durée). Seules les pièces du vaisseau bougent, sous un enfant `Mouvement` : la fiche qui suit le vaisseau ne tremble pas |
| Rayon d'attaque et recul | `Beam.asset` (`BeamFeedback`) | `AttackResolved`, après les dés : le rayon va vers la cible finale | Champ *Prefab* : un rayon dont l'axe avant fait une unité de long ; le jeu l'étire entre les deux vaisseaux. L'épaisseur du rayon provisoire suit la force de l'attaque |
| Recul aux dégâts | `Knockback.asset` (`KnockbackFeedback`) | `HpLost` d'une attaque ou d'un renvoi | Champ *Sparks* : les étincelles de l'impact. Le recul se règle (par point de PV, maximum, durées) |
| Flambée des réacteurs | `Thrusters.asset` (`ThrusterFeedback`) | `TechnologyActivated` | Champ *Prefab* : la flamme, orientée vers l'arrière, posée sur chaque réacteur |
| Explosion | `Explosion.asset` (`ExplosionFeedback`) | `PlayerEliminated` | Champ *Prefab* : l'explosion. L'épave dérive ensuite lentement |
| Dé à 8 faces | `DiceTray` et `DieSpinner` | `DiceRolled`, `DieRolled` | Champ *Die Model* de `Theme/ThemeSettings`. Sans modèle, un octaèdre généré |

**Conventions pour les modèles** (à reporter dans [`ASSETS.md`](ASSETS.md) par l'atelier Blender) :
- **Vaisseau** : un repère vide `Canon` là où part un tir, et un repère vide `Reacteur…` (`Reacteur_Gauche`, `Reacteur_Droit`…) au bout de chaque réacteur. Sans eux, le tir part devant le nez et la flamme derrière la queue.
- **Dé à 8 faces** : environ une unité de haut ; huit repères vides `Face_1` à `Face_8`, un par face, dont l'axe avant sort de la face et l'axe haut pointe vers le haut du chiffre. Les faces opposées font 9, comme sur un vrai d8. Le jeu tourne la face tirée vers la caméra, droite.
