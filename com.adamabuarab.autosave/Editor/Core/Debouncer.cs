using System;
using UnityEditor;

namespace DevTools.AutoSave.Core
{
    /// <summary>
    /// A lightweight debouncer designed for use on the Unity Editor main thread.
    ///
    /// Rather than spinning up a background thread or using coroutines (unavailable
    /// in Editor-only contexts), this class tracks elapsed time using
    /// <see cref="EditorApplication.timeSinceStartup"/> and evaluates readiness
    /// each time <see cref="Tick"/> is called from an EditorApplication.update hook.
    ///
    /// Timing model: <see cref="Arm"/> records the signal time ONCE and is a no-op
    /// if a callback is already pending. The timer fires N seconds after the FIRST
    /// signal in each cycle. Call <see cref="Cancel"/> to reset the cycle.
    /// </summary>
    internal sealed class Debouncer
    {
        private readonly Func<double> _timeProvider;

        private double _armedTime = double.MinValue;
        private bool   _pending   = false;

        /// <summary>
        /// Delay in seconds that must elapse after <see cref="Arm"/> before
        /// <see cref="Tick"/> fires the callback.
        /// </summary>
        public float DelaySeconds { get; set; }

        /// <summary>True if a callback is waiting to fire.</summary>
        public bool IsPending => _pending;

        /// <param name="delaySeconds">Initial debounce delay.</param>
        /// <param name="timeProvider">
        ///   Optional time source. Defaults to <see cref="EditorApplication.timeSinceStartup"/>.
        ///   Inject a custom provider in tests to control time deterministically.
        /// </param>
        public Debouncer(float delaySeconds, Func<double> timeProvider = null)
        {
            DelaySeconds  = delaySeconds;
            _timeProvider = timeProvider ?? (() => EditorApplication.timeSinceStartup);
        }

        /// <summary>
        /// Arms the debounce timer. No-op if already pending — the clock is NOT
        /// reset on repeated calls. This ensures the callback fires N seconds after
        /// the FIRST signal, not the most recent one.
        /// </summary>
        public void Arm()
        {
            if (_pending) return;
            _armedTime = _timeProvider();
            _pending   = true;
        }

        /// <summary>
        /// Should be called every editor update frame.
        /// Fires <paramref name="callback"/> once when the debounce delay has elapsed
        /// since <see cref="Arm"/> was first called, then resets pending state.
        /// </summary>
        /// <returns>True if the callback was fired this tick.</returns>
        public bool Tick(Action callback)
        {
            if (!_pending) return false;

            double elapsed = _timeProvider() - _armedTime;
            if (elapsed < DelaySeconds) return false;

            _pending = false;
            callback?.Invoke();
            return true;
        }

        /// <summary>Cancels any pending debounced callback without firing it.</summary>
        public void Cancel()
        {
            _pending = false;
        }
    }
}
