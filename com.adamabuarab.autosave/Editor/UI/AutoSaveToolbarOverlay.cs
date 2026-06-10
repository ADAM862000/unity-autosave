using DevTools.AutoSave.Core;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace DevTools.AutoSave.UI
{
#if UNITY_2021_2_OR_NEWER
    /// <summary>
    /// Scene View toolbar overlay that shows the current auto save state.
    /// Clicking it opens Project Settings → Auto Save.
    ///
    /// Performance: the Refresh callback does a simple bool check + string
    /// equality before touching any UIElements properties — effectively free
    /// on frames where the state has not changed.
    /// </summary>
    [Overlay(typeof(SceneView), "adam-autosave-status", "Auto Save",
        defaultDisplay        = true,
        defaultDockZone       = DockZone.TopToolbar,
        defaultDockPosition   = DockPosition.Bottom)]
    internal sealed class AutoSaveToolbarOverlay : Overlay
    {
        private Label  _statusLabel;
        private bool   _lastEnabledState  = true;
        private bool   _lastDisplayedState = true;

        // ── Overlay lifecycle ─────────────────────────────────────────────────────

        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignItems   = Align.Center;
            root.style.paddingLeft  = 4;
            root.style.paddingRight = 4;

            _statusLabel = new Label
            {
                text    = GetStatusText(),
                tooltip = "Auto Save — click to open settings."
            };
            _statusLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
            _statusLabel.style.fontSize = 11;
            _statusLabel.style.color    = GetStatusColor();

            _statusLabel.RegisterCallback<ClickEvent>(_ =>
                SettingsService.OpenProjectSettings("Project/Auto Save"));

            root.Add(_statusLabel);

            EditorApplication.update += Refresh;

            return root;
        }

        public override void OnWillBeDestroyed()
        {
            EditorApplication.update -= Refresh;
            base.OnWillBeDestroyed();
        }

        // ── Refresh (called every editor update) ──────────────────────────────────

        private void Refresh()
        {
            if (_statusLabel == null) return;

            var settings = AutoSaveEditorHooks.CurrentSettings;
            if (settings == null) return;

            bool shouldDisplay = settings.ShowStatusBarIcon;
            bool isEnabled     = settings.Enabled;

            // Only touch UIElements properties when state actually changed.
            // Avoids triggering layout/repaint on every frame (~100 Hz).
            if (shouldDisplay != _lastDisplayedState)
            {
                displayed           = shouldDisplay;
                _lastDisplayedState = shouldDisplay;
            }

            if (!shouldDisplay) return;

            if (isEnabled != _lastEnabledState)
            {
                _statusLabel.text        = GetStatusText(isEnabled);
                _statusLabel.style.color = GetStatusColor(isEnabled);
                _lastEnabledState        = isEnabled;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string GetStatusText()
        {
            var s = AutoSaveEditorHooks.CurrentSettings;
            return GetStatusText(s != null && s.Enabled);
        }

        private static string GetStatusText(bool enabled)
            => enabled ? "💾 Auto Save On" : "⏸ Auto Save Off";

        private static StyleColor GetStatusColor(bool enabled)
            => new StyleColor(enabled
                ? new Color(0.4f, 0.9f, 0.4f)
                : new Color(0.7f, 0.7f, 0.7f));

        private static StyleColor GetStatusColor()
        {
            var s = AutoSaveEditorHooks.CurrentSettings;
            return GetStatusColor(s != null && s.Enabled);
        }
    }
#endif
}
