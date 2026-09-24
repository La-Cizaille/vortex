using System;
using UnityEngine;
using Vortex.Core.Config;
using Vortex.Core.Content;
using Vortex.Core.Rules;

namespace Vortex.Client.Content
{
    /// <summary>
    /// The game content shipped with the client: the four files of the rules engine package (ADR-0008), referenced as
    /// text assets so that they are embedded in builds. Loading goes through the engine's validating loader.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Contenu du jeu", fileName = "GameContent")]
    public sealed class GameContent : ScriptableObject
    {
        [SerializeField] private TextAsset cards = null!;
        [SerializeField] private TextAsset events = null!;
        [SerializeField] private TextAsset technologies = null!;
        [SerializeField] private TextAsset config = null!;

        /// <summary>Loads and validates the cards, events and technologies. Throws <see cref="GameDataException"/> on invalid content.</summary>
        public GameData LoadData() => GameDataLoader.Load(Text(cards, nameof(cards)), Text(events, nameof(events)), Text(technologies, nameof(technologies)));

        /// <summary>Loads and validates the content, then builds a rules engine. Throws <see cref="GameDataException"/> on invalid content.</summary>
        public GameEngine CreateEngine()
        {
            GameData data = LoadData();
            GameConfig gameConfig = GameDataLoader.LoadConfig(Text(config, nameof(config)), data);
            return new GameEngine(data, gameConfig);
        }

        /// <summary>Assigns the four files (editor setup and tests).</summary>
        public void Assign(TextAsset cardsFile, TextAsset eventsFile, TextAsset technologiesFile, TextAsset configFile)
        {
            cards = cardsFile;
            events = eventsFile;
            technologies = technologiesFile;
            config = configFile;
        }

        private string Text(TextAsset? asset, string field)
        {
            if (asset == null)
            {
                throw new InvalidOperationException(name + ": the '" + field + "' content file is not assigned.");
            }

            return asset.text;
        }
    }
}
