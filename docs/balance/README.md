# Équilibrage

Ce dossier contient les **rapports du simulateur** (`Vortex.Simulator`) et leur analyse. Un rapport est généré, reproductible (graine et empreinte du contenu indiquées en tête), et n'est jamais modifié à la main.

## Lancer une simulation

```
dotnet run -c Release --project dotnet/Vortex.Simulator -- --players 2,3,4,5 --games 1000 --seed 1 --out docs/balance/<date>-<sujet>.md
```

| Option | Rôle | Défaut |
|---|---|---|
| `--players` | Tailles de table simulées | `2,3,4,5` |
| `--games` | Parties par scénario et par taille de table | `200` |
| `--seed` | Graine : même graine et même contenu donnent les mêmes chiffres | `1` |
| `--samples` | Simulations par coup candidat du bot heuristique (plus = plus fort et plus lent) | `2` |
| `--data` | Dossier du contenu | `core/Runtime/Data` |
| `--out` | Fichier du rapport (sinon, sortie standard) | — |

Pour chaque taille de table, le simulateur joue deux scénarios :
- **tous heuristiques**, pour la durée des parties, l'avantage de position, les combats, les cartes et les événements ;
- **écart de niveau**, avec un bot heuristique contre des bots aléatoires.

Il renvoie le code 1 si le moteur a rencontré une erreur. Dans ce cas, le rapport liste les graines pour rejouer les parties concernées.

## Évaluer une modification de carte
1. Lancer une simulation de référence avec la graine habituelle, **avant** la modification.
2. Modifier le JSON de la carte, puis valider le contenu (`Vortex.ContentTool validate`).
3. Relancer la simulation avec **la même graine**, et comparer les deux rapports : durée, écart de la carte, avantage de position.
4. Joindre les deux rapports à la PR.

Les bots ne jouent pas comme des humains (voir ADR-0010). Un écart de quelques points est un **signal à confirmer en partie réelle**, pas un verdict.

## Premiers constats : référence du 2026-09-24

Rapport : [`2026-09-24-reference.md`](2026-09-24-reference.md), 8 000 parties, aucune erreur du moteur.

1. **Fort avantage au premier joueur.** Le joueur qui gagne l'initiative remporte 71 % des duels (au lieu de 50 %), 46 % des parties à 3 (au lieu de 33 %) et 38 % à 4 (au lieu de 25 %). Le taux de victoire décroît avec la position dans le tour. C'est le déséquilibre le plus net, et il est structurel. Pistes à simuler :
   - le premier joueur ne peut pas attaquer à la première manche ;
   - un bonus de bouclier ou de PV de départ pour les derniers joueurs ;
   - un premier joueur qui tourne à chaque manche.
2. **La Fin des temps rythme les parties.** Elle est atteinte dans 38 % des duels, 67 % des parties à 3, 82 % à 4 et 90 % à 5. Les parties durent en moyenne 9 à 13 manches, soit environ 9 à 22 minutes à 30 s par tour de joueur (hypothèse). C'est cohérent pour du mobile ; le réglage de `DoomRound[n]` sera le levier principal de la durée.
3. **L'Élection galactique n'arrive presque jamais** (0 à 0,4 % des victoires). Réunir 4 paires de couleurs en sacrifiant ses cartes semble irréaliste dans la durée d'une partie. C'est à discuter : soit c'est voulu (une victoire rare et spectaculaire), soit la condition doit baisser (3 technologies ?).
4. **Les choix comptent face au hasard pur.** Un bot heuristique bat des bots aléatoires dans 77 à 99 % des parties. C'est rassurant, mais la référence aléatoire est faible (voir ADR-0010). La mesure fine de la part du hasard viendra d'une comparaison entre deux niveaux de bot.
5. **Combats.** Environ 3,5 PV retirés par attaque, et une attaque sur cinq ne fait aucun dégât.
6. **Cartes à surveiller** (écart par rapport à la moyenne des cartes, bots actuels) :
   - **fortes** : Orgueil (D_016, +12 pts), Quarantaine obligatoire (D_021), Canon à particules (A_001), Vente de pièces détachées (D_012), Générateur auxiliaire (D_011) ;
   - **faibles** : Tout est une question d'équilibre (D_020, −7 pts), Le grand final (A_010, −6 pts), Spores corrosifs (A_020), Recels en tous genres (A_014), Niaque (D_010).

   Les soins massifs et le bouclier ressortent en tête : dans un jeu où l'on retire peu de PV par attaque, récupérer des PV ou monter son bouclier est très rentable. Ces classements dépendent en partie du style des bots : ils sont à confirmer lors de la revue des cartes.
