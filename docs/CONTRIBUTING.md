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

## Installer Unity
- Version : celle de `unity/ProjectSettings/ProjectVersion.txt`, aujourd'hui **6000.6.3f1**. Politique : la dernière version Update pendant le développement, la LTS du moment avant toute publication (ADR-0013). Modules : *Android Build Support* et *Windows Build Support (IL2CPP)*.
- **Dossier d'installation** : l'emplacement par défaut du Hub (`C:\Program Files\Unity\Hub\Editor`) convient. Évitez les dossiers profonds : des fichiers de paquets internes peuvent y dépasser la limite Windows de 260 caractères, l'installeur les abandonne sans rien signaler, et le projet ne compile plus. C'est arrivé avec le paquet URP, installé sous `AppData\Local\Programs`.
- **Tests Unity** : `powershell -ExecutionPolicy Bypass -File tools/Test-Unity.ps1` (EditMode par défaut, `-Platform PlayMode` pour les tests en jeu). L'option `-ExecutionPolicy Bypass` ne vaut que pour ce lancement : sans elle, Windows PowerShell refuse les scripts locaux. Le script trouve l'éditeur d'après la version du projet ; la variable `UNITY_EDITOR` permet d'en désigner un autre.
- Le projet est dans `unity/`. Ses paquets sont épinglés dans `unity/Packages/manifest.json`, et le moteur y est référencé comme paquet local (`file:../../core`).
- **Unity Hub installé depuis le Microsoft Store** : Windows isole ses fichiers, et la licence activée dans le Hub reste dans son dossier privé (`%LOCALAPPDATA%\Packages\UnityTechnologies.UnityHub_…\LocalCache\Local\Unity\licenses\`). L'éditeur lancé en ligne de commande (tests, builds) ne la voit pas. Il faut copier `UnityEntitlementLicense.xml` dans `%LOCALAPPDATA%\Unity\licenses\`, puis refaire cette copie quand le Hub renouvelle la licence. La version classique du Hub n'a pas ce problème.

## Unity MCP (outil de développement)
Unity MCP permet à un assistant IA de piloter l'éditeur : scènes, objets, mode Play, console, captures, tests. C'est un outil de développement (SECURITY.md §0) :
- Le paquet Unity est épinglé sur un commit (`com.coplaydev.unity-mcp`, v10.2.0), et le serveur MCP sur sa version PyPI (`mcpforunityserver==10.2.0`, dans `.mcp.json`).
- **Mise en route**, une fois par poste :
  1. Installer [uv](https://docs.astral.sh/uv/) (`winget install astral-sh.uv`).
  2. Ouvrir le projet dans Unity, puis lancer le menu *Vortex → Développement → Configurer Unity MCP*. Ce menu choisit le transport stdio et coupe la télémétrie de l'éditeur. Redémarrer ensuite l'éditeur.
  3. Démarrer ou redémarrer Claude Code à la racine du dépôt, et approuver le serveur `unity` déclaré dans `.mcp.json`.
- **Fonctionnement** : Claude Code lance le serveur, qui parle au pont de l'éditeur sur `127.0.0.1:6400`. Le pont n'écoute que sur la machine, mais **sans authentification** : tout programme local peut le piloter tant que l'éditeur est ouvert. La télémétrie est coupée des deux côtés : variables d'environnement pour le serveur, préférence de l'éditeur pour le pont.
- **Jamais dans un produit livré** : le paquet contient un petit assembly runtime (utilitaires passifs). `ReleaseBuildGuard` fait donc échouer tout build de publication tant qu'un paquet réservé au développement est installé. Les builds de développement restent possibles.
- Pour une montée de version : relire le changelog, mettre à jour le commit, la version du serveur et, si elles changent, les clés de préférence dans `UnityMcpSetup`.

## Fusion des fichiers Unity
Ajouter UnityYAMLMerge dans votre configuration Git locale (le chemin dépend de votre version d'Unity) :
```
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "'C:/Program Files/Unity/Hub/Editor/<version>/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"
```

## Sécurité
Lire [`SECURITY.md`](SECURITY.md) avant d'ajouter une dépendance, une entrée externe ou un service.
