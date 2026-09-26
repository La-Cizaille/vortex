using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using Vortex.Client.Content;

namespace Vortex.Tests.EditMode
{
    /// <summary>Project conventions that Unity does not enforce by itself.</summary>
    public class ConventionTests
    {
        [Test]
        public void The_client_names_no_card_event_or_technology()
        {
            // Cards, events and technologies are data (ADR-0008): the client reacts to what the engine's events say,
            // never to which card caused them, so content can be rebalanced, replaced or added without touching the
            // client (ADR-0007, ANIMATIONS.md §1). Comments may give an id as an example.
            var contentId = new Regex(@"\b(A_[0-9]{3}|D_[0-9]{3}|EVT_[A-Z_]+|TECH_(BLUE|RED|GREEN|YELLOW))\b");
            var offending = Directory.GetFiles("Assets/_Vortex/Scripts", "*.cs", SearchOption.AllDirectories)
                .SelectMany(file => File.ReadAllLines(file).Select((line, index) => (file, line, index)))
                .Where(l => !l.line.TrimStart().StartsWith("//", System.StringComparison.Ordinal) && contentId.IsMatch(l.line))
                .Select(l => l.file + ":" + (l.index + 1) + " " + l.line.Trim())
                .ToList();
            Assert.That(offending, Is.Empty);
        }

        [Test]
        public void Every_serializable_client_class_is_found_by_unity()
        {
            // Unity binds a ScriptableObject or MonoBehaviour to its script only when the class is alone in a file of
            // the same name; otherwise the assets lose their script reference (m_Script: {fileID: 0}).
            var types = typeof(GameContent).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && (t.IsSubclassOf(typeof(UnityEngine.ScriptableObject)) || t.IsSubclassOf(typeof(UnityEngine.MonoBehaviour))))
                .ToList();
            Assert.That(types, Is.Not.Empty);

            var scripts = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/_Vortex/Scripts" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid)))
                .Select(script => script.GetClass())
                .Where(c => c != null)
                .ToList();
            foreach (var type in types)
            {
                Assert.That(scripts, Does.Contain(type), type.FullName + " must be declared in " + type.Name + ".cs.");
            }
        }
    }
}
