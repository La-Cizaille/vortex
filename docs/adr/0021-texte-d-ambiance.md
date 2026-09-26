# ADR-0021 : Un texte d'ambiance facultatif dans le format du contenu

- **Statut** : accepté, 2026-09-26.

## Contexte
La bible de direction artistique (ADR-0020) veut une ligne d'ambiance sous le texte de règle des cartes, des événements et des technologies ; le designer l'écrira plus tard (ARB-95). Les fichiers de contenu sont la source de vérité (ADR-0008) et chargés avec un format strict : un membre inconnu est refusé.

## Options
1. **Une table à part dans le client** (identifiant → texte). Le contenu reste inchangé, mais le texte d'une carte vit à deux endroits, et un outil de contenu ne le voit pas.
2. **Un champ facultatif `flavor` dans les fichiers de contenu**, à côté du texte et de l'arbitrage.

## Décision
**Option 2.** `flavor` est facultatif sur les cartes, les événements et les technologies :
- absent, rien ne change : les fichiers actuels restent canoniques ;
- présent, c'est du texte brut, non vide, de 200 caractères au plus, sans `<` ni `>` : le client l'insère dans du texte riche, il ne doit donc jamais porter de balise ;
- il n'énonce jamais une règle (bible §5.3) : le moteur ne le lit pas ;
- `CARDS.md` le reprend ; le client l'affiche en italique sous le texte de règle ; le schéma de l'éditeur le décrit.

## Conséquences
- Le designer peut écrire les textes d'ambiance directement dans les fichiers de contenu, validés par l'outil de contenu.
- Le chargeur garde son format strict : un seul membre de plus, décrit dans le code et dans le schéma.
