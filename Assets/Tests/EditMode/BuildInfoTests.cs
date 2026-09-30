using BulletHell.Core;
using NUnit.Framework;

namespace BulletHell.Tests
{
    /// <summary>D6: the version line shown in the Main Menu and the build menu's version format.</summary>
    public class BuildInfoTests
    {
        [Test]
        public void TheMenuLineStartsWithTheBuildVersion()
        {
            StringAssert.StartsWith("v" + BuildInfo.Version, BuildInfo.Display);
        }

        [Test]
        public void VersionsAreWrittenMajorMinorBuild()
        {
            Assert.AreEqual("1.0.12", BuildInfo.FormatVersion(1, 0, 12));
            Assert.AreEqual("2.3.1", BuildInfo.FormatVersion(2, 3, 1));
        }

        [Test]
        public void EditorDisplayMarksADevelopmentBuild()
        {
            // The Editor is a development build: the line says so, so a dev build is never mistaken for a release.
            StringAssert.Contains("dev", BuildInfo.Display);
        }
    }
}
