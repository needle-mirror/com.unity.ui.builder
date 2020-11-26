using NUnit.Framework;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Unity.UI.Builder.EditorTests
{
    class ViewportHeaderTests : BuilderIntegrationTest
    {
        /// <summary>
        /// The current UI Builder package version is displayed in the **Viewport** title bar.
        /// </summary>
#if UI_BUILDER_PACKAGE
        [Test]
        public void CurrentBuilderVersionIsDisplayedInTheTitlebar()
        {
            var packageInfo = PackageInfo.FindForAssetPath("Packages/" + BuilderConstants.BuilderPackageName);
            var builderPackageVersion = packageInfo.version;
            Assert.True(viewport.subTitle.Contains(builderPackageVersion));
        }
#endif
    }
}
