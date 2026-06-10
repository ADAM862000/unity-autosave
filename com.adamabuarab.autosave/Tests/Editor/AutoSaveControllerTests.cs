using DevTools.AutoSave.Core;
using NUnit.Framework;

namespace DevTools.AutoSave.Tests
{
    // ── Test doubles ──────────────────────────────────────────────────────────────

    internal sealed class FakeDirtyDetector : IDirtyStateDetector
    {
        public bool DirtyScenes { get; set; }
        public bool DirtyAssets { get; set; }

        public bool HasDirtyScenes()    => DirtyScenes;
        public bool HasDirtyAssets()    => DirtyAssets;
        public bool HasAnyDirtyState()  => DirtyScenes || DirtyAssets;
    }

    internal sealed class FakeSaveExecutor : ISaveExecutor
    {
        public int  ExecuteCallCount { get; private set; }
        public bool ShouldFail       { get; set; }

        public SaveResult Execute(AutoSaveSettings settings)
        {
            ExecuteCallCount++;
            return ShouldFail
                ? new SaveResult(false, 0, 0, "Simulated failure")
                : new SaveResult(true, 1, 0);
        }
    }

    // ── Tests ─────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal sealed class AutoSaveControllerTests
    {
        private FakeDirtyDetector  _dirtyDetector;
        private FakeSaveExecutor   _saveExecutor;
        private AutoSaveSettings   _settings;
        private double             _fakeTime;
        private AutoSaveController _controller;

        [SetUp]
        public void SetUp()
        {
            _dirtyDetector = new FakeDirtyDetector();
            _saveExecutor  = new FakeSaveExecutor();
            _fakeTime      = 0;
            _settings = new AutoSaveSettings
            {
                Enabled        = true,
                SaveMode       = SaveMode.AfterDelayAndOnFocusLost,
                DelaySeconds   = 1f,
                SaveScenes     = true,
                SaveAssets     = true,
                SaveOnPlayMode = true,
                SaveOnCompile  = true
            };

            _controller = new AutoSaveController(
                _settings,
                _dirtyDetector,
                _saveExecutor,
                () => _fakeTime);
        }

        [TearDown]
        public void TearDown() => _controller?.Dispose();

        // ── Delay mode ─────────────────────────────────────────────────────────────

        [Test]
        public void OnEditorUpdate_WhenDirtyAndDelayElapsed_CallsSave()
        {
            // Frame 1: was clean, now dirty → arms the debounce timer
            _dirtyDetector.DirtyScenes = true;
            _controller.OnEditorUpdate();

            // Advance time past the delay, then tick
            _fakeTime += 1.1;
            _controller.OnEditorUpdate();

            Assert.AreEqual(1, _saveExecutor.ExecuteCallCount);
        }

        [Test]
        public void OnEditorUpdate_WhenNotDirty_DoesNotCallSave()
        {
            _dirtyDetector.DirtyScenes = false;
            _dirtyDetector.DirtyAssets = false;

            _controller.OnEditorUpdate();
            _fakeTime += 2.0;
            _controller.OnEditorUpdate();

            Assert.AreEqual(0, _saveExecutor.ExecuteCallCount);
        }

        [Test]
        public void OnEditorUpdate_WhenDisabled_DoesNotCallSave()
        {
            _settings.Enabled = false;
            _controller.ApplySettings(_settings);
            _dirtyDetector.DirtyScenes = true;

            _controller.OnEditorUpdate();
            _fakeTime += 2.0;
            _controller.OnEditorUpdate();

            Assert.AreEqual(0, _saveExecutor.ExecuteCallCount);
        }

        [Test]
        public void OnEditorUpdate_BeforeDelayElapsed_DoesNotCallSave()
        {
            _dirtyDetector.DirtyScenes = true;
            _controller.OnEditorUpdate(); // arms timer

            _fakeTime += 0.5; // less than 1s delay
            _controller.OnEditorUpdate();

            Assert.AreEqual(0, _saveExecutor.ExecuteCallCount);
        }

        [Test]
        public void OnEditorUpdate_DoesNotSaveMultipleTimes_AfterSingleDirtyEdge()
        {
            // Arm the timer on first dirty frame
            _dirtyDetector.DirtyScenes = true;
            _controller.OnEditorUpdate();

            // Elapse delay — should save once
            _fakeTime += 1.1;
            _controller.OnEditorUpdate();
            Assert.AreEqual(1, _saveExecutor.ExecuteCallCount);

            // Continue ticking — should NOT save again without a new dirty edge
            _fakeTime += 2.0;
            _controller.OnEditorUpdate();
            Assert.AreEqual(1, _saveExecutor.ExecuteCallCount, "Should not save again without new dirty transition.");
        }

        [Test]
        public void OnEditorUpdate_SavesAgain_AfterNewDirtyEdge()
        {
            // First edit → save
            _dirtyDetector.DirtyScenes = true;
            _controller.OnEditorUpdate();
            _fakeTime += 1.1;
            _controller.OnEditorUpdate();
            Assert.AreEqual(1, _saveExecutor.ExecuteCallCount);

            // Simulate: save made it clean, user edits again (clean → dirty transition)
            _dirtyDetector.DirtyScenes = false;
            _controller.OnEditorUpdate(); // clean frame resets edge detector
            _dirtyDetector.DirtyScenes = true;
            _controller.OnEditorUpdate(); // dirty again → re-arms timer
            _fakeTime += 1.1;
            _controller.OnEditorUpdate();

            Assert.AreEqual(2, _saveExecutor.ExecuteCallCount);
        }

        // ── Focus lost ─────────────────────────────────────────────────────────────

        [Test]
        public void OnFocusLost_WhenDirtyAndFocusModeActive_CallsSave()
        {
            _dirtyDetector.DirtyScenes = true;
            _controller.OnFocusLost();
            Assert.AreEqual(1, _saveExecutor.ExecuteCallCount);
        }

        [Test]
        public void OnFocusLost_WhenNotDirty_DoesNotCallSave()
        {
            _controller.OnFocusLost();
            Assert.AreEqual(0, _saveExecutor.ExecuteCallCount);
        }

        [Test]
        public void OnFocusLost_WhenFocusModeDisabled_DoesNotCallSave()
        {
            _settings.SaveMode = SaveMode.AfterDelay;
            _controller.ApplySettings(_settings);
            _dirtyDetector.DirtyScenes = true;

            _controller.OnFocusLost();
            Assert.AreEqual(0, _saveExecutor.ExecuteCallCount);
        }

        // ── Play Mode ──────────────────────────────────────────────────────────────

        [Test]
        public void OnBeforePlayMode_WhenDirtyAndEnabled_CallsSave()
        {
            _dirtyDetector.DirtyScenes = true;
            _controller.OnBeforePlayMode();
            Assert.AreEqual(1, _saveExecutor.ExecuteCallCount);
        }

        [Test]
        public void OnBeforePlayMode_WhenDisabled_DoesNotCallSave()
        {
            _settings.SaveOnPlayMode = false;
            _controller.ApplySettings(_settings);
            _dirtyDetector.DirtyScenes = true;

            _controller.OnBeforePlayMode();
            Assert.AreEqual(0, _saveExecutor.ExecuteCallCount);
        }

        // ── Compilation ────────────────────────────────────────────────────────────

        [Test]
        public void OnBeforeCompile_WhenDirtyAndEnabled_CallsSave()
        {
            _dirtyDetector.DirtyAssets = true;
            _controller.OnBeforeCompile();
            Assert.AreEqual(1, _saveExecutor.ExecuteCallCount);
        }

        [Test]
        public void OnBeforeCompile_WhenSaveOnCompileDisabled_DoesNotCallSave()
        {
            _settings.SaveOnCompile = false;
            _controller.ApplySettings(_settings);
            _dirtyDetector.DirtyAssets = true;

            _controller.OnBeforeCompile();
            Assert.AreEqual(0, _saveExecutor.ExecuteCallCount);
        }

        // ── SaveCompleted event ────────────────────────────────────────────────────

        [Test]
        public void SaveCompleted_IsRaisedAfterSuccessfulSave()
        {
            SaveResult? received = null;
            _controller.SaveCompleted += r => received = r;
            _dirtyDetector.DirtyScenes = true;

            _controller.OnFocusLost();

            Assert.IsNotNull(received);
            Assert.IsTrue(received.Value.Success);
        }

        [Test]
        public void SaveCompleted_IsRaisedAfterFailedSave()
        {
            _saveExecutor.ShouldFail = true;
            SaveResult? received = null;
            _controller.SaveCompleted += r => received = r;
            _dirtyDetector.DirtyScenes = true;

            _controller.OnFocusLost();

            Assert.IsNotNull(received);
            Assert.IsFalse(received.Value.Success);
        }

        // ── ApplySettings ──────────────────────────────────────────────────────────

        [Test]
        public void ApplySettings_NullSettings_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() => _controller.ApplySettings(null));
        }

        // ── Dispose ────────────────────────────────────────────────────────────────

        [Test]
        public void Dispose_PreventsSubsequentSaves()
        {
            _dirtyDetector.DirtyScenes = true;
            _controller.Dispose();

            _controller.OnFocusLost();
            _controller.OnBeforePlayMode();
            _controller.OnBeforeCompile();

            Assert.AreEqual(0, _saveExecutor.ExecuteCallCount);
        }

        [Test]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                _controller.Dispose();
                _controller.Dispose();
            });
        }
    }
}
