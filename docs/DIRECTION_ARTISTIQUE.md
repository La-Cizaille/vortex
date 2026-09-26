# Direction artistique et univers

Ce document est la **bible** de l'apparence et du ton du jeu. Tout visuel, tout texte d'interface, toute animation et tout son s'y réfère.

- Décision de fond : [ADR-0020](adr/0020-direction-artistique.md) ; choix du designer : ARB-93.
- Liens avec le reste :
  - la liste des visuels et leurs formats reste dans [`ASSETS.md`](ASSETS.md) ;
  - les animations dans [`ANIMATIONS.md`](ANIMATIONS.md) ;
  - l'écran dans [`INTERFACE.md`](INTERFACE.md).
- Cette direction ne change **ni les règles ni le moteur**. Elle passe par le thème, la table des textes, les profils d'animation et les modèles (ADR-0007, ADR-0014, ADR-0015).

**Statut : v1 validée** sur trois planches de tendance (ARB-93 à ARB-96). Les points encore ouverts sont marqués **[à valider]**.

## 1. Pitch et piliers

> **Le dernier grand casse avant la fin des temps.**
> Un vortex avale peu à peu la Marge, un secteur oublié de la galaxie. Ses capitaines pirates ont encore quelques manches pour piller, trahir, et se faire élire patron de ce qui reste, avant que tout soit aspiré.

**Piliers**
1. **Fun d'abord.** Chaque coup se lit, se sent et se paie tout de suite. L'humour accompagne le jeu sans jamais le ralentir.
2. **Un monde sombre, une voix cruelle.** L'univers est pris au sérieux : sombre, lourd, sans issue, jamais cartoon. L'humour vient du **sarcasme froid** posé sur ce monde, pas de gags visuels. **L'image est grave, le texte est cruel** : c'est ce décalage qui fait rire (ARB-94). On ne rit jamais aux dépens du joueur réel ni de personnes réelles (§5.2).
3. **Technologie de récupération.** Tout est machine : câbles, valves, tuyaux, circuits, écrans, voyants. Des vaisseaux et des modules rafistolés, branchés les uns sur les autres, qui tiennent par le bricolage (ARB-95).
4. **Pirates en sursis.** Tout est usé, rafistolé, volé. Les équipages sont épuisés, les coques ont déjà servi à d'autres morts. Rien n'est neuf, rien n'est garanti, personne n'est sauvé.
5. **Satire électorale.** L'Élection galactique est une campagne : affiches, promesses, pots-de-vin, votes achetés.
6. **Lisible avant tout.** Une information de jeu reste nette dans n'importe quelle ambiance : la blague vit **à côté** de l'information, jamais à sa place (§5.3).

## 2. L'univers

### 2.1 La Marge
- Un secteur en bordure de carte, abandonné par les grandes puissances le jour où le **Vortex** est apparu : un trou noir errant qui se rapproche à chaque manche.
- Il ne reste que des pirates, des marchands sans scrupules, et quatre factions qui se disputent les restes. L'une d'elles, la Légion de l'Ordre, ne rit jamais : elle sert de contrepoint sérieux aux trois autres.
- La loi du secteur tient en une phrase : *tout ce qui n'est pas cloué appartient au plus rapide ; tout ce qui est cloué aussi.*

### 2.2 Les capitaines (les joueurs)
- Chacun commande un rafiot rapiécé, peint aux couleurs de son siège.
- Ils combattent pour deux raisons :
  - **survivre** : c'est la victoire par **Domination**, être le dernier en vol ;
  - **se faire élire Grand Électeur de la Marge** : c'est la victoire par l'**Élection galactique**, en ralliant les quatre factions.
- L'ironie de la situation est assumée : le vainqueur de l'élection règne sur un secteur promis au vortex.

### 2.3 Le marché noir
- Le seul commerce encore ouvert. On y achète des modificateurs volés, démontés sur des épaves, ou « tombés d'un cargo ».
- Sa devise : *Garantie : aucune. Remboursement : jamais.*

### 2.4 Le Vortex et la Fin des temps
- Le compteur « Fin des temps dans N manches » est **l'approche du vortex**.
- Quand il passe, il arrache tous les modificateurs et souffle tous les boucliers. La partie continue dans ce qu'il reste.

### 2.5 Les épaves
- Un capitaine éliminé laisse une épave qui dérive. Avec l'option des fantômes, son équipage mort continue de se mêler de tout.

### 2.6 La voix du secteur : SINISTRA
- **Qui** : l'IA d'expertise des **Assurances Horizon Final**, dont le slogan est *« Nous couvrons tout. Sauf vous. »*
- **Son rôle** : elle commente la partie dans le journal, les annonces de tour et les épitaphes. Elle estime chaque dommage, classe chaque dossier, et ne rembourse jamais.
- **Son ton** : d'une politesse glaciale, elle vouvoie, avec un vocabulaire d'assureur (sinistre, franchise, dossier classé, clause d'exclusion). Elle ne plaisante jamais : elle constate. Le rire vient de son indifférence totale à la mort des équipages.
- **Pourquoi elle** : un narrateur unique donne une voix constante au jeu et concentre l'humour à un seul endroit, qu'on peut couper (§5.4).
- **Elle aura une voix** (ARB-96), avec le lot son (§6.8) : en attendant, elle parle en texte.

## 3. Les factions

Les quatre technologies deviennent quatre factions, **et le jeu affiche le nom de la faction** à la place de celui de la technologie (ARB-95). Obtenir une technologie, c'est **obtenir le soutien d'une faction** pour l'élection. Un combo est **un pacte** avec elle.

| Technologie (couleur) | Faction | Qui ils sont | Visuel | Ton | Lien avec la mécanique |
|---|---|---|---|---|---|
| **Ordre** (bleu) | **La Légion de l'Ordre** | Des fantassins d'élite en armure lourde, dernière force régulière de la Marge. Disciplinés, fanatiques, sans le moindre humour : ce sont eux, les pas rigolos. | Armures lourdes, visières, bannières, rangs serrés. Emblème : un éclair | **Sérieux, martial**. L'humour vient de ce qui les entoure, jamais d'eux | Dévier une attaque : interceptée et redirigée, « conformément au protocole » |
| **Casino Cosmique** (jaune) | **La Maison** | La pègre du jeu et de l'usure. Elle possède les dettes du secteur, et se rembourse en pièces détachées, mécaniques ou non. | Salles enfumées, dorures ternies, néons malades, jetons, tables de jeu. Emblème : une pièce | Mielleux, menaçant | Deux actions d'équipage : **« double mise »** |
| **Rebelles** (rouge) | **Le Front Syndical des Mutins** | Des équipages mutinés qui ont pendu leurs officiers et votent tout à main levée, y compris les exécutions. | Pochoirs rouges, drapeaux déchirés, barricades de ferraille. Emblème : un poing levé | Militant, fanatique | Surcharge et avantage : **« mouvement social »** |
| **Abomination organique** (vert) | **Les Adeptes de la Corruption** | Un culte fongique. La contamination y est une communion ; les fidèles ne sont plus tout à fait des gens, ni tout à fait vivants. | Chair, mycélium, bioluminescence verdâtre, coques colonisées, horreur organique suggérée. Emblème : un champignon | Doucereux, inquiétant | Tourment réactivé : **« grande communion »** |

Les couleurs des technologies restent celles du thème : les joueurs les connaissent déjà, et chaque faction a en plus son emblème (§6.6), qui ne repose pas sur la couleur seule.

## 4. De la mécanique à la fiction

Les **mots des règles ne changent pas** (PV, bouclier, surcharge, Tourment…) : ils sont courts, connus des joueurs, et apparaissent dans les textes des cartes. La fiction les habille seulement dans les textes d'ambiance.

| Règle | Dans l'univers |
|---|---|
| PV | L'intégrité de la coque |
| Bouclier, reparamétrage | Le déflecteur, qu'on recalibre à coups de clé |
| Sabotage | Le bidouillage du déflecteur d'un autre, à distance |
| Surcharge | Pousser le réacteur au-delà de la garantie constructeur |
| Tourment | L'infestation par les spores des Adeptes |
| Marché noir, recycler | Le marché noir ; « on vide l'étal » |
| Modificateur (carte) | Un **module** de vaisseau récupéré, qu'on branche dans une baie (ATK ou DEF) |
| Technologie, combo | Le soutien d'une faction ; un pacte |
| Manche | Un tour d'horloge avant le vortex |
| Dé | Le dé du destin : un d8 de casino, forcément pipé |
| Élimination | Un sinistre, que SINISTRA classe |

## 5. Charte d'écriture

### 5.1 Le ton
- **Sec, court, glacial.** La chute tombe à la fin de la phrase. Une blague qu'il faut expliquer est retirée.
- **Sombre** : l'humour est noir (mort, cupidité, absurdité administrative, cruauté ordinaire). Pas de clin d'œil, pas de gag, pas de ton bon enfant : on constate l'horreur avec indifférence, et c'est cette indifférence qui fait rire.
- **Adulte**, pas vulgaire pour rien. Un juron bien placé reste permis.
- **Français d'abord**, avec les mots du milieu : jargon de casino, de syndicat, d'assurance, de piraterie.
- On garde l'esprit des noms de cartes actuels (Langue de bois, Chance de cocu, Vautours…) : jeux de mots, expressions détournées, références populaires.

### 5.2 Les limites (validées, ARB-93)
- **Oui** : humour noir sur la mort, la cupidité, la bureaucratie, la religion et la politique **fictives** du secteur ; violence **sérieuse et pesante** (coques éventrées, équipages perdus, horreur organique **suggérée**), sans gore détaillé.
- **Non** :
  - aucune personne réelle, aucun parti réel, aucune religion réelle ;
  - aucune blague qui vise un groupe de personnes (origine, genre, orientation, handicap…) ;
  - aucun contenu sexuel explicite.
- Ces limites visent aussi une classification d'âge raisonnable sur les boutiques (PEGI 16 probable avec ce ton).

### 5.3 Où va l'humour
| Texte | Registre | Règle |
|---|---|---|
| Texte de règle d'une carte, règles, aperçus, raisons d'un refus, questions d'une décision | **Neutre et exact** | Aucune blague : c'est ce que le joueur lit pour décider |
| Libellés de l'interface (boutons, menus) | **Clair**, touche d'ambiance tolérée dans les titres | Le mot d'action reste évident (« Fin de tour », pas un jeu de mots) |
| Nom d'une carte, d'un événement, d'une faction | **Ambiance** | Voir §8 pour les renommages |
| Texte d'ambiance d'une carte | **Ambiance** | Une ligne sous le texte de règle, facultative. Écrite par le designer dans un second temps (ARB-95) ; il faudra un champ de plus dans le contenu |
| Journal, annonces de tour, épitaphes, astuces | **La voix de SINISTRA** | Le fait d'abord (qui, quoi, combien), la pique ensuite, courte |

### 5.4 La voix de SINISTRA
- Une ligne du journal garde **le fait lisible d'un coup d'œil**. La pique est un suffixe court, tiré au hasard parmi plusieurs variantes, pour ne pas lasser.
- Une option **« Commentaires de SINISTRA »** permet de les couper (ARB-95), texte et voix ensemble.
- Exemples (à écrire pour de bon avec la revue des textes) :
  - Attaque : « Rouge perfore Bleu : 6 dégâts. *Franchise non applicable.* »
  - Coup critique : « Critique ! Bleu perd Sous-couche blindée. *Nos experts parlent de perte totale.* »
  - Élimination : « Le vaisseau de Vert est déclaré épave. *Dossier classé. Merci de votre fidélité.* »
  - Annonce de tour : « À vous, capitaine Jaune. *Votre contrat expire à votre décès. Probablement bientôt.* »
  - Marché noir recyclé : « Rouge vide l'étal. *Le vendeur n'a pas survécu à la négociation.* »
  - Approche du vortex : « Le vortex passe. *Il ne rembourse pas non plus.* »
  - Épitaphe de fin de partie : « Ci-gît Bleu. Il avait un bouclier et des principes. Il n'a plus ni l'un ni l'autre. »
  - Astuce : « Un bouclier désactivé protège exactement autant qu'une prière. »

## 6. Direction visuelle

Deux registres, choisis par le designer (ARB-93), assombris après la première planche (ARB-94) :
- **A, pour la 3D : l'industriel usé et sombre.** Coques lourdes et cabossées, tôle brute, rouille, suie, lumières rares. Réaliste dans les proportions et le poids, **jamais cartoon**.
- **C, pour la 2D : la propagande autoritaire.** Affiches en deux ou trois couleurs (noir, os, rouge sang), photocopies dégradées, tampons, avis de recherche. Le graphisme d'un régime, pas celui d'une bande dessinée.

Ils se rejoignent dans le cockpit et les menus : du métal noirci couvert d'affiches arrachées.

**Une couche technologique partout** (ARB-95) : câbles, gaines, valves, manomètres, tuyaux, circuits imprimés, connecteurs, voyants, écrans cathodiques. Rien n'est lisse : chaque surface montre comment elle est branchée.

**Ce qui fait rire n'est jamais l'image.** Les visuels et les animations restent graves ; le sarcasme vient des textes (§5).

### 6.1 Palette
| Rôle | Couleurs | Usage |
|---|---|---|
| **Fond** | Suie `#0E0F11`, graisse `#17181B`, tôle `#3E4347`, rouille `#6E3A22` | Coques, cockpit, panneaux, ciel |
| **Papier** | Os `#CFC4A8` (papier jauni), encre `#141312` | Menus, affiches, cartes à texte, tampons |
| **Sang** | `#8E1B1B` | Affiches, tampons, verdicts |
| **Lumières** (rares) | Ambre sale `#E0922F`, vert terminal `#4DFF7A`, néon malade `#C23A6B` | Réacteurs, écrans de bord, enseignes de la Maison |
| **Danger** | Jaune `#D9A514` sur noir, en bandes usées | Surcharge, avertissements, vortex |
| **Factions** | Les couleurs des technologies, un peu désaturées sur les décors (bleu, jaune, rouge, vert) | Cartes, emblèmes, diodes |
| **Sièges** | Les cinq couleurs actuelles | Peinture des vaisseaux, liserés |
| **Retours de jeu** | Perte (rouge), soin (vert), surcharge (ambre), comme aujourd'hui | Chiffres, jauges, effets |

Règles :
- **Éclairage bas** : de grandes zones d'ombre, des sources de lumière rares et justifiées. Ce qui brille attire l'œil parce que tout le reste est sombre.
- Les informations de jeu (chiffres, cibles, sélection) gardent un contraste franc : l'ambiance ne les noie jamais.
- Les couleurs des sièges et des factions ne changent pas sans vérifier leur contraste entre elles et sur le nouveau fond.
- Une information ne passe jamais par la couleur seule : forme, emblème, motif ou texte l'accompagnent (daltonisme).

### 6.2 Typographies **[à valider sur la planche]**
Polices libres (licence SIL OFL ou Apache), avec les accents du français. Chaque fichier et sa licence sont tracés dans `art-src/LICENCES.md`.

| Rôle | Candidate | Pourquoi |
|---|---|---|
| Titres, affiches, gros chiffres | **Anton** (OFL) | Condensée, massive, typique des affiches de propagande |
| Pochoirs (coques, caisses, verdicts) | **Big Shoulders Stencil** (OFL) | Marquage militaire et industriel |
| Épitaphes, citations (rare) | **IM Fell English** (OFL) | Caractère gravé, funèbre |
| Texte courant, règles des cartes | **Barlow** / **Barlow Condensed** (OFL) | Très lisible en petit, sur mobile |
| Chiffres techniques du cockpit, journal | **Share Tech Mono** (OFL) | Terminal informatique rétro : vert phosphore sur fond noir, séparateurs `/` et `:` (« > PV: 27/30 / BOUCLIER: 5 ») |

Le thème a déjà deux emplacements de police, titre et texte (`ThemeSettings`). Une police mono pourra s'y ajouter.

### 6.3 Matériaux 3D (registre A)
- **Low-poly texturé** (ARB-77), inchangé : le détail vient des textures.
- **Les peintures extérieures restent brillantes** : on est dans l'espace, la coque est laquée et vernie, avec des reflets francs (brillance élevée dans URP), mais **dans des teintes profondes**, jamais acidulées. L'usure se limite aux **arêtes, éclats, rayures et abords des rivets**, où la rouille et la tôle nue apparaissent. L'intérieur (cockpit, soutes) est plus mat, plus sale.
- La tôle porte des rivets, des soudures, des traces de brûlure et des rustines.
- **Machinerie apparente** : faisceaux de câbles gainés, valves à volant, tuyaux, boîtiers électroniques, radiateurs, antennes. Sur les vaisseaux, le cockpit, le dé et la table.
- Les **marquages** sont fonctionnels et sinistres : numéros de coque au pochoir, avertissements, décomptes de victimes, mentions d'assureur (« NON COUVERT »).
- **Lumières émissives rares** (le Bloom est déjà réglé) : réacteurs, feux de position, écrans. Pas d'enseigne partout.
- La peinture `Siege` reste en niveaux de gris clairs, teintée par le jeu (ARB-76), avec l'usure aux arêtes.
- Textures générées par les scripts Blender quand c'est possible (bruit de rouille, de suie, d'écaillage, à la manière du métal brossé des cartes). Elles restent reproductibles.

### 6.4 Formes
- **Vaisseaux** : massifs, industriels, asymétriques par réparation plutôt que par fantaisie. Silhouettes lourdes et lisibles de loin.
- **Interface** : deux familles.
  - Les **plaques de métal noirci** pour tout ce qui est « à bord » : cockpit, panneaux des sièges, arc d'actions. Écrans de terminal verts, voyants, câbles qui courent entre les éléments.
  - Le **papier jauni** pour tout ce qui est « dans le secteur » : menus, bandeau de manche, fin de partie, bulles d'aide. Affiches arrachées, photocopies, agrafes.
- **Tampons** pour les verdicts : « ÉLIMINÉ », « DOSSIER CLASSÉ », « ÉLU ».
- Pas de formes rondes et joyeuses, pas d'inclinaisons fantaisistes : les éléments sont droits, lourds, administratifs.

### 6.5 Écrans clés **[à valider sur la planche]**
- **Accueil** : un mur d'affiches de propagande arrachées, sous une lumière blafarde, le vortex en fond.
- **Partie locale** : un registre d'assurance, où l'on « souscrit » un contrat par capitaine.
- **Annonce de tour** : un bandeau de métal avec le nom du capitaine au pochoir.
- **Fin de partie** :
  - l'**avis de recherche** du vainqueur (Domination) ;
  - ou son **affiche de propagande** tamponnée « ÉLU » (Élection) ;
  - avec les épitaphes des autres, tamponnées « DOSSIER CLASSÉ ».
- **Pause** : une notice d'assurance, avec ses petites lignes.

### 6.5 bis Les cartes deviennent des modules (ARB-95)
Une carte n'est plus un papier : c'est un **module de vaisseau branchable**, un boîtier qu'on enfiche dans une baie du cockpit.
- Direction validée sur la planche (ARB-96).
- **Le boîtier** : métal noirci, vis, grille d'aération, **connecteur à broches** en bas (il entre dans la baie), une poignée ou des encoches sur le haut.
- **L'illustration** apparaît sur un **écran** du module, légèrement teinté et balayé.
- **Le nom** est une plaque gravée ou un pochoir. **Le texte de règle** est affiché sur un écran de terminal ou une plaque rétroéclairée, toujours net et lisible (§5.3).
- **La couleur de faction** passe par une bande de voyants et le liseré du boîtier ; l'emblème est estampé.
- **Usage unique ou durable** : un voyant ou un fusible visible (un module à usage unique a un fusible qui grille).
- Les **sockets du cockpit** deviennent des **baies** avec leurs broches ; le **marché noir** devient un **rack** de modules récupérés.
- C'est une reprise du modèle 3D de la carte (`build_card.py`, ADR-0017), à faire par l'atelier Blender ; les zones nommées (illustration, nom, texte) restent, le jeu n'a pas à changer.

### 6.6 Iconographie
- Pictogrammes **au pochoir** : formes pleines, une couleur, lisibles à 32 px.
- Chaque faction a un emblème (§3) : sur les cartes, les diodes du cockpit, les affiches et les coques.
- Les icônes des emplacements (ATK, DEF, EVT, TECH ; ARB-86) passent au même style de pochoir.

### 6.7 Animation et retours de jeu
- **Poids et gravité**, pas d'exagération cartoon : les coups sont lourds, les coques encaissent, les débris restent. Le recul élastique actuel (ARB-89) est à revoir vers un recul plus massif, amorti plus vite **[à valider en jeu]**.
- **Pas de gags visuels.** Ce qui se passe à l'écran est sérieux : fumée, étincelles, dépressurisation, débris.
- **Rythme** : un effet ne retarde jamais le jeu au-delà de ce qu'il raconte. La vitesse de lecture s'applique à tout.

### 6.8 Son (lot à part, plus tard)
- **Voix de SINISTRA** (ARB-96) : une voix de synthèse volontairement froide et artificielle, à peine modulée, comme une annonce automatique.
  - Répliques courtes, enregistrées d'avance en fichiers audio (pas de synthèse pendant la partie : rien ne dépend d'un service en ligne, rien ne sort de l'appareil).
  - Elle double les lignes du journal et des annonces qui ont une pique, jamais le fait brut, et se coupe avec les commentaires de SINISTRA ; un volume à part dans les options.
  - Outil et licence des fichiers produits tracés dans `art-src/LICENCES.md` ; usage commercial vérifié avant de choisir l'outil.
- **Ambiance** : drones industriels, radio du secteur grésillante, fragments de propagande ; un jazz d'ascenseur sinistre pour la Maison.
- **Retours de jeu** : impacts métalliques sourds, grincements de coque, alarmes, coups de tampon, bips du terminal de SINISTRA (en texte, sans voix enregistrée au début).
- Le jeu n'a aucun son aujourd'hui : il faudra des sources libres ou faites maison, licences tracées, comme les images.

## 7. Illustrations des cartes (Stable Diffusion, par le designer)

Le designer génère les illustrations de son côté. Pour qu'elles tiennent ensemble :

- **Format** :
  - la fenêtre d'illustration de la carte est en **16:9, paysage** ;
  - générer par exemple en 1344 × 768, puis recadrer en 16:9 (1280 × 720) ;
  - Unity la ramène à 1024 px de large au plus.
- **Fichier** : PNG nommé d'après l'identifiant (`A_005.png`, `EVT_TROU_NOIR.png`, `TECH_BLUE.png`), déposé dans `unity/Assets/_Vortex/Art/Cards/`. Le jeu le prend tout seul, et l'illustration provisoire revient si on l'enlève.
- **Style commun (registre C)** :
  - illustration sombre de science-fiction industrielle, peinture réaliste, éclairage bas et contrasté ;
  - palette réduite (§6.1), grain, usure ; jamais cartoon, jamais acidulé ;
  - un sujet central lisible en petit ;
  - l'image est affichée **sur l'écran d'un module** : pas besoin de cadre dans l'image.
- **Sans texte dans l'image** : le jeu écrit le nom et le texte. Les lettres générées sont presque toujours fausses.
- **Cohérence** :
  - même modèle et mêmes réglages pour toute la série ;
  - une couleur dominante par faction, celle de la carte ;
  - les cartes neutres restent dans les tons suie, rouille et os.
- **Licences** :
  - noter le modèle utilisé et sa licence dans `art-src/LICENCES.md` (usage commercial permis ou non) ;
  - ne pas demander le style d'un artiste vivant nommé ;
  - ne pas verser les fichiers du modèle dans le dépôt.

Une fiche de prompts par faction (formulations de base, éléments à éviter) sera proposée avec la revue des assets.

## 8. Renommages

- Les noms de cartes, d'événements et de factions **peuvent changer**, avec la validation du designer, dans le cadre du travail d'équilibrage (ARB-93).
- Circuit :
  1. une proposition dans la revue des assets ;
  2. la validation du designer ;
  3. la modification des fichiers de contenu, qui restent la source de vérité (ADR-0008), vérifiée par l'outil de contenu ;
  4. `CARDS.md` régénéré ;
  5. une entrée ARB.
- Les **identifiants** (`A_005`, `TECH_BLUE`…) ne changent jamais : les illustrations, les tests et les sauvegardes s'y accrochent.

## 9. Suite

1. **Planche de tendance** : palette, typographies, emblèmes, maquettes d'écrans clés et lignes de SINISTRA, pour valider cette bible sur pièces.
2. **Revue de tous les assets** : HUD, menus, modèles, animations et textes, face à cette bible. Chaque élément avec son état, l'écart, la proposition, la priorité, le coût, et qui s'en charge : cette session ou l'atelier Blender.
3. **Tranche verticale** : la table (cockpit, un vaisseau, un panneau de siège) et l'accueil entièrement passés dans la nouvelle direction, validés en jeu.
4. **Déploiement par lots**, une PR par lot.
