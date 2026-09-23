# Architecture Decision Records

Un ADR consigne **une** décision structurante : son contexte, les options envisagées, le choix fait et ses conséquences. Un ADR accepté ne se modifie pas. Pour changer une décision, on écrit un nouvel ADR qui le **remplace**.

| # | Décision | Statut |
|---|---|---|
| [0001](0001-moteur-unity.md) | Unity 6.3 LTS comme moteur client | Accepté |
| [0002](0002-core-partage.md) | Moteur de règles C# pur partagé (package UPM local) | Accepté |
| [0003](0003-reseau-serveur-autoritaire.md) | Serveur autoritaire .NET + WebSocket (phase 2) | Accepté |
| [0004](0004-rng-deterministe.md) | RNG déterministe maison (PCG32) | Accepté |
| [0005](0005-file-de-resolution-et-decisions.md) | File de résolution explicite et décisions interrompantes | Remplacé par 0009 |
| [0006](0006-une-classe-par-carte.md) | Une classe par carte, registre explicite | Accepté, complété par 0007 |
| [0007](0007-points-interception-et-briques-effets.md) | Moteur indépendant des cartes : points d'interception et briques d'effets | Accepté |
| [0008](0008-json-source-de-verite.md) | Le JSON est la source de vérité du contenu (Excel retiré) | Accepté |
| [0009](0009-decisions-par-re-execution-deterministe.md) | Décisions par re-exécution déterministe | Accepté |

Modèle : copier un ADR existant et garder les sections *Statut*, *Contexte*, *Options*, *Décision* et *Conséquences*.
