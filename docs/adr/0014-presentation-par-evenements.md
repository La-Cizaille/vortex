# ADR-0014 : Présentation pilotée par les événements, retours visuels décrits dans l'éditeur

- **Statut** : accepté, 2026-09-24.

## Contexte
Le client Unity doit montrer une partie que le moteur résout instantanément. Une attaque produit en un seul appel une suite d'événements : attaque déclarée, dés lancés, bouclier effectif, perte de PV, élimination…

Le game designer veut, à terme, une **grande liberté visuelle** et un vrai contrôle sur l'aspect graphique, plus que sur le code. Pour le prototype, des animations simples suffisent, mais elles ne doivent pas devenir une dette à réécrire quand viendra le vrai habillage.

Contraintes :
- le client ne doit jamais voir l'information cachée (SECURITY.md) ;
- en phase 2, la même présentation affichera une partie en ligne.

## Options
1. **Les vues comparent l'ancien et le nouvel état et animent la différence dans le code.** C'est simple au début, mais l'ordre et la cause des changements se perdent (qui a attaqué qui, avec quel dé), et chaque animation vit dans le code.
2. **Une animation écrite en dur par type d'événement.** L'ordre est respecté, mais changer un visuel demande de modifier le code : c'est la dette qu'on veut éviter.
3. **Des événements joués un par un, chacun déclenchant un retour visuel décrit par un asset éditable dans Unity.**

## Décision
L'option 3.

**Session** (`IGameSession`)
- La présentation ne connaît que la **vue publique** (`GameView`), le joueur qui doit agir, les commandes légales, et une méthode pour soumettre une commande. Une commande refusée renvoie une erreur typée et ne change rien.
- `LocalHotSeatSession` garde l'état complet en privé. N'importe quel siège peut être un bot : la présentation le fait jouer pas à pas (`PlayBotStep`), quand elle est inactive, pour que ses coups restent lisibles.
- En phase 2, une `NetworkSession` implémentera la même interface.

**Lecture des événements** (`EventPlayer`)
- Les événements sont joués **un par un** : chacun déclenche son retour visuel, qui indique combien de temps attendre avant le suivant.
- La lecture a une vitesse réglable et un bouton « passer », et bloque les entrées du joueur tant qu'elle n'est pas finie.
- C'est du C# pur, avancé par `Tick`, donc déterministe et testable sans scène.

**Retours visuels** : des assets `ScriptableObject`, créés depuis le menu *Create → Vortex*.
- `PauseFeedback` : une simple attente.
- `PrefabFeedback` : fait apparaître un prefab (particules, objet animé, séquence Timeline…) à un point d'ancrage de la scène, puis le détruit.
- D'autres viendront au fil des besoins : Timeline, Animator, son…
- Un `FeedbackProfile` associe un retour à chaque type d'événement, avec un retour de repli : un nouveau type d'événement ne casse jamais la présentation.
- Les **points d'ancrage** (vaisseau d'un joueur, marché, centre de la table, bandeau) sont fournis par la scène (`IFeedbackStage`) : le retour décide quoi montrer, la scène dit où.

**Outils de création**
- Le designer travaille avec les outils de Unity : prefabs, Timeline, Animator, VFX Graph, Shader Graph, post-traitement URP.
- Les mouvements pilotés par le code (cartes qui volent, dé, caméra) utiliseront **PrimeTween** : licence MIT, pas d'allocation mémoire, version épinglée. Il sera ajouté avec la scène de jeu (M4.4) et inscrit dans SECURITY.md, puisqu'il sera embarqué dans le produit.

**Convention** : une classe `ScriptableObject` ou `MonoBehaviour` vit seule dans un fichier à son nom. Sinon, Unity perd la référence au script dans les assets. Un test le vérifie.

## Conséquences
- (+) Changer l'aspect d'une attaque, d'un soin ou d'une élimination se fait dans l'éditeur, sans toucher au code ni aux règles.
- (+) L'ordre et la cause de chaque changement sont montrés, dans le même ordre que la résolution du moteur (RULES B7).
- (+) Le rythme de lecture est testé automatiquement, et la même présentation servira au jeu en ligne.
- (−) Une indirection de plus : pour qu'un visuel apparaisse, il faut créer son asset et le brancher dans le profil. C'est le prix de la liberté visuelle.
- (−) Pendant la lecture, les vues affichent un état intermédiaire. Elles se recalent sur la vue publique finale quand la lecture est terminée.
