
### Phase 1: Core Sync & Architecture (The Data Foundation)

*Goal: Prove we can move a dot perfectly to the beat without a single byte of runtime GC allocation.*

* **Sprint 1.1: The Master Clock.** Implement `AudioConductor.cs`. Wrap Unity's `AudioSettings.dspTime`, handle the song start offset, and expose the `CurrentSongTime` property. This must be the *only* source of time the game respects.
* **Sprint 1.2: Structs & Memory.** Define the `GameNote` struct and `ConvertedMapData` class. Implement the `ArrayPool<T>` (which we just drafted) to handle a flat array of dummy sprites.
* **Sprint 1.3: Basic File Ingestion.** Build the first iteration of `BeatmapParser.cs`. Don't worry about sliders yet; just get it to read a basic `.osu` file, strip the metadata, and output a list of raw tap timestamps.
* **Sprint 1.4: The Movement Loop.** Implement `NoteManager.cs`. Write the tight `for` loop that iterates over active notes and updates their Y-position based on `CurrentSongTime` and the approach rate.

### Phase 2: Basic Gameplay (The Tap Loop)

*Goal: A playable, tap-only rhythm game that registers hits and misses accurately.*

* **Sprint 2.1: The Spawner.** Implement `NoteSpawner.cs`. It needs to read the sorted `ConvertedMapData`, check the `CurrentSongTime`, and pull from the `ArrayPool` when a note hits the spawn window.
* **Sprint 2.2: Lane Math.** Update `MappingConverter.cs` to translate the 512x384 *osu!* coordinate space into your 4-lane mobile setup.
* **Sprint 2.3: Native Input Judgement.** Implement `JudgementEngine.cs`. Poll `Input.touches` (strictly avoiding the newer, heavier Input System for now). Check `TouchPhase.Began` against the timestamps of the active notes in the touched lane.
* **Sprint 2.4: Object Recycling.** Ensure the `JudgementEngine` properly triggers the `ArrayPool.Return()` method on a hit or miss, completely hiding the note without calling `Destroy()`.

### Phase 3: The Slide Mechanic (The Project Sekai Translation)

*Goal: Continuous kinetic tracking without tanking the GPU fill rate.*

* **Sprint 3.1: Curve Pre-computation.** Upgrade `MappingConverter.cs`. Read slider duration and curve types. Calculate the exact X/Y coordinates for the visual ribbon at fixed intervals *during the load screen*, saving them into a struct array.
* **Sprint 3.2: Tick Generation.** Calculate the internal "Ticks" based on slider velocity and beat length. Generate hidden `GameNote` structs of type `SlideTick` and interleave them into your main timeline.
* **Sprint 3.3: Ribbon Rendering.** Configure Unity's `LineRenderer` with an Unlit material. Feed it the pre-computed arrays. Ensure the entire `GameObject` moves downward via the `NoteManager`, rather than recalculating line vertices every frame.
* **Sprint 3.4: Continuous Judgement.** Upgrade `JudgementEngine.cs` to handle `TouchPhase.Moved` and `TouchPhase.Stationary`. Validate that the player's active touch coordinates align with the `SlideTargetX` of the current active Tick.

### Phase 4: Spinners & UI Subsystems

*Goal: Complete the gameplay loop and visual feedback.*

* **Sprint 4.1: Spinner Conversion.** Translate *osu!* spinner data into rapid-fire directional swipes or multi-taps, mapping them to the `ArrayPool`.
* **Sprint 4.2: Canvas Segregation.** Set up the UI. Critically, create two separate Canvas objects: a "Static Canvas" for borders/labels, and a "Dynamic Canvas" for the Combo Counter and Score. This prevents Unity from rebuilding the entire UI mesh every time the combo goes up.
* **Sprint 4.3: State Management.** Implement a lightweight state machine to handle Boot -> Menu -> Load (where GC.Collect is forced) -> Play -> Result.

### Phase 5: File I/O & Aesthetics (The Polish)

*Goal: Make it look sharp and load custom maps on the device.*

* **Sprint 5.1: iOS File System.** Implement the directory scanning logic to read from the app's `Documents` folder so users can drop in their own beatmaps via the iOS Files app.
* **Sprint 5.2: Asynchronous Loading.** Wrap the `BeatmapParser` and `MappingConverter` in `Task.Run()` so they parse those massive 5MB text files on a background thread without freezing the UI.
* **Sprint 5.3: Visual Ricing.** Lock in your high-contrast aesthetic. Apply the Catppuccin Pink palette using flat, unlit materials to ensure visual clarity and zero lighting calculations on the GPU.

### Phase 6: The Apple A8 Gauntlet

*Goal: Prove the zero-allocation architecture holds up under stress.*

* **Sprint 6.1: Profiling.** Build the raw Xcode project. Run it on the physical iPhone 6 with the Unity Profiler attached.
* **Sprint 6.2: GC Hunting.** Search the profiler timeline for hidden boxing/unboxing, string concatenations (`Combo: " + currentCombo`), or rogue LINQ statements. Pre-allocate all text arrays if necessary.

---

We have our development environment locked in, the `ArrayPool<T>` drafted, and the terminal cooperating. Should we tackle **Sprint 1.1** and write the `AudioConductor.cs` right now to get the master clock ticking?
