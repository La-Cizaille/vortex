# ADR-0002 : Moteur de règles en C# pur, partagé via un package UPM local

- **Statut** : accepté, 2026-09-23

## Contexte
Les mêmes règles doivent tourner dans quatre contextes :
- le client Unity ;
- les tests (en CI, sans licence Unity) ;
- le simulateur d'équilibrage ;
- plus tard, le serveur autoritaire.

Une divergence entre ces copies produirait des bugs ou de la triche.

## Options
1. Règles dans des MonoBehaviours : impossible à exécuter hors Unity.
2. DLL précompilée copiée dans Unity : étape de build manuelle et risque de version périmée.
3. **Sources dans un package UPM local `core/`**, sans référence à `UnityEngine` (`noEngineReferences`). Unity le référence en `file:../../core`, et un csproj .NET inclut les mêmes fichiers.

## Décision
Option 3.

## Conséquences
- (+) Une seule source de vérité, compilée par les deux chaînes. Toute modification est testée en CI.
- (+) Le moteur ne dépend d'aucune API Unity, ce qui le rend testable et portable.
- (−) Le code doit rester en C# 9 / netstandard2.1. La CI compile le csproj avec `LangVersion 9`, ce qui détecte les écarts.
- (−) Les `.meta` des fichiers de `core/` sont générés par Unity et doivent être commités.
