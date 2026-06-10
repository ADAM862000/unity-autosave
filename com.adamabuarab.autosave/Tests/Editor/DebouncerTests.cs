using DevTools.AutoSave.Core;
using NUnit.Framework;

namespace DevTools.AutoSave.Tests
{
    [TestFixture]
    internal sealed class DebouncerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────────

        private static (Debouncer debouncer, System.Action<double> advance)
            CreateDebouncer(float delaySeconds)
        {
            double currentTime = 0;
            System.Action<double> advance = dt => currentTime += dt;
            var debouncer = new Debouncer(delaySeconds, () => currentTime);
            return (debouncer, advance);
        }

        // ── Tests ─────────────────────────────────────────────────────────────────

        [Test]
        public void Tick_WhenNoPendingSignal_DoesNotFireCallback()
        {
            var (debouncer, _) = CreateDebouncer(1f);
            bool fired = false;

            bool result = debouncer.Tick(() => fired = true);

            Assert.IsFalse(result, "Tick should return false when nothing is pending.");
            Assert.IsFalse(fired,  "Callback should not fire when no signal was sent.");
        }

        [Test]
        public void Tick_BeforeDelayElapses_DoesNotFireCallback()
        {
            var (debouncer, advance) = CreateDebouncer(2f);
            bool fired = false;
            debouncer.Arm();

            advance(1.5); // less than 2s delay
            bool result = debouncer.Tick(() => fired = true);

            Assert.IsFalse(result, "Tick should not fire before the delay has elapsed.");
            Assert.IsFalse(fired);
        }

        [Test]
        public void Tick_AfterDelayElapses_FiresCallbackOnce()
        {
            var (debouncer, advance) = CreateDebouncer(1f);
            int callCount = 0;
            debouncer.Arm();

            advance(1.1); // past the delay
            debouncer.Tick(() => callCount++);

            Assert.AreEqual(1, callCount, "Callback should fire exactly once after delay.");
        }

        [Test]
        public void Tick_AfterCallbackFired_DoesNotFireAgain()
        {
            var (debouncer, advance) = CreateDebouncer(1f);
            int callCount = 0;
            debouncer.Arm();

            advance(1.1);
            debouncer.Tick(() => callCount++);
            advance(0.1);
            debouncer.Tick(() => callCount++); // should not fire again

            Assert.AreEqual(1, callCount, "Callback should not fire again without a new Arm.");
        }

        [Test]
        public void Arm_CalledMultipleTimes_DoesNotResetTimer()
        {
            // First-edge semantics: repeated Arm() calls must NOT push the deadline out.
            var (debouncer, advance) = CreateDebouncer(2f);
            bool fired = false;
            debouncer.Arm();

            advance(1.5);
            debouncer.Arm(); // should be a no-op — clock was already ticking
            advance(0.6);    // total 2.1s from the FIRST Arm; past the 2s delay

            debouncer.Tick(() => fired = true);

            Assert.IsTrue(fired, "Arm() should not reset the timer — fires from first Arm.");
        }

        [Test]
        public void Arm_AfterCancel_StartsNewCycle()
        {
            var (debouncer, advance) = CreateDebouncer(2f);
            bool fired = false;
            debouncer.Arm();
            debouncer.Cancel();

            debouncer.Arm(); // fresh cycle
            advance(2.1);
            debouncer.Tick(() => fired = true);

            Assert.IsTrue(fired, "Arm after Cancel should start a new cycle.");
        }

        [Test]
        public void Cancel_ClearsPendingState()
        {
            var (debouncer, advance) = CreateDebouncer(1f);
            bool fired = false;
            debouncer.Arm();
            debouncer.Cancel();

            advance(2.0);
            debouncer.Tick(() => fired = true);

            Assert.IsFalse(fired, "Cancelled debounce should not fire.");
            Assert.IsFalse(debouncer.IsPending);
        }

        [Test]
        public void IsPending_ReflectsArmAndFireState()
        {
            var (debouncer, advance) = CreateDebouncer(1f);
            Assert.IsFalse(debouncer.IsPending);

            debouncer.Arm();
            Assert.IsTrue(debouncer.IsPending);

            advance(1.1);
            debouncer.Tick(() => { });
            Assert.IsFalse(debouncer.IsPending);
        }

        [Test]
        public void DelaySeconds_CanBeChangedDynamically()
        {
            var (debouncer, advance) = CreateDebouncer(5f);
            bool fired = false;
            debouncer.Arm();
            debouncer.DelaySeconds = 1f; // shorten after arming

            advance(1.1);
            debouncer.Tick(() => fired = true);

            Assert.IsTrue(fired, "Changing DelaySeconds should be respected on next evaluation.");
        }
    }
}
