# Auto Save

[!\[Unity 2021.3+](https://img.shields.io/badge/Unity-2021.3%2B-blue?logo=unity)](https://unity.com)
[!\[License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[!\[UPM Compatible](https://img.shields.io/badge/UPM-compatible-brightgreen)](https://docs.unity3d.com/Manual/upm-ui.html)

Smart auto save for Unity Editor. Saves scenes and assets based on configurable triggers — only when something is actually dirty.

\---

## Features

|||
|-|-|
|**After Delay**|Saves N seconds after a change is detected|
|**On Focus Lost**|Saves when the editor loses OS focus|
|**Before Play Mode**|Saves before entering Play Mode|
|**Before Compile**|Saves before script compilation|
|**Dirty-state detection**|Never saves when nothing has changed|
|**Debounced**|Won't fire while you're actively editing|
|**Toolbar indicator**|On/off status in the Scene View toolbar|
|**Hot-reload settings**|Changes apply instantly, no restart needed|

\---

## Install

**Package Manager → + → Add package from git URL:**

```
https://github.com/ADAM862000/unity-autosave.git?path=com.adamabuarab.autosave
```

**Or from disk:** Package Manager → + → Add package from disk → select `package.json`.

**Or manually:** copy `com.adamabuarab.autosave/` into your project's `Packages/` folder.

\---

## Quick Start

Works immediately after install. Default behaviour:

* Saves 3 seconds after a change, and again when you switch apps
* Saves before Play Mode and compilation
* Prints `\[AutoSave] Auto saved.` to the Console

No setup required.

\---

## Settings

**Edit → Project Settings → Auto Save**

|Setting|Default|Description|
|-|-|-|
|Enable|On|Master switch|
|Trigger Mode|After Delay + On Focus Lost|When saves fire|
|Delay|3s|How long to wait after a change (0.5–300s)|
|Save Before Play Mode|On|Saves before entering Play Mode|
|Save Before Compile|On|Saves before script compilation|
|Save Scenes|On|Includes open scenes|
|Save Assets|On|Includes prefabs, ScriptableObjects, etc.|
|Console Output|Minimal|Silent / Minimal / Verbose|
|Show Toolbar Status|On|Status indicator in Scene View|

Untitled scenes (not yet saved to disk) are always skipped.

\---

## Menu

**Tools → Auto Save**

* **Open Settings** — opens the settings panel
* **Enable Auto Save** — toggles on/off
* **Save Now** — manual immediate save

\---

## Architecture

```
com.adamabuarab.autosave/
├── Editor/
│   ├── Core/
│   │   ├── AutoSaveController.cs       # Orchestrator
│   │   ├── AutoSaveEditorHooks.cs      # Unity lifecycle bridge
│   │   ├── AutoSaveLogger.cs           # Logging
│   │   ├── Debouncer.cs                # Timer utility
│   │   ├── DirtyStateDetector.cs       # Dirty-state detection
│   │   └── SaveExecutor.cs             # Save operations
│   ├── Settings/
│   │   ├── AutoSaveSettings.cs         # Settings model
│   │   └── AutoSaveSettingsRepository.cs
│   └── UI/
│       ├── AutoSaveMenuItems.cs
│       ├── AutoSaveSettingsProvider.cs
│       └── AutoSaveToolbarOverlay.cs
└── Tests/Editor/
    ├── AutoSaveControllerTests.cs
    ├── AutoSaveSettingsTests.cs
    ├── DebouncerTests.cs
    └── SaveResultTests.cs
```

**Settings** are stored in `EditorPrefs` as JSON — no `.asset` files, no source control noise.

**Controller and hooks are separate** so all save logic is testable without a running Unity editor.

**The debounce timer arms once** on the first dirty frame, then runs without resetting. Saves fire N seconds after the change was *first* detected — repeated edits do not push the deadline out.

\---

## Extending

Replace `IDirtyStateDetector` or `ISaveExecutor` with your own implementation:

```csharp
public sealed class MyExecutor : ISaveExecutor
{
    public SaveResult Execute(AutoSaveSettings settings)
    {
        // custom save logic
        return new SaveResult(true, scenesSaved: 1, assetsSaved: 0);
    }
}
```

Both interfaces are in `DevTools.AutoSave.Core`.

\---

## Troubleshooting

**Not saving automatically**

* Confirm it's enabled: Edit → Project Settings → Auto Save
* The scene must be dirty (asterisk in title bar)
* Untitled scenes are skipped by design

**"Auto saved." appears but the file timestamp didn't change**

* `AssetDatabase.SaveAssets()` batches writes. The file is likely already saved.

**Toolbar overlay missing**

* Requires Unity 2021.2+
* Check Show Toolbar Status is on in settings
* Right-click the Scene View toolbar to confirm the overlay is enabled

**Editor hitching on save**

* Raise the delay to 10–30s
* Disable Save Assets if you have many dirty ScriptableObjects
* Switch to On Focus Lost mode

\---

## Requirements

* Unity 2021.3+
* No dependencies

\---

## License

MIT — see [LICENSE](LICENSE).

