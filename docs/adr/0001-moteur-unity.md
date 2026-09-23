# ADR-0001 : Unity 6.3 LTS comme moteur client

- **Statut** : accepté, 2026-09-23

## Contexte
Il s'agit d'un jeu de cartes et de plateau en 3D simple, façon Hearthstone. Les cibles sont Android et Windows. Le développement est assisté par IA, via des serveurs MCP qui pilotent l'éditeur.

## Options
1. **Unity 6.3 LTS** : outillage mobile mature, C#, Unity MCP (CoplayDev) maintenu, LTS supportée jusqu'en décembre 2027.
2. **Godot 4** : libre et léger, mais export mobile moins outillé et écosystème MCP moins mûr.
3. **Stack web** (TypeScript + Three.js, empaqueté pour le mobile) : partage de code facile avec un serveur Node, mais le Web n'est pas une cible et la 3D mobile y demande plus d'effort.

## Décision
Unity 6.3 LTS, pipeline URP (preset mobile).

## Conséquences
- (+) Build Android et Windows natif. Unity Personal est gratuit sous le seuil de revenus.
- (+) Le C# est partagé avec les outils .NET (ADR-0002).
- (−) Unity compile en C# 9 / netstandard2.1 : le code partagé doit rester dans ce sous-ensemble.
- (−) Les fichiers de scène sont en YAML et demandent UnityYAMLMerge pour les fusions.
