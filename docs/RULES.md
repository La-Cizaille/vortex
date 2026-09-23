# Vortex : règles consolidées

> **Statut** : v0.1, en attente de validation par le game designer.
> **Sources** : `design/Vortex.xlsx` (onglets *Déroulement du jeu*, *Modificateurs attaque*, *Modificateurs défense*, *Evènements*, *Technologies*), complété par les arbitrages pris en session.
> **Rôle de ce document** : c'est la **référence du moteur de règles**. Chaque comportement codé dans `core/` renvoie à une section d'ici (§x.y). Toute modification de règle commence par ce fichier.

Les valeurs chiffrées marquées ⚙ sont **configurables** (`GameConfig`) et seront calibrées par le simulateur (jalon M3).

---

## 1. Lexique

| Terme | Définition |
|---|---|
| **Tour** | Le tour d'**un** joueur. |
| **Manche** | Un tour de table complet. Dans le texte d'un **événement**, « pendant ce tour » désigne la **manche**. |
| **Prochain tour / tour suivant** | Dans une carte : le prochain tour **du joueur concerné**. |
| **Dégâts** | PV perdus à cause d'une attaque ou d'un effet d'attaque. |
| **Perte de PV** | Tout PV perdu, quelle que soit la cause. Chaque perte porte une **source** : `Attack`, `Reflect` (renvoi, ex. Loi du Talion), `Torment`, `Event`, `Self` (coût payé par le joueur ou effet sur soi, ex. D_002, D_020). |
| **Bouclier (BOU)** | Valeur de 0 à 8 ⚙. Les cartes peuvent l'amener à 0. |
| **Bouclier désactivé** | Compte pour 0 dans le calcul des dégâts, mais sa valeur est **conservée**. |
| **Neutre** | Couleur sans technologie. Une carte neutre ne participe **jamais** à un combo. |
| **Technologie** | Les couleurs Bleu (Ordre), Jaune (Casino Cosmique), Rouge (Rebelles) et Vert (Abomination organique). |

**Règle anti-boucle** : une perte de PV de source `Reflect` ne déclenche **aucun** effet réactif (Loi du Talion, Chance de cocu, Vautours, etc.).

**Surcharge et dégâts** : le jeton de surcharge est perdu sur une perte de PV `Attack`, `Reflect` ou `Event`. Il est conservé sur `Torment` et `Self`.

---

## 2. Matériel

| Élément | Quantité |
|---|---|
| Joueurs | 2 à 5 |
| Modificateurs ATK | 27 cartes différentes, **54 exemplaires** |
| Modificateurs DEF | 27 cartes différentes, **50 exemplaires** (D_006, D_017, D_022 et D_027 en 1 exemplaire) |
| Événements | 8 cartes différentes, 15 exemplaires, dont 1 « Fin des temps » **retirée du paquet** (§4) |
| Technologies | 4 combos, un par couleur |
| Dé | d8 |

---

## 3. Mise en place

1. Chaque vaisseau commence avec **30 PV** ⚙ (maximum 30 ⚙).
2. Tous les joueurs commencent avec le **même bouclier** : `StartShield[n]` ⚙, qui dépend du nombre de joueurs n (valeur provisoire : 4).
3. On mélange séparément les paquets ATK, DEF et Événements. On révèle **5** ⚙ cartes de chaque paquet de modificateurs : ce sont les deux **marchés noirs**.
4. **Initiative** : chaque joueur lance 1d8. Le plus haut commence. En cas d'égalité, seuls les joueurs à égalité relancent.
5. On joue ensuite dans le **sens horaire**, selon l'ordre des sièges.

---

## 4. Début de manche

1. Si `EventFrequency` ⚙ le prévoit (valeur provisoire : à chaque manche), on tire **un événement**. Son effet dure jusqu'au début de la manche suivante.
2. À la manche `DoomRound[n]` ⚙ (valeur provisoire : 10), la **Fin des temps** se déclenche **à la place** de l'événement :
   - tous les modificateurs **équipés** sont défaussés ;
   - tous les boucliers passent à 0.
   C'est un effet ponctuel : les boucliers peuvent ensuite être reconstruits.
3. Si le paquet d'événements est vide, on mélange sa défausse pour le reconstituer.
4. Si le premier joueur est éliminé, la manche commence au joueur vivant suivant.

---

## 5. Tour d'un joueur

### 5.1 Début du tour
1. Le joueur perd **1 PV par jeton de Tourment** posé sur ses modificateurs. Chaque combo Abomination ajoute +1 par jeton (§8). Source : `Torment`.
2. Les effets « au début de votre tour » s'appliquent, emplacement ATK puis DEF.
3. Les effets qui duraient « jusqu'à votre prochain tour » expirent.

### 5.2 Marché noir : une seule option parmi
- **Prendre** 1 carte d'un marché (2 cartes pendant *Nouvel arrivage*).
  - La carte va dans l'emplacement correspondant (ATK ou DEF).
  - L'ancienne carte de cet emplacement est défaussée avec ses jetons. Si c'était une carte à usage unique, son effet **n'est pas** déclenché.
  - Le marché est complété immédiatement.
  - Les jetons de Tourment présents sur la carte prise **la suivent**, sans perte de PV immédiate.
- **Recycler** un marché : ses cartes sont défaussées et on en révèle 5 nouvelles.
- **Passer**.

Quand un paquet est vide, on mélange sa défausse. Si le paquet et la défausse sont vides, le marché reste incomplet.

### 5.3 Fenêtre d'activation
Elle s'ouvre **après** le marché noir et reste ouverte **jusqu'à la fin du tour** (avant comme après l'action d'équipage). Le joueur peut y activer **tout ce qu'il possède et qui est activable**, sans limite de nombre :
- cartes à usage unique (défaussées après usage) ;
- cartes à déclenchement manuel (D_011) ;
- combos (§8).

Un effet « lors de votre prochaine attaque » est **perdu** à la fin du tour s'il n'y a pas eu d'attaque.

### 5.4 Action d'équipage
**Une** action au choix, ou passer. Avec *Casino Cosmique*, deux actions **différentes**.

| Action | Effet |
|---|---|
| **Attaque** | §6 |
| **Reparamétrage du bouclier** | Relance 1d8, qui remplace le bouclier actuel. En consommant la surcharge : 2d8, somme plafonnée à 8. |
| **Sabotage** | Relance 1d8, qui remplace le bouclier d'un adversaire. Impossible contre *Intouchable*. La surcharge ne s'applique pas. |
| **Surcharge** | Gagne un jeton de surcharge (maximum 1 ⚙). |

### 5.5 Fin du tour
Les effets « pendant votre tour » expirent, puis on vérifie les conditions de victoire (§9).

---

## 6. Résolution d'une attaque (ordre exact)

1. **Choix de la cible** : un adversaire vivant.
   - Interdit si *Favoritisme* protège la cible contre cet attaquant.
   - Si *Mutinerie* s'applique, l'action et la cible sont imposées.
2. **Déviation** : si la cible a *Ordre* actif, elle peut rediriger l'attaque vers un autre joueur vivant, qui ne peut être ni l'attaquant ni elle-même. L'attaque est alors résolue entièrement contre la nouvelle cible, sans nouvelle déviation possible.
3. **Avant le jet** :
   - *Canon à particules* vole 2 points de bouclier (le bouclier de l'attaquant reste ≤ 8). Bloqué par *Intouchable*.
   - *Black Jack* : l'attaquant annonce une valeur de 1 à 8.
4. **Jet** :
   - Le lot de base est **1d8**. On ajoute **+1d8** si l'attaque est surchargée (jeton consommé ou *On envoie la sauce*).
   - *Tapis!* : 3d8, on garde les 2 meilleurs.
   - **Avantage** : on lance le lot deux fois et on garde le meilleur total. **Désavantage** : on garde le pire. Les deux **s'annulent**.
   - *Main sûre* : le dé lancé à l'avance tient lieu de premier dé.
5. **Critique** : au moins un dé conservé affiche 8.
6. **Valeur d'attaque** = somme des dés conservés + bonus. Les bonus s'ajoutent **avant** le bouclier.
7. **Bouclier effectif** = bouclier de la cible, ramené à 0 s'il est désactivé ou ignoré.
8. **Dégâts** = max(0, valeur d'attaque − bouclier effectif).
9. **Multiplicateurs** : ×2 pour chaque effet actif. Ils se cumulent.
10. **Critique** : si la cible a au moins un modificateur, **elle choisit** celui qui est détruit (avec ses jetons). Sinon, elle subit +1 dégât. Les dégâts normaux s'appliquent dans les deux cas.
11. **Protections de la cible**, dans cet ordre :
    - *T'as pas entendu un truc?* ;
    - *Sous-couche blindée* ;
    - *Lève la tête* ;
    - *Chance de cocu* ;
    - *Niaque*, qui s'applique en dernier, sur le montant final.
12. **Application** : la cible perd les PV (source `Attack`). Si le montant est > 0, elle perd sa surcharge, sauf *Nothing else matters*.
13. **Effets d'après-coup**, résolus dans l'ordre des sièges à partir de l'attaquant.
14. **Éliminations**, puis *Le grand final*, puis vérification de la victoire.

**Exemple** :
- L'attaquant a *Rétablir l'ordre* (+4). Le dé fait 5. Le bouclier de la cible vaut 6.
- Valeur d'attaque : 5 + 4 = 9. Dégâts : max(0, 9 − 6) = **3**.

---

## 7. Tourment

- **Poser un jeton** : il est placé sur un modificateur de la cible, et la cible perd 1 PV (source `Torment`).
- **Cible sans modificateur** : **aucun** jeton n'est posé et elle ne perd aucun PV.
- Les jetons se cumulent. Ils disparaissent quand le modificateur est remplacé, détruit ou défaussé. Ils **suivent** la carte si elle est volée ou échangée.
- **Réactiver les Tourments** : chaque joueur perd de nouveau 1 PV (+ bonus Abomination) par jeton posé sur ses modificateurs. Les jetons posés sur les cartes des marchés n'infligent rien.

---

## 8. Technologies (combos)

- **Condition** : le modificateur ATK et le modificateur DEF équipés sont de la **même couleur non neutre**.
- **Activation** (fenêtre §5.3) : les deux cartes sont défaussées (leurs effets d'usage unique ne sont pas déclenchés). L'effet du combo s'applique, et la technologie est marquée comme **obtenue** par ce joueur.

| Couleur | Nom | Effet |
|---|---|---|
| Bleu | Ordre | Jusqu'au début de votre prochain tour, vous pouvez dévier les attaques subies (§6.2). |
| Jaune | Casino Cosmique | Ce tour-ci, vous effectuez 2 actions d'équipage **différentes**. |
| Rouge | Rebelles | Vous gagnez un jeton de surcharge. Votre prochaine attaque surchargée se fait avec avantage. |
| Vert | Abomination organique | Les Tourments infligent +1 point pour le reste de la partie (cumulable). Puis tous les Tourments en jeu sont réactivés. |

**Synergie technologique** (bonus passif) : **désactivée** dans cette version. Le moteur prévoit un point d'extension.

---

## 9. Victoire

- **Domination** : être le dernier vaisseau en vie.
- **Élection galactique** : avoir obtenu les **4** technologies. La victoire est immédiate.
- **Égalité** : tous les joueurs restants sont éliminés simultanément.
- **Élimination** : un vaisseau à 0 PV est éliminé. Ses cartes sont défaussées avec leurs jetons.

---

## 10. Arbitrages carte par carte

Le texte des cartes fait foi. Ce tableau précise uniquement **ce que le texte ne tranche pas**. Chaque ligne est implémentée dans la classe de la carte, avec un commentaire qui renvoie ici.

### 10.1 Modificateurs d'attaque
| ID | Nom | Arbitrage |
|---|---|---|
| A_001 | Canon à particules | Vol de 2 points avant le jet. Si la cible a moins de 2, on vole ce qu'elle a. Bloqué par Intouchable. |
| A_002 | Langue de bois | Cible choisie à l'activation. Son bouclier est désactivé jusqu'à la fin de mon tour. |
| A_003 | Épuration | « Changer » = relancer 1d8 le bouclier choisi. Bloqué par Intouchable. |
| A_004 | La paix a un prix | Je choisis X ≤ mon bouclier. Mon bouclier baisse de X, et ma prochaine attaque de ce tour gagne +X. |
| A_005 | Rétablir l'ordre | +4 à la valeur d'attaque, avant le bouclier. |
| A_006 | Grosse Bertha | ×2 sur les dégâts de la prochaine attaque de ce tour. |
| A_007 | Délestage forcé | Défausse les 2 modificateurs de la cible, avec leurs jetons. |
| A_008 | Brocante spatiale | Recycle le marché ATK, puis je prends une carte dans le nouveau marché. Brocante est défaussée. |
| A_009 | Jamais deux sans trois | Je récupère les 2 modificateurs de la cible (avec leurs jetons) dans mes emplacements. Mes cartes sont défaussées. |
| A_010 | Le grand final | Si mon attaque élimine un vaisseau, tous les autres joueurs perdent leurs modificateurs et leur bouclier passe à 0. La carte est ensuite défaussée. |
| A_011 | Cruauté | +4 PV si les dégâts sont > 0. |
| A_012 | Vindicte populaire | +1 par carte **neutre** visible dans les deux marchés, compté à l'attaque. |
| A_013 | On envoie la sauce | La prochaine attaque de ce tour est surchargée, sans consommer de jeton. |
| A_014 | Recels en tous genres | Si les dégâts sont > 0, je défausse 1 modificateur de la cible (à mon choix). |
| A_015 | Racket | Je choisis de voler ou de détruire. Une carte volée remplace la mienne dans l'emplacement correspondant. |
| A_016 | BLITZKRIEG! | Mes attaques surchargées ignorent le bouclier. |
| A_017 | Dingo de la surcharge | +4 sur mes attaques surchargées. |
| A_018 | Appendice laser | À chaque attaque, 2 jetons répartis à mon choix sur les modificateurs de la cible. |
| A_019 | Agents pathogènes | +4 si au moins un modificateur de la cible porte un Tourment. |
| A_020 | Spores corrosifs | Si les dégâts sont > 0, 5 jetons sur 5 cartes différentes des marchés (à mon choix). |
| A_021 | Accident bactériologique | 1 jeton par modificateur, pour la cible et ses deux voisins de siège (moi exclu). |
| A_022 | Réseau fongique | Réactivation des Tourments (§7). |
| A_023 | Pile ou face | Somme des dés conservés paire : +3. Impaire : −1. |
| A_024 | Black Jack | Annonce avant le jet. ×2 si un dé conservé est égal à la valeur annoncée. |
| A_025 | Corruption du croupier | Lors de ma prochaine attaque de ce tour : j'échange 1 modificateur de la cible avec la carte du même emplacement chez un autre joueur ou dans le marché correspondant. |
| A_026 | Carte sous l'coude | Avantage sur mes attaques. |
| A_027 | Tapis! | Je défausse mes 2 modificateurs, puis ma prochaine attaque de ce tour est surchargée avec 3d8, dont on garde les 2 meilleurs. Mon jeton n'est pas consommé. |

### 10.2 Modificateurs de défense
| ID | Nom | Arbitrage |
|---|---|---|
| D_001 | Intouchable | Bloque les modifications de valeur par un adversaire : sabotage, vol, échange, Roulette, Épuration, plafond D_005. Ne bloque **pas** les désactivations, les ignorances ni les événements. |
| D_002 | Dommage collatéral | Je convertis X PV en X points de bouclier. On ne peut pas descendre à 0 PV ni dépasser un bouclier de 8. Source `Self`. |
| D_003 | Ni vu ni connu | Échange les boucliers de 2 vaisseaux au choix (Intouchable protège). |
| D_004 | Favoritisme | Après une attaque subie, cet attaquant ne peut pas me cibler à son prochain tour. |
| D_005 | Sabotage électoral | Ennemi choisi à l'équipement. Son bouclier est plafonné à (mon bouclier − 2), minimum 0, recalculé en continu. |
| D_006 | Mutinerie syndicale | Si je subis des dégâts d'une attaque : je choisis l'action d'équipage de l'attaquant et ses cibles pour son prochain tour. Si c'est impossible, il passe. |
| D_007 | Sous-couche blindée | 1 dégât maximum par attaque, sauf attaque surchargée. Une attaque surchargée défausse la carte. |
| D_008 | T'as pas entendu un truc? | La prochaine attaque subie inflige 0 dégât. La carte est ensuite défaussée. |
| D_009 | Les affaires sont les affaires | Comme A_008, sur le marché DEF. |
| D_010 | Niaque | Si une attaque devait m'éliminer, elle inflige 0 dégât. La carte est ensuite défaussée. |
| D_011 | Générateur auxiliaire | Activation **manuelle** (§5.3) : mon bouclier passe à 8, puis la carte est défaussée. |
| D_012 | Vente de pièces détachées | +1 PV par carte neutre visible dans les marchés. |
| D_013 | Vautours | +1 PV pour chaque instance de dégâts d'un joueur à un autre. Pas pour la victime, et pas pour les sources `Torment` ou `Reflect`. |
| D_014 | Nothing else matters | Je ne peux pas perdre ma surcharge jusqu'au début de mon prochain tour. |
| D_015 | Lève la tête, bombe le torse | Si PV ≤ 10 : 1 dégât maximum par attaque, sauf attaque surchargée. |
| D_016 | Orgueil | +15 PV (plafonnés au maximum). Mon bouclier est désactivé jusqu'au début de mon prochain tour. |
| D_017 | Loi du Talion | Si je perds des PV sur une attaque, l'attaquant perd le même nombre de PV (source `Reflect`). |
| D_018 | Régénération parasitaire | +1 PV pour chaque PV que **je** perds à cause d'un Tourment. |
| D_019 | Mimétisme cellulaire | À partir de mon prochain tour, à chaque début de tour, mon bouclier copie le plus élevé des **autres** joueurs. |
| D_020 | Tout est une question d'équilibre | En début de tour : −3 PV si PV ≥ 10 (source `Self`), +3 PV si PV < 10. |
| D_021 | Quarantaine obligatoire | Retire **tous** les jetons (joueurs et marchés). +3 PV par jeton retiré. |
| D_022 | Je te touche pas avec un bâton | Si je subis des dégâts d'une attaque, je pose 1 jeton sur un modificateur de l'attaquant (à mon choix). |
| D_023 | Main sûre | Au début de ma phase d'équipage, je lance 1d8. Ce dé sert de premier dé pour l'action choisie (attaque, reparamétrage ou sabotage). |
| D_024 | Roulette | Tous les boucliers tournent d'un siège, dans le sens que je choisis. Un joueur Intouchable garde le sien, et on le saute. |
| D_025 | Chance de cocu | Quand je perds des PV (hors `Torment` et `Reflect`) : 1d8. Pair : 0 perte. Impair : +3. |
| D_026 | Le casino gagne toujours | Désavantage sur les attaques que je subis. |
| D_027 | La banque | À partir de mon prochain tour, à chaque début de tour : bouclier +2 (maximum 8). |

### 10.3 Événements
| Nom | Arbitrage |
|---|---|
| Tempête électromagnétique | Tous les boucliers sont désactivés pendant la manche. |
| Trou noir | Tous les modificateurs **équipés** sont défaussés. Les marchés ne changent pas. |
| Nouvel arrivage | Les deux marchés sont recyclés. Pendant la manche, chaque joueur peut prendre 2 cartes, quels que soient les marchés. |
| Surcharge ionique | +4 à la valeur de toutes les attaques pendant la manche. |
| Nuée parasitaire | 1 jeton sur chaque modificateur équipé de chaque joueur (1 PV perdu par jeton). |
| Espace aseptisé | Tous les jetons portés par les joueurs sont retirés. |
| Le calme avant la tempête | Aucun effet. |
| Fin des temps | Hors paquet. Voir §4.2. |

---

## 11. Questions ouvertes (à trancher par le game designer)

- Valeurs de `StartShield[n]` et `DoomRound[n]` : elles seront proposées par le simulateur (M3).
- Fréquence des événements : une par manche, jugée potentiellement excessive. À mesurer au M3.
- Cartes marquées « x » dans le fichier (A_003, A_010, A_015, A_021, A_022) : implémentées telles quelles, mais **à revoir**.
- Effets de la synergie technologique : à définir.
