using System.IO;
using NUnit.Framework;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>Development tooling never reaches a release build (docs/SECURITY.md section 0).</summary>
    public class DevToolingTests
    {
        [Test]
        public void Dev_only_packages_are_detected_in_a_manifest()
        {
            string withMcp = "{ \"dependencies\": { \"com.unity.ugui\": \"2.6.0\", \"com.coplaydev.unity-mcp\": \"https://example/x.git#abc\" } }";
            Assert.That(DevOnlyPackages.FindIn(withMcp), Is.EqualTo(new[] { "com.coplaydev.unity-mcp" }));
            Assert.That(DevOnlyPackages.FindIn("{ \"dependencies\": { \"com.unity.ugui\": \"2.6.0\" } }"), Is.Empty);
            Assert.That(DevOnlyPackages.FindIn("{ }"), Is.Empty);
        }

        [Test]
        public void Every_dev_only_package_of_the_project_is_known()
        {
            // The project's own manifest: whatever it contains among dev-only packages is what a release build refuses.
            string manifest = File.ReadAllText(Path.Combine("Packages", "manifest.json"));
            Assert.That(DevOnlyPackages.FindIn(manifest), Is.SubsetOf(DevOnlyPackages.Names));
        }
    }
}
