using UnityEngine;

namespace DevTools.AutoSave.Core
{
    /// <summary>
    /// Verbosity-aware logging facade.
    /// All output goes to the standard Unity Console via Debug.Log/LogWarning/LogError.
    ///
    /// This is a static class deliberately — it holds no state and needs no lifetime
    /// management. All filtering is done at call site to avoid string interpolation
    /// allocations when logging is suppressed.
    /// </summary>
    internal static class AutoSaveLogger
    {
        private const string Tag = "[AutoSave]";

        internal static void LogSaveResult(in SaveResult result, NotificationVerbosity verbosity)
        {
            if (verbosity == NotificationVerbosity.Silent || !result.AnythingSaved)
                return;

            if (verbosity == NotificationVerbosity.Minimal)
                Debug.Log($"{Tag} Auto saved.");
            else
                Debug.Log($"{Tag} {result}");
        }

        /// <summary>Only logs when verbosity is Verbose. Avoids string allocation otherwise.</summary>
        internal static void LogVerbose(string message, NotificationVerbosity verbosity)
        {
            if (verbosity == NotificationVerbosity.Verbose)
                Debug.Log($"{Tag} {message}");
        }

        internal static void LogWarning(string message) =>
            Debug.LogWarning($"{Tag} {message}");

        internal static void LogError(string message) =>
            Debug.LogError($"{Tag} {message}");
    }
}
