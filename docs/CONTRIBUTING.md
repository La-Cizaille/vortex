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
  /// <remarks>Ruling: see the card entry in core/Runtime/Data/cards.json.</remarks>
  ```

## Tests
- Toute règle et toute carte a **au moins un test**, qui s'appuie sur `ScriptedDice` pour contrôler les dés.
- Toute commande illégale a un test négatif : erreur attendue, état inchangé.
- Couverture de `Vortex.Core` : **≥ 90 %** des lignes, bloquant en CI.
- Pour lancer les tests : `dotnet test dotnet/Vortex.sln`.

## Ajouter, modifier ou retirer une carte
La source de vérité est `core/Runtime/Data/` : `cards.json`, `events.json` et `technologies.json` (ADR-0008). Dans VS Code, le JSON Schema référencé par `$schema` donne l'autocomplétion et signale les erreurs en direct.

1. Éditer l'entrée de la carte :
   - **`text`** : le texte imprimé.
   - **`ruling`** : l'interprétation exacte, écrite **uniquement** avec le modèle d'effets (RULES.md partie B). Un arbitrage ne cite **jamais** une autre carte : on écrit « soumis à l'autorisation *modifier un bouclier* », pas « bloqué par Intouchable ».
   - Un **id retiré n'est jamais réutilisé**.
2. Si la carte a besoin d'un point d'interception qui n'existe pas, on l'ajoute **de façon générique** à RULES.md B2, avec son test. On ne crée jamais d'exception propre à la carte (ADR-0007).
3. Mettre en forme, valider et régénérer le catalogue :
   ```
   dotnet run --project dotnet/Vortex.ContentTool -- format core/Runtime/Data
   dotnet run --project dotnet/Vortex.ContentTool -- validate core/Runtime/Data
   dotnet run --project dotnet/Vortex.ContentTool -- docs core/Runtime/Data docs
   ```
4. Si le nombre de cartes change, mettre à jour les quantités attendues dans `GameDataContentTests`. C'est le **seul** test lié au contenu réel.
5. Déclarer les **briques d'effets** de la carte dans `effects` (catalogue et paramètres : [`BRICKS.md`](BRICKS.md)). Pour une mécanique qu'aucune brique ne couvre : ajouter une brique **générique** dans `core/Runtime/Effects/Bricks`, l'enregistrer dans `BrickCatalog`, écrire son test sur contenu factice (`dotnet/Vortex.Core.Tests/Bricks`), puis régénérer la documentation.
6. Mesurer l'impact **avant** de modifier le contenu : décrire le changement dans une variante (`docs/balance/variants/`), puis le comparer à la référence avec `Vortex.Simulator compare`. Voir [`balance/README.md`](balance/README.md).

Les valeurs globales (PV, bouclier de départ, fréquence des événements…) sont dans `GameConfig`, pas dans les cartes.

## Consigner un arbitrage de game design
Une question de règle que `RULES.md` ne tranche pas est **posée au game designer**, jamais devinée. Une fois la réponse obtenue :
1. L'appliquer au bon endroit :
   - le champ `ruling` de la carte, si la décision ne concerne qu'elle ;
   - `RULES.md`, partie A ou B, si elle est générique (sans jamais citer de carte) ;
   - `config.json` et la valeur ⚙ de `RULES.md`, si c'est un réglage.
2. Ajouter une entrée `ARB-xx` au [journal des arbitrages](ARBITRAGES.md) : la question, la décision, la raison donnée et l'endroit où elle est appliquée. Une entrée n'est jamais réécrite : une décision qui change fait l'objet d'une nouvelle entrée qui cite l'ancienne.
3. Retirer la question des « Questions ouvertes » de `RULES.md` si elle y figurait.
4. Écrire ou adapter le test qui fixe le comportement.

Une décision **technique** ou structurante ne va pas dans ce journal : elle fait l'objet d'un ADR (`docs/adr/`).

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
