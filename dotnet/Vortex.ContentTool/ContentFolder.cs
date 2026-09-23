using System.IO;
using Vortex.Core.Content;

namespace Vortex.ContentTool
{
    /// <summary>Reads the content files of a data folder (the only place the tool touches the file system for input).</summary>
    internal sealed class ContentFolder
    {
        private const long MaxFileBytes = ContentJson.MaxInputChars * 4L; // UTF-8 worst case

        public ContentFolder(string directory)
        {
            Directory = directory;
            CardsJson = Read(CardsFile.FileName);
            EventsJson = Read(EventsFile.FileName);
            TechnologiesJson = Read(TechnologiesFile.FileName);
            ConfigJson = Read(Vortex.Core.Config.GameConfig.FileName);
        }

        public string Directory { get; }

        public string CardsJson { get; }

        public string EventsJson { get; }

        public string TechnologiesJson { get; }

        public string ConfigJson { get; }

        /// <summary>Loads and validates every content file, including the configuration.</summary>
        public GameData Load()
        {
            GameData data = GameDataLoader.Load(CardsJson, EventsJson, TechnologiesJson);
            GameDataLoader.LoadConfig(ConfigJson, data);
            return data;
        }

        public string PathOf(string fileName)
        {
            return Path.Combine(Directory, fileName);
        }

        private string Read(string fileName)
        {
            var info = new FileInfo(PathOf(fileName));
            if (!info.Exists)
            {
                throw new IOException("Missing content file: " + info.FullName);
            }

            if (info.Length > MaxFileBytes)
            {
                throw new InvalidDataException(fileName + " is too large.");
            }

            return File.ReadAllText(info.FullName);
        }
    }
}
