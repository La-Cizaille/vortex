# Contribuer à Vortex

## Workflow Git
1. `main` est protégée. On y fusionne **uniquement** par Pull Request, avec la CI verte.
2. Une branche par sujet, avec les préfixes `feat/`, `fix/`, `docs/`, `chore/` ou `art/`. Exemple : `feat/m1-attack-pipeline`.
3. Chaque PR, rédigée en français, répond à quatre questions :
   - **Quoi** : ce qui change.
   - **Pourquoi** : le lien avec la règle (§ de `RULES.md`) ou l'ADR concerné.
   - **Risques** : sécurité, régression, performance.
   - **Vérification** : les commandes lancées et ce qu'il faut regarder.
4. Commits : messages en anglais, à l'impératif (`Add attack pipeline`), avec un sujet par commit.

## Conventions de code
- **Code et commentaires en anglais**. La documentation (`docs/`) et les PR sont en français.
- Le style est défini par [`.editorconfig`](../.editorconfig). `dotnet format --verify-no-changes` doit passer.
- `core/` compile en **C# 9 / netstandard2.1**, pour rester compatible avec Unity :
  - pas de namespaces sur une ligne, pas de `record` ;
  - pas d'API absente de netstandard2.1.
- `Nullable` est activé et les warnings sont traités comme des erreurs.
- Aucun état statique mutable dans `core/`.
- Pas de réflexion au runtime (IL2CPP).
- Toute API publique de `core/` porte un commentaire XML `///`.
- Une classe de carte commence par le **texte exact** de la carte, puis l'arbitrage appliqué, avec un renvoi vers la section de `RULES.md` :
  ```csharp
  /// <summary>A_005 "Rétablir l'ordre" — "Vos attaques infligent +4 points de dégats supplémentaires."</summary>
  /// <remarks>Ruling (RULES.md §10.1): +4 to attack value, applied before shield.</remarks>
  ```

## Tests
- Toute règle et toute carte a **au moins un test**, qui s'appuie sur `ScriptedDice` pour contrôler les dés.
- Toute commande illégale a un test négatif : erreur attendue, état inchangé.
- Couverture visée pour `core/` : **≥ 90 %** des lignes.
- Pour lancer les tests : `dotnet test dotnet/Vortex.sln`.

## Modifier une carte ou les valeurs d'équilibrage
1. Modifier `design/Vortex.xlsx`, qui est la source de vérité, ou `GameConfig` pour les valeurs globales.
2. Régénérer les données :
   ```
   dotnet run --project dotnet/Vortex.CardImporter -- design/Vortex.xlsx core/Runtime/Data
   ```
   L'import vérifie que les ids sont uniques, que les couleurs et les usages sont connus et que les nombres d'exemplaires sont cohérents.
3. Si le **comportement** d'une carte change, mettre à jour sa classe, son test et `RULES.md` §10.
4. Mesurer l'impact avec le simulateur (`dotnet run --project dotnet/Vortex.Simulator`), à partir du jalon M3.

## Ajouter ou modifier un visuel (à partir du jalon M4)
- Le code ne référence **jamais** un asset directement : tout passe par les catalogues de `unity/Assets/_Vortex/Theme/`.
- Pour remplacer l'illustration d'une carte, déposer `unity/Assets/_Vortex/Art/Cards/<ID>.png` (par exemple `A_005.png`). Les réglages d'import mobile et l'enregistrement dans le catalogue sont appliqués automatiquement.
- Pour un vaisseau, déposer `Art/Ships/<Nom>.fbx`, puis l'associer dans `ShipCatalog`.
- Les couleurs, polices et vitesses d'animation se règlent dans `ThemeSettings`, en direct dans la scène **Galerie**.
- Tant qu'un visuel manque, un placeholder généré est affiché.

## Fusion des fichiers Unity
Ajouter UnityYAMLMerge dans votre configuration Git locale (le chemin dépend de votre version d'Unity) :
```
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "'C:/Program Files/Unity/Hub/Editor/<version>/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"
```

## Sécurité
Lire [`SECURITY.md`](SECURITY.md) avant d'ajouter une dépendance, une entrée externe ou un service.
