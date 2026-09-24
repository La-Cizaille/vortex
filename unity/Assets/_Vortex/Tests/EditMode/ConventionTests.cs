using System.Linq;
using NUnit.Framework;
using UnityEditor;
using Vortex.Client.Content;

namespace Vortex.Tests.EditMode
{
    /// <summary>Project conventions that Unity does not enforce by itself.</summary>
    public class ConventionTests
    {
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
