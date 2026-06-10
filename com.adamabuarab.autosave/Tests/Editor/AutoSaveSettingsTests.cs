using NUnit.Framework;

namespace DevTools.AutoSave.Tests
{
    [TestFixture]
    internal sealed class AutoSaveSettingsTests
    {
        [Test]
        public void DefaultValues_MatchExpectedConstants()
        {
            var s = new AutoSaveSettings();

            Assert.AreEqual(AutoSaveSettings.DefaultEnabled,           s.Enabled);
            Assert.AreEqual(AutoSaveSettings.DefaultSaveMode,          s.SaveMode);
            Assert.AreEqual(AutoSaveSettings.DefaultDelaySeconds,      s.DelaySeconds);
            Assert.AreEqual(AutoSaveSettings.DefaultSaveOnPlayMode,    s.SaveOnPlayMode);
            Assert.AreEqual(AutoSaveSettings.DefaultSaveOnCompile,     s.SaveOnCompile);
            Assert.AreEqual(AutoSaveSettings.DefaultSaveScenes,        s.SaveScenes);
            Assert.AreEqual(AutoSaveSettings.DefaultSaveAssets,        s.SaveAssets);
            Assert.AreEqual(AutoSaveSettings.DefaultShowStatusBarIcon, s.ShowStatusBarIcon);
            Assert.AreEqual(AutoSaveSettings.DefaultVerbosity,         s.Verbosity);
        }

        [Test]
        public void DelaySeconds_ClampedToMinimum()
        {
            var s = new AutoSaveSettings();
            s.DelaySeconds = -10f;
            Assert.AreEqual(0.5f, s.DelaySeconds);
        }

        [Test]
        public void DelaySeconds_ClampedToMaximum()
        {
            var s = new AutoSaveSettings();
            s.DelaySeconds = 9999f;
            Assert.AreEqual(300f, s.DelaySeconds);
        }

        [Test]
        public void IsDelayModeActive_TrueForAfterDelay()
        {
            var s = new AutoSaveSettings { SaveMode = SaveMode.AfterDelay };
            Assert.IsTrue(s.IsDelayModeActive);
            Assert.IsFalse(s.IsFocusLostModeActive);
        }

        [Test]
        public void IsFocusLostModeActive_TrueForOnFocusLost()
        {
            var s = new AutoSaveSettings { SaveMode = SaveMode.OnFocusLost };
            Assert.IsTrue(s.IsFocusLostModeActive);
            Assert.IsFalse(s.IsDelayModeActive);
        }

        [Test]
        public void BothModesActive_ForAfterDelayAndOnFocusLost()
        {
            var s = new AutoSaveSettings { SaveMode = SaveMode.AfterDelayAndOnFocusLost };
            Assert.IsTrue(s.IsDelayModeActive);
            Assert.IsTrue(s.IsFocusLostModeActive);
        }

        [Test]
        public void Clone_ProducesIndependentCopy()
        {
            var original = new AutoSaveSettings
            {
                Enabled      = false,
                DelaySeconds = 10f,
                SaveMode     = SaveMode.OnFocusLost
            };

            var clone = original.Clone();
            clone.Enabled      = true;
            clone.DelaySeconds = 5f;

            // Original must be unchanged
            Assert.IsFalse(original.Enabled);
            Assert.AreEqual(10f,                original.DelaySeconds);
            Assert.AreEqual(SaveMode.OnFocusLost, original.SaveMode);
        }

        [Test]
        public void ResetToDefaults_RestoresAllValues()
        {
            var s = new AutoSaveSettings
            {
                Enabled      = false,
                DelaySeconds = 99f,
                SaveMode     = SaveMode.OnFocusLost,
                Verbosity    = NotificationVerbosity.Silent
            };

            s.ResetToDefaults();

            Assert.AreEqual(AutoSaveSettings.DefaultEnabled,      s.Enabled);
            Assert.AreEqual(AutoSaveSettings.DefaultDelaySeconds, s.DelaySeconds);
            Assert.AreEqual(AutoSaveSettings.DefaultSaveMode,     s.SaveMode);
            Assert.AreEqual(AutoSaveSettings.DefaultVerbosity,    s.Verbosity);
        }
    }
}
