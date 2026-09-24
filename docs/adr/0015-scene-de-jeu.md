# ADR-0015 : Scène de jeu en 3D à caméra fixe, interface 2D sans logique de règles

- **Statut** : accepté, 2026-09-24.

## Contexte
Le game designer a décrit l'écran de jeu ([`INTERFACE.md`](../INTERFACE.md)) : adversaires face au joueur, marché noir au centre, vaisseau du joueur en bas, actions en demi-cercle, glisser-déposer avec aperçu. Il veut à terme une grande liberté visuelle (fond spatial, vaisseaux animés), mais le prototype doit rester simple.

Contraintes :
- le jeu tourne sur Android (tactile, sans survol) et sous Windows (souris) ;
- le texte des cartes est long et doit rester lisible sur un téléphone ;
- l'interface ne doit jamais diverger des règles, qui changent encore (options de règles, revue des cartes à venir).

## Options
1. **Tout en 2D** (uGUI). C'est le plus simple, mais la lumière, les particules et le post-traitement de URP servent peu, et le rendu reste plat.
2. **Tout en 3D**, cartes et jauges comprises. Le rendu est riche, mais le texte, la mise en page selon l'écran et le glisser-déposer coûtent cher à bien faire.
3. **Une scène 3D à caméra fixe pour ce qui se voit, une interface 2D par-dessus pour ce qui se lit et se manipule.**

## Décision
L'option 3.

**Deux couches**
- **Scène 3D**, vue par une caméra fixe : vaisseaux, dé, fond spatial, effets. Elle profite de tout l'outillage de Unity (lumière, particules, VFX Graph, Shader Graph, post-traitement URP).
- **Interface 2D** (uGUI et TextMeshPro) par-dessus : cartes, marché, jauges, actions, info-bulles, fenêtres de décision. Le texte y reste net à toute résolution, et la mise en page suit l'écran (référence 1920×1080, zones sûres).
- Les éléments de l'interface qui se rattachent à un vaisseau (jauges, cibles d'un glisser) suivent la position de ce vaisseau à l'écran.

**Aucune règle dans l'interface**
- Ce qu'on peut faire, et sur qui, vient des commandes légales de la session (`IGameSession.LegalCommands`). Une cible est proposée si une commande légale la vise, sinon elle est grisée avec la raison donnée par le moteur.
- Les aperçus (dés d'une attaque, bonus, bouclier effectif, fourchette de dégâts) sont calculés **par le moteur**, avec le même code que l'action réelle et à partir de la seule information publique. L'interface les affiche sans rien recalculer.

**Gestes**
- Une couche d'entrée unique traduit souris et doigt en trois gestes : voir en grand (survol, ou appui long), sélectionner (clic ou toucher), agir sur une cible (glisser-déposer). Les vues ne connaissent que ces gestes.

**Textes**
- Tous les textes de l'interface passent par une table de textes, en français seulement pour l'instant : `TextTable`, un asset clé → texte éditable dans Unity, avec les clés listées dans le code (`TextKeys`). Une clé sans texte s'affiche `#clé`, pour que le manque se voie. Le paquet Localization de Unity, plus lourd (il repose sur Addressables), sera évalué si l'on traduit.
- Les textes qui ne viennent pas du jeu lui-même (noms des joueurs) sont affichés sans interprétation des balises de mise en forme : un nom ne peut pas changer l'apparence de l'écran.

## Conséquences
- (+) Le designer garde toute la liberté visuelle de Unity pour la scène, sans toucher à l'interface ni aux règles.
- (+) Une règle ou une carte qui change modifie automatiquement ce que l'interface propose et prévisualise : il n'y a qu'un seul code de règles.
- (+) Souris et tactile partagent les mêmes vues ; seul le traducteur de gestes diffère.
- (−) Le moteur doit offrir un aperçu d'attaque qui ne tire aucun dé : c'est un ajout au moteur (M4.5), couvert par des tests qui le comparent à l'attaque réelle.
- (−) Deux couches à garder alignées : les éléments d'interface rattachés à un vaisseau suivent sa position à l'écran.
