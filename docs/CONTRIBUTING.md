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
Le code ne référence **jamais** un asset directement : tout passe par les catalogues de `unity/Assets/_Vortex/Theme/`. Tant qu'un visuel manque, un visuel provisoire généré le remplace et le jeu fonctionne. Les chemins ci-dessous partent de `unity/Assets/_Vortex/`.

| Pour changer… | Où | Comment |
|---|---|---|
| L'illustration d'une carte, d'un événement ou d'une technologie | `Art/Cards/<ID>.png` | Déposer l'image, nommée avec l'identifiant de [`CARDS.md`](CARDS.md) : `A_005.png`, `EVT_TROU_NOIR.png`, `TECH_BLUE.png`. À sa première importation, elle reçoit les réglages mobiles (sprite, 1024 pixels au plus, compression ASTC 6×6 sous Android), qu'on peut ajuster ensuite, et elle entre dans `Theme/CardArtCatalog`. La supprimer ramène le visuel provisoire. Un fichier dont le nom n'est pas un identifiant est signalé dans la console et ignoré. Le menu *Vortex → Habillage → Vérifier les illustrations* liste les identifiants encore sans image. |
| L'aspect d'une carte | `Prefabs/Card.prefab`, `Theme/Materials/Card*.mat` | La carte est un objet 3D (ADR-0017) : éditer le prefab (corps, position des textes et de l'illustration, badge) et ses matériaux (couleurs, dos, shader *Unlit* ou *Lit*). Un modèle de carte peut remplacer le corps. Le code ne fait que remplir ses éléments. |
| Un vaisseau | `Art/Ships/<Nom>.fbx`, puis `Theme/ShipCatalog` | Déposer le modèle (importé sans caméras ni lumières), en faire un prefab si besoin, puis l'associer au vaisseau par défaut ou à un siège. |
| Les couleurs, les polices et la vitesse des animations | `Theme/ThemeSettings` | Éditer dans l'inspecteur. En mode Play dans la scène Galerie, le changement se voit aussitôt. |
| Les pictogrammes des actions et les autres icônes | `Art/Icons/<Nom>.png`, puis `Theme/IconCatalog` | Déposer l'image avec le nom attendu ([`ASSETS.md`](ASSETS.md) §3, par exemple `Action_Attaque.png`). Elle reçoit les réglages d'icône et entre seule dans le catalogue ; le bouton l'affiche à la place de son nom court. |
| La place des commandes du joueur | Scène `Game`, objets `Joueur` (arc d'actions, combo, jeton de surcharge), `Marché noir` (boutons du marché) et `Fin de tour` | Déplacer ou redimensionner dans la scène : le code ne fait que les remplir et les allumer. L'arc d'actions se règle dans le composant `ActionArc` de l'objet `Joueur` (centre, rayon, ouverture, écart entre le côté attaque et le côté bouclier, ordre des actions de chaque côté) ; clic droit sur le composant, *Disposer les actions*, pour voir le résultat dans l'éditeur. |
| Les icônes dans le texte des cartes (`<ATQ>`, `<BOU>`…) | `Theme/ThemeSettings`, champ *Text Icons* | Créer un sprite asset TextMeshPro dont les sprites s'appellent `ATQ`, `BOU`, `DIC`, `MKT`, `MOD`, `SUR` et `TOR`, puis le choisir dans le thème. Sans lui, les icônes sont omises et le texte reste lisible. |
| Les textes de l'interface | `Content/TextTable` | Éditer le texte de chaque clé. Le nom et le texte des cartes viennent du contenu du jeu, pas de cette table. Les clés `log.*` sont les lignes du journal : `{0}` joueur, `{1}` autre joueur, `{2}` quantité, `{3}` valeur, `{4}` carte, événement ou effet, `{5}` dés. Un texte vide retire l'événement du journal. |
| La disposition de la table | `Scenes/Game.unity`, `Prefabs/SeatPanel.prefab` | Le bandeau, le marché, le panneau du joueur, le journal et les boutons sont des objets de la scène ; le panneau d'un adversaire est un prefab. Les vaisseaux se placent par leur position à l'écran, dans les réglages *Disposition* de l'objet `Partie` (0,0 en bas à gauche, 1,1 en haut à droite). |
| Le vaisseau d'un joueur éliminé, le cadre du tour, le jeton de surcharge | `Theme/ThemeSettings` | Couleurs *Wreck*, *Highlight* et *Overcharge*. |
| Les dés | `Presentation/Feedback/Dice`, `Prefabs/DiceTray.prefab` | Le retour visuel `Dice` règle la durée du lancer, le temps d'affichage du résultat et l'endroit où il apparaît ; le prefab règle l'apparence des dés. Les faces qui défilent pendant le lancer sont décoratives : le résultat vient toujours du moteur. |
| La taille de la carte agrandie | Objet `Zoom` de la scène `Game` | Réglages *Screen Height*, *Depth* et *Margin* du composant `CardZoom`. |
| Les menus (accueil, partie locale, développement, options) | `Scenes/Menu.unity` | Les fenêtres et les boutons sont des objets de la scène : les déplacer ou les redimensionner. Le code ne fait que les remplir (textes de `Content/TextTable`) et les brancher. |
| Les fenêtres de la partie (pause, fin de partie, « Tour de X ») | Scène `Game`, calque `Premier plan` | Même principe ; la durée du bandeau « Tour de X » se règle dans son composant `TurnAnnouncement`. |
| La réaction à un événement du jeu (attaque, soin, élimination…) | `Presentation/Feedback/DefaultFeedbackProfile` | Le profil dit quoi jouer pour chaque type d'événement (ADR-0014). On crée un retour visuel (*Create → Vortex → Retours visuels*), par exemple un `PrefabFeedback` qui fait apparaître des particules ou une séquence Timeline sur le vaisseau visé, puis on le branche dans le profil. |

**Scène Galerie** (`Scenes/Gallery.unity`) : en mode Play, elle montre toutes les cartes, tous les événements, toutes les technologies et le vaisseau de chaque siège, avec le thème et les illustrations du moment. Elle sert à régler l'apparence sans jouer une partie ; elle ne fait pas partie du jeu livré.

Un asset de base supprimé par erreur se recrée avec le menu *Vortex → Développement → Créer les assets de base manquants*. Ce menu ne remplace jamais un asset existant.

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

## Outils Unity sans l'éditeur

Ces scripts lancent Unity en mode batch : l'éditeur doit être **fermé** (ils le vérifient et s'arrêtent sinon).

| Script | Rôle |
|---|---|
| `tools/Test-Unity.ps1 [-Platform PlayMode]` | Lance les tests Unity et affiche le résumé. |
| `tools/Update-UnityAssets.ps1` | Crée les assets de base qui manquent (thème, catalogues, prefabs, scènes, textes) et complète la table des textes. Ne remplace jamais un asset existant : pour en régénérer un, le supprimer d'abord. |
| `tools/Capture-Unity.ps1 -Scene Game\|Gallery\|Menu -Out <fichier.png> [-Round N] [-Human] [-Phase …]` | Enregistre une image 1920×1080 de la table (des bots jouent jusqu'à la manche demandée), de la galerie ou du menu. Table : avec `-Human`, le premier siège est une personne, et l'image montre son tour après le marché, pendant (`-Phase Market`), ou pendant la visée d'une attaque avec son aperçu (`-Phase Aim`) ; `-Phase Pause` montre le menu de pause, `-Phase End` la fin de partie, `-Phase Log` le journal ouvert. Menu : l'accueil, ou `-Phase Local`, `Dev`, `Options`. Pratique pour vérifier une disposition, une illustration ou un modèle en contexte. |

Chaque commande se lance avec `powershell -ExecutionPolicy Bypass -File <script>`.

## Blender (modèles 3D)

La chaîne de production est décrite dans l'ADR-0016, les visuels à créer et leurs formats dans [`ASSETS.md`](ASSETS.md).

**Installer, une fois par poste**
1. Blender LTS 4.5 : `winget install BlenderFoundation.Blender.LTS.4.5`. La 5.2 est possible, c'est une question ouverte d'ASSETS §5.
2. L'extension Blender MCP, dans la même version que le serveur de `.mcp.json` : `uvx --from mcp-for-blender==2.0.4 mcp-for-blender install-addon`. Puis, dans Blender : *Edit → Preferences → Add-ons*, activer **MCP for Blender**.
3. Dans les préférences de l'extension :
   - laisser la télémétrie **décochée** ;
   - dans le panneau de l'extension, laisser **décochées** les intégrations de téléchargement (Poly Haven, Sketchfab, Poly Pizza, Hyper3D, Hunyuan3D), sauf décision contraire (ASSETS §5).
4. Laisser **désactivé** *Edit → Preferences → Save & Load → Auto Run Python Scripts* (réglage par défaut) : un `.blend` peut contenir des scripts.

**Travailler avec l'assistant**
1. Ouvrir Blender, appuyer sur `N` dans la vue 3D, ouvrir l'onglet **MCP for Blender** et cliquer sur **Start MCP Server**. L'extension écoute alors sur 127.0.0.1:9876.
2. Démarrer Claude Code à la racine du dépôt, puis approuver le serveur `blender` de `.mcp.json`. Il tourne en **mode sûr** (`BLENDER_MCP_SAFE_MODE=1`) : chaque script est contrôlé avant de s'exécuter.
3. Arrêter le serveur de l'extension à la fin de l'atelier : il n'a pas d'authentification.

**Du modèle au jeu**
1. Enregistrer la source dans `art-src/` (par exemple `art-src/ships/Ship_Faucon.blend`). Elle est stockée avec Git LFS.
2. Exporter : `blender --background art-src/ships/Ship_Faucon.blend --python tools/blender/export_unity.py -- unity/Assets/_Vortex/Art/Ships/Ship_Faucon.fbx`. Le script refuse d'exporter si l'échelle n'est pas appliquée ou si le budget de triangles est dépassé.
3. Dans Unity, associer le modèle à un siège ou au vaisseau par défaut dans `Theme/ShipCatalog`.
4. Vérifier le résultat dans la scène Galerie, ou avec `tools/Capture-Unity.ps1`.

## Fusion des fichiers Unity
Ajouter UnityYAMLMerge dans votre configuration Git locale (le chemin dépend de votre version d'Unity) :
```
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "'C:/Program Files/Unity/Hub/Editor/<version>/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"
```

## Sécurité
Lire [`SECURITY.md`](SECURITY.md) avant d'ajouter une dépendance, une entrée externe ou un service.
