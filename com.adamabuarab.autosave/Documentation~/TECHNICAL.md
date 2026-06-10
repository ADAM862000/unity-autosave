# Auto Save — Technical Reference

## Architecture

Three layers:

```
┌──────────────────────────────────────┐
│  UI Layer                            │
│  SettingsProvider · Overlay · Menu   │
└──────────────┬───────────────────────┘
               │
┌──────────────▼───────────────────────┐
│  Settings Layer                      │
│  AutoSaveSettings · Repository       │
└──────────────┬───────────────────────┘
               │
┌──────────────▼───────────────────────┐
│  Core Layer                          │
│  Controller · Hooks · Debouncer      │
│  DirtyDetector · SaveExecutor        │
└──────────────────────────────────────┘
```

**Controller** holds all save logic. No Unity static API calls — fully unit-testable.

**Hooks** (`[InitializeOnLoad]`) wires Unity callbacks to the controller and owns its lifetime.

**Debouncer** arms once on the clean→dirty edge via `Arm()`, which is a no-op if already
pending. The timer fires N seconds after the FIRST signal in each cycle — repeated dirty
frames do not reset the clock. Call `Cancel()` to end a cycle.

**Settings** live in `EditorPrefs` as JSON. No `.asset` files, no source control noise.

---

## Save Safety

- Untitled scenes are skipped — no blocking Save Dialog
- Dirty check runs before every save — no no-op writes
- `CompilationPipeline.compilationStarted` fires before domain reload — safest moment to save
- `isPlayingOrWillChangePlaymode` guard in the executor blocks saves during any Play Mode transition

---

## Performance

| Path | Frequency | Cost |
|---|---|---|
| `OnEditorUpdate` | ~100 Hz | Two bool checks |
| `HasDirtyScenes()` | When pending | O(loaded scenes) |
| `HasDirtyAssets()` | Only if scenes clean | O(scenes × root objects) |
| `Debouncer.Tick()` | When pending | One float comparison |
| `ExecuteSave()` | On trigger | Standard Unity I/O |

`HasDirtyScenes()` short-circuits `HasDirtyAssets()` in the common case. No `foreach` on arrays — indexed loops only.

---

## Testing

Pure NUnit, Edit Mode only. No `[UnityTest]`, no coroutines, no async.

Fakes: `FakeDirtyDetector`, `FakeSaveExecutor`, injected `Func<double>` time provider.

---

## Extension Points

```csharp
// Custom dirty detection
public sealed class MyDetector : IDirtyStateDetector
{
    public bool HasDirtyScenes() => ...;
    public bool HasDirtyAssets() => ...;
    public bool HasAnyDirtyState() => HasDirtyScenes() || HasDirtyAssets();
}

// Custom save behaviour
public sealed class MyExecutor : ISaveExecutor
{
    public SaveResult Execute(AutoSaveSettings settings)
    {
        // your logic
        return new SaveResult(true, scenesSaved: 1, assetsSaved: 0);
    }
}
```

Both interfaces: `DevTools.AutoSave.Core`.
