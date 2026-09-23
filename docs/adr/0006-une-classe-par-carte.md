# ADR-0006 : Une classe par carte, registre explicite, sans DSL ni réflexion

- **Statut** : accepté, 2026-09-23

## Contexte
Il y a 54 modificateurs, 8 événements et 4 technologies. Leurs effets sont très hétérogènes : vols, échanges, paris sur le dé, réactions sur le tour des autres, déclencheurs conditionnels. Le game designer veut équilibrer les valeurs sans toucher au code.

## Options
1. **DSL JSON** (déclencheur + opération) : séduisant, mais il faudrait inventer un mini-langage capable d'exprimer 66 effets. Il deviendrait vite aussi complexe que du code, en moins lisible et moins testable.
2. **Classe par carte** qui redéfinit des points d'accroche typés. Les **valeurs** (+4, ×2, 15 PV…) restent dans le JSON, dans `params`.
3. Enregistrement des classes par attribut et réflexion : fragile avec IL2CPP et le stripping de code, et il masque le graphe de dépendances.

## Décision
Option 2, avec un **registre statique explicite** (`CardRegistry` : id → fabrique), écrit à la main.

## Conséquences
- (+) Chaque carte est lisible, commentée avec son texte exact et son arbitrage, et testée isolément.
- (+) L'équilibrage des valeurs se fait dans le JSON, sans recompiler.
- (+) Pas de réflexion : compatible IL2CPP et facile à auditer.
- (−) Une carte nouvelle ou modifiée demande du code. Un test vérifie que chaque id du JSON a une classe, et inversement.
