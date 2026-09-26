# Direction artistique et univers

Ce document est la **bible** de l'apparence et du ton du jeu. Tout visuel, tout texte d'interface, toute animation et tout son s'y réfère.

- Décision de fond : [ADR-0020](adr/0020-direction-artistique.md) ; choix du designer : ARB-93.
- Liens avec le reste :
  - la liste des visuels et leurs formats reste dans [`ASSETS.md`](ASSETS.md) ;
  - les animations dans [`ANIMATIONS.md`](ANIMATIONS.md) ;
  - l'écran dans [`INTERFACE.md`](INTERFACE.md).
- Cette direction ne change **ni les règles ni le moteur**. Elle passe par le thème, la table des textes, les profils d'animation et les modèles (ADR-0007, ADR-0014, ADR-0015).

**Statut : brouillon v1**, à valider avec le designer, section par section. Les points encore ouverts sont marqués **[à valider]**.

## 1. Pitch et piliers

> **Le dernier grand casse avant la fin des temps.**
> Un vortex avale peu à peu la Marge, un secteur oublié de la galaxie. Ses capitaines pirates ont encore quelques manches pour piller, trahir, et se faire élire patron de ce qui reste, avant que tout soit aspiré.

**Piliers**
1. **Fun d'abord.** Chaque coup se lit, se sent et se paie tout de suite. L'humour accompagne le jeu sans jamais le ralentir.
2. **Humour noir, sarcastique, adulte.** On rit de la mort, de la cupidité, de la bureaucratie et de la politique d'un univers fictif. On ne rit jamais aux dépens du joueur réel ni de personnes réelles (§5.2).
3. **Pirates bricoleurs.** Tout est usé, rafistolé, volé, repeint. Rien n'est neuf, rien n'est garanti.
4. **Satire électorale.** L'Élection galactique est une campagne : affiches, promesses, pots-de-vin, votes achetés.
5. **Lisible avant tout.** Une information de jeu reste nette dans n'importe quelle ambiance : la blague vit **à côté** de l'information, jamais à sa place (§5.3).

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

### 2.6 La voix du secteur : SINISTRA **[à valider]**
- **Qui** : l'IA d'expertise des **Assurances Horizon Final**, dont le slogan est *« Nous couvrons tout. Sauf vous. »*
- **Son rôle** : elle commente la partie dans le journal, les annonces de tour et les épitaphes. Elle estime chaque dommage, classe chaque dossier, et ne rembourse jamais.
- **Son ton** : d'une politesse glaciale, elle vouvoie, avec un vocabulaire d'assureur (sinistre, franchise, dossier classé, clause d'exclusion).
- **Pourquoi elle** : un narrateur unique donne une voix constante au jeu et concentre l'humour à un seul endroit, qu'on peut couper (§5.4).

## 3. Les factions

Les quatre technologies deviennent quatre factions. Obtenir une technologie, c'est **obtenir le soutien d'une faction** pour l'élection. Un combo est **un pacte** avec elle.

| Technologie (couleur) | Faction | Qui ils sont | Visuel | Ton | Lien avec la mécanique |
|---|---|---|---|---|---|
| **Ordre** (bleu) | **La Légion de l'Ordre** | Des fantassins d'élite en armure lourde, dernière force régulière de la Marge. Disciplinés, fanatiques, sans le moindre humour : ce sont eux, les pas rigolos. | Armures lourdes, visières, bannières, rangs serrés. Emblème : un casque de combat sur un bouclier | **Sérieux, martial**. L'humour vient de ce qui les entoure, jamais d'eux | Dévier une attaque : interceptée et redirigée, « conformément au protocole » |
| **Casino Cosmique** (jaune) | **La Maison** | La pègre du jeu. Elle possède la moitié des dettes du secteur et l'autre moitié des croupiers. | Néons, dorures criardes, jetons, tapis vert. Emblème : un d8 couronné | Bonimenteur, faussement généreux | Deux actions d'équipage : **« double mise »** |
| **Rebelles** (rouge) | **Le Front Syndical des Mutins** | Des équipages en grève perpétuelle, armés jusqu'aux dents. Ils votent tout, sauf le retour au travail. | Pochoirs, peinture à la bombe, drapeaux rapiécés. Emblème : un poing qui serre une clé à molette | Militant, grandiloquent | Surcharge et avantage : **« mouvement social »** |
| **Abomination organique** (vert) | **Les Adeptes de la Corruption** | Un culte fongique. La contamination y est une communion ; les membres ne sont plus tout à fait des gens. | Chair, mycélium, bioluminescence verte, coques colonisées. Emblème : une spore en forme d'œil | Doucereux, prosélyte | Tourment réactivé : **« grande communion »** |

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
| Technologie, combo | Le soutien d'une faction ; un pacte |
| Manche | Un tour d'horloge avant le vortex |
| Dé | Le dé du destin : un d8 de casino, forcément pipé |
| Élimination | Un sinistre, que SINISTRA classe |

## 5. Charte d'écriture

### 5.1 Le ton
- **Sec, court, grinçant.** La chute tombe à la fin de la phrase. Une blague qu'il faut expliquer est retirée.
- **Adulte** : l'humour est noir (mort, cupidité, absurdité administrative), mais pas vulgaire pour rien. Un juron bien placé reste permis.
- **Français d'abord**, avec les mots du milieu : jargon de casino, de syndicat, d'assurance, de piraterie.
- On garde l'esprit des noms de cartes actuels (Langue de bois, Chance de cocu, Vautours…) : jeux de mots, expressions détournées, références populaires.

### 5.2 Les limites (validées, ARB-93)
- **Oui** : humour noir sur la mort, la cupidité, la bureaucratie, la religion et la politique **fictives** du secteur ; violence cartoon, exagérée, sans gore réaliste.
- **Non** :
  - aucune personne réelle, aucun parti réel, aucune religion réelle ;
  - aucune blague qui vise un groupe de personnes (origine, genre, orientation, handicap…) ;
  - aucun contenu sexuel explicite.
- Ces limites visent aussi une classification d'âge raisonnable sur les boutiques (PEGI 12 à 16 selon la violence finale).

### 5.3 Où va l'humour
| Texte | Registre | Règle |
|---|---|---|
| Texte de règle d'une carte, règles, aperçus, raisons d'un refus, questions d'une décision | **Neutre et exact** | Aucune blague : c'est ce que le joueur lit pour décider |
| Libellés de l'interface (boutons, menus) | **Clair**, touche d'ambiance tolérée dans les titres | Le mot d'action reste évident (« Fin de tour », pas un jeu de mots) |
| Nom d'une carte, d'un événement, d'une faction | **Ambiance** | Voir §8 pour les renommages |
| Texte d'ambiance d'une carte **[à valider]** | **Ambiance** | Une ligne sous le texte de règle, en italique, facultative |
| Journal, annonces de tour, épitaphes, astuces | **La voix de SINISTRA** | Le fait d'abord (qui, quoi, combien), la pique ensuite, courte |

### 5.4 La voix de SINISTRA
- Une ligne du journal garde **le fait lisible d'un coup d'œil**. La pique est un suffixe court, tiré au hasard parmi plusieurs variantes, pour ne pas lasser.
- Une option **« Commentaires de SINISTRA »** permet de les couper **[à valider]**.
- Exemples (à écrire pour de bon avec la revue des textes) :
  - Attaque : « Rouge perfore Bleu : 6 dégâts. *Franchise non applicable.* »
  - Coup critique : « Critique ! Bleu perd Sous-couche blindée. *Nos experts parlent de perte totale.* »
  - Élimination : « Le vaisseau de Vert est déclaré épave. *Dossier classé. Merci de votre fidélité.* »
  - Annonce de tour : « À vous, capitaine Jaune. *Tâchez de survivre : les formulaires sont longs.* »
  - Marché noir recyclé : « Rouge vide l'étal. *Le vendeur pleure, puis recompte.* »
  - Approche du vortex : « Le vortex passe. *Il ne rembourse pas non plus.* »
  - Épitaphe de fin de partie : « Ci-gît Bleu. Il avait un bouclier et des principes. Il n'a plus ni l'un ni l'autre. »
  - Astuce : « Un bouclier désactivé protège exactement autant qu'une prière. »

## 6. Direction visuelle

Deux registres, choisis par le designer (ARB-93) :
- **A, pour la 3D : le futur usé, rouille et néon.** Tôles rivetées, rustines, rouille, enseignes au néon.
- **C, pour la 2D : le pulp rétro de propagande.** Affiches électorales, avis de recherche, tampons, sérigraphie des années 50 à 70.

Ils se rejoignent dans le cockpit et les menus : du métal usé couvert d'affiches collées et d'autocollants.

### 6.1 Palette
| Rôle | Couleurs | Usage |
|---|---|---|
| **Fond** | Graisse `#17181B`, tôle `#4D5358`, rouille `#8A4B2A` | Coques, cockpit, panneaux, ciel |
| **Papier** | Papier d'affiche `#E9DDC0`, encre `#1C1A17` | Menus, affiches, cartes à texte, tampons |
| **Néon** (accent, rare) | Magenta `#FF2E88`, cyan `#2EE6FF` | Ce qui brille : réacteurs, enseignes, sélection, tour en cours |
| **Danger** | Jaune `#FFC21A` sur noir, en bandes | Surcharge, avertissements, fin des temps |
| **Factions** | Les couleurs actuelles des technologies (bleu, jaune, rouge, vert) | Cartes, emblèmes, diodes |
| **Sièges** | Les cinq couleurs actuelles | Peinture des vaisseaux, liserés |
| **Retours de jeu** | Perte (rouge), soin (vert), surcharge (ambre), comme aujourd'hui | Chiffres, jauges, effets |

Règles :
- Le néon reste un **accent** : s'il est partout, plus rien ne ressort.
- Les couleurs des sièges et des factions ne changent pas sans vérifier leur contraste entre elles et sur le nouveau fond.
- Une information ne passe jamais par la couleur seule : forme, emblème, motif ou texte l'accompagnent (daltonisme).

### 6.2 Typographies **[à valider sur la planche]**
Polices libres (licence SIL OFL ou Apache), avec les accents du français. Chaque fichier et sa licence sont tracés dans `art-src/LICENCES.md`.

| Rôle | Candidate | Pourquoi |
|---|---|---|
| Titres, affiches, gros chiffres | **Anton** (OFL) | Condensée, massive, typique des affiches de propagande |
| Avis de recherche, épitaphes (rare) | **Rye** (OFL) | Western, « WANTED », à petites doses |
| Texte courant, règles des cartes | **Barlow** / **Barlow Condensed** (OFL) | Très lisible en petit, sur mobile |
| Chiffres techniques du cockpit, journal | **Share Tech Mono** (OFL) | Afficheur de bord |

Le thème a déjà deux emplacements de police, titre et texte (`ThemeSettings`). Une police mono pourra s'y ajouter.

### 6.3 Matériaux 3D (registre A)
- **Low-poly texturé** (ARB-77), inchangé : le détail vient des textures.
- La tôle est peinte, écaillée, rouillée sur les arêtes, avec des rivets, des soudures et des rustines d'une autre couleur.
- On y colle des **décalcomanies** : logos de faction, numéros de coque, et des mentions d'assureur (« NON COUVERT », « VÉHICULE DE REMPLACEMENT »).
- **Néons émissifs** pour tout ce qui brille (le Bloom est déjà réglé) : réacteurs, feux, enseignes.
- La peinture `Siege` reste en niveaux de gris clairs, teintée par le jeu (ARB-76). Elle devient une **peinture écaillée**, qui laisse voir la tôle dessous.
- Textures générées par les scripts Blender quand c'est possible (bruit de rouille, de crasse, d'écaillage, à la manière du métal brossé des cartes). Elles restent reproductibles.

### 6.4 Formes
- **Vaisseaux** : bricolés, asymétriques, avec des pièces rapportées (un réacteur de trop, un canon scotché). Chacun garde une silhouette lisible de loin.
- **Interface** : deux familles.
  - Les **plaques de tôle rivetées** pour tout ce qui est « à bord » : cockpit, panneaux des sièges, arc d'actions.
  - Le **papier collé** pour tout ce qui est « dans le secteur » : menus, bandeau de manche, fin de partie, bulles d'aide. Papier déchiré, ruban adhésif, punaises.
- **Tampons encreurs** pour les verdicts : « ÉLIMINÉ », « DOSSIER CLASSÉ », « ÉLU ».
- **Étiquettes à imprimante Dymo** pour les petits libellés à bord.

### 6.5 Écrans clés **[à valider sur la planche]**
- **Accueil** : un mur d'affiches électorales à moitié arrachées, les quatre factions qui se recouvrent l'une l'autre, le vortex en fond.
- **Partie locale** : un registre de l'assurance, où l'on « souscrit » un contrat par capitaine.
- **Annonce de tour** : un bandeau de tôle avec le nom du capitaine au pochoir.
- **Fin de partie** :
  - l'**avis de recherche** du vainqueur (Domination) ;
  - ou son **affiche de campagne** barrée « ÉLU » (Élection) ;
  - avec les épitaphes des autres, tamponnées « DOSSIER CLASSÉ ».
- **Pause** : une notice d'assurance, avec ses petites lignes.

### 6.6 Iconographie
- Pictogrammes **au pochoir** : formes pleines, une couleur, lisibles à 32 px.
- Chaque faction a un emblème (§3) : sur les cartes, les diodes du cockpit, les affiches et les coques.
- Les icônes des emplacements (ATK, DEF, EVT, TECH ; ARB-86) passent au même style de pochoir.

### 6.7 Animation et retours de jeu
- **Exagération cartoon** : on appuie sur l'anticipation, l'impact et le rebond, comme le recul élastique déjà en place (ARB-89).
- **Gags visuels courts**, jamais bloquants : une pièce qui se détache sous un coup, une rustine qui saute, une fumée noire qui tousse, un tampon qui s'écrase sur une épave.
- **Rythme** : un effet ne retarde jamais le jeu au-delà de ce qu'il raconte. La vitesse de lecture s'applique à tout.

### 6.8 Son (lot à part, plus tard)
- **Ambiance** : radio pirate du secteur, surf rock et synthés des années 70, avec un casino lounge pour la Maison.
- **Retours de jeu** : bruits métalliques, grincements, coups de tampon, bips de la voix de SINISTRA (en texte, sans voix enregistrée au début).
- Le jeu n'a aucun son aujourd'hui : il faudra des sources libres ou faites maison, licences tracées, comme les images.

## 7. Illustrations des cartes (Stable Diffusion, par le designer)

Le designer génère les illustrations de son côté. Pour qu'elles tiennent ensemble :

- **Format** :
  - la fenêtre d'illustration de la carte est en **16:9, paysage** ;
  - générer par exemple en 1344 × 768, puis recadrer en 16:9 (1280 × 720) ;
  - Unity la ramène à 1024 px de large au plus.
- **Fichier** : PNG nommé d'après l'identifiant (`A_005.png`, `EVT_TROU_NOIR.png`, `TECH_BLUE.png`), déposé dans `unity/Assets/_Vortex/Art/Cards/`. Le jeu le prend tout seul, et l'illustration provisoire revient si on l'enlève.
- **Style commun (registre C)** :
  - pulp rétro et affiche de propagande, sérigraphie, trame de demi-teintes ;
  - palette réduite (§6.1), grain et usure ;
  - un sujet central lisible en petit.
- **Sans texte dans l'image** : le jeu écrit le nom et le texte. Les lettres générées sont presque toujours fausses.
- **Cohérence** :
  - même modèle et mêmes réglages pour toute la série ;
  - une couleur dominante par faction, celle de la carte ;
  - les cartes neutres restent dans les tons papier et rouille.
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
