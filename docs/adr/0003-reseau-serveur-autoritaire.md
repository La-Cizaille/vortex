# ADR-0003 : Réseau par serveur autoritaire .NET + WebSocket (phase 2)

- **Statut** : accepté (implémentation en phase 2), 2026-09-23

## Contexte
Le jeu est au tour par tour, de 2 à 5 joueurs, avec un lobby. Il n'y a pas de physique ni de synchronisation temps réel. La triche doit être impossible.

## Options
1. **Netcode for GameObjects + Unity Lobby/Relay** : pensé pour le temps réel et l'autorité par l'hôte. L'hôte pourrait tricher, sauf avec un serveur dédié Unity, qui est lourd.
2. **Photon (Fusion/PUN)** : clé en main, mais payant au-delà d'un certain nombre de joueurs simultanés, avec une dépendance à un tiers. L'autorité serveur réelle demande leur offre serveur.
3. **Serveur ASP.NET Core .NET 10 + WebSocket**, qui exécute `core` (ADR-0002).

## Décision
Option 3. Le protocole JSON reprend exactement les commandes, événements et décisions du moteur. Le lobby est géré par le serveur, avec des codes de salon.

## Conséquences
- (+) Autorité complète : le client n'envoie que des intentions et la validation est la même qu'en local.
- (+) Coût d'hébergement faible (petit VPS) et aucun frais par joueur.
- (−) Il faut écrire le lobby, la reconnexion et la limitation de débit nous-mêmes. Ces points sont couverts par `SECURITY.md`.
- Le travail de phase 1 prépare la phase 2 : état sérialisable, projection publique et décisions interrompantes.
