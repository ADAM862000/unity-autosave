using DevTools.AutoSave.Core;
using NUnit.Framework;

namespace DevTools.AutoSave.Tests
{
    [TestFixture]
    internal sealed class SaveResultTests
    {
        [Test]
        public void AnythingSaved_TrueWhenScenesSaved()
        {
            var result = new SaveResult(true, 1, 0);
            Assert.IsTrue(result.AnythingSaved);
        }

        [Test]
        public void AnythingSaved_TrueWhenAssetsSaved()
        {
            var result = new SaveResult(true, 0, 3);
            Assert.IsTrue(result.AnythingSaved);
        }

        [Test]
        public void AnythingSaved_FalseWhenNothingSaved()
        {
            var result = new SaveResult(true, 0, 0);
            Assert.IsFalse(result.AnythingSaved);
        }

        [Test]
        public void ToString_SuccessCase_ContainsSavedText()
        {
            var result = new SaveResult(true, 2, 3);
            StringAssert.Contains("2", result.ToString());
            StringAssert.Contains("3", result.ToString());
        }

        [Test]
        public void ToString_FailureCase_ContainsErrorMessage()
        {
            var result = new SaveResult(false, 0, 0, "disk full");
            StringAssert.Contains("disk full", result.ToString());
        }
    }
}
