# Software Requirements Specification (SRS) - PulseShift

## 1. Introduction

### 1.1 Purpose
This document specifies the software requirements for **PulseShift**, a 2D mobile rhythm game. It defines the scope, functional requirements, technical constraints, and system architecture required to develop and deploy the game successfully on legacy mobile hardware.

### 1.2 Product Scope
PulseShift is a rhythm game designed specifically for legacy iOS hardware (iPhone 6 running iOS 12.5.8). Rather than mimicking the traditional cursor-based gameplay of *osu!*, PulseShift parses standard `.osu` beatmaps and converts their spatial data into a distinct directional-input gameplay system featuring taps, multi-directional swipes, and continuous kinetic slide paths.

### 1.3 Definitions, Acronyms, and Abbreviations
*   **DSP Time:** Digital Signal Processing time; the highly accurate audio clock provided by Unity, essential for rhythm game synchronization.
*   **Object Pooling:** A memory management technique where game objects are pre-instantiated and reused, rather than created and destroyed at runtime.
*   **Ticks:** Discrete points along a slide path that evaluate the player's continuous touch position and timing to determine combo continuation.
*   **SRS:** Software Requirements Specification.

---

## 2. Overall Description

### 2.1 Product Perspective
PulseShift operates as a standalone iOS application built on a Unity Long Term Support (LTS) version compatible with iOS 12. It requires local access to the device's file system (via iTunes File Sharing or the iOS Files app) to allow users to import their own beatmaps and audio files.

### 2.2 Operating Environment Constraints
*   **Target Device:** iPhone 6
*   **OS:** iOS 12.5.8 (Minimum)
*   **Hardware Limits:** Apple A8 CPU, 1 GB LPDDR3 RAM, PowerVR GX6450 GPU.
*   **Graphics API:** OpenGL ES 3.0 (or lightweight Metal if fully stable on the target Unity LTS).

### 2.3 Design & Visual Constraints
To maintain strict performance targets, the game will utilize a flat, minimalistic 2D art style. A high-contrast color palette—such as the Catppuccin Mocha Pink aesthetic—will be used to provide excellent visual clarity, lane distinction, and UI separation without relying on expensive real-time lighting, shaders, or heavy particle effects.

---

## 3. System Features & Functional Requirements

### 3.1 Beatmap Parsing & Conversion System
*   **Description:** The system must read user-provided `.osu` files and translate them into a playable format.
*   **Requirements:**
    *   **FR-1.1:** The parser must accurately deserialize `[General]`, `[Metadata]`, `[Difficulty]`, `[TimingPoints]`, and `[HitObjects]` sections.
    *   **FR-1.2:** The converter must map spatial *osu!* hit circles to directional taps or lane-specific taps.
    *   **FR-1.3:** The converter must map *osu!* sliders into **Slide Objects**. This includes:
        *   Calculating the total duration based on beat length and slider velocity.
        *   Translating the spatial curve points into the game's coordinate/lane system.
        *   Calculating the specific timestamps and expected screen positions for all internal **Slider Ticks**.
    *   **FR-1.4:** The converter must map *osu!* spinners to rapid, multi-directional tap/swipe inputs.

### 3.2 Gameplay & Input System
*   **Description:** The core gameplay loop mapping user interaction to the generated notes.
*   **Requirements:**
    *   **FR-2.1:** The input system must register single taps, continuous touch tracking (drags/slides), and multi-directional swipes.
    *   **FR-2.2:** The game must evaluate discrete inputs against expected note timestamps using strict judgement windows (Perfect, Good, Miss).
    *   **FR-2.3 (Slide Tracking & Ticks):** For Slide Objects, the game must evaluate the player's continuous touch position:
        *   The player must initiate the slide with a tap at the starting coordinate.
        *   As the song progresses, the game must evaluate the active touch $X/Y$ coordinates at every parsed **Tick** timestamp.
        *   Missing a tick (finger lifted or out of bounds) breaks the current combo and grays out the slide visual.
    *   **FR-2.4:** Missed notes or ticks must reset the player's combo multiplier.
    *   **FR-2.5:** The scoring system must calculate accuracy dynamically and issue an end-of-map grade (S, A, B, C, D).

### 3.3 Audio Synchronization
*   **Description:** Playback and synchronization of the audio files.
*   **Requirements:**
    *   **FR-3.1:** Audio playback must be tied to `AudioSettings.dspTime` to prevent desynchronization caused by frame drops.
    *   **FR-3.2:** Note spawning and movement algorithms must calculate positions strictly based on DSP time minus the approach rate/lead-in time.

### 3.4 File Management
*   **Description:** Handling user-generated content.
*   **Requirements:**
    *   **FR-4.1:** The app must expose a `Documents` directory accessible via iOS File Sharing.
    *   **FR-4.2:** The system must scan the directory on launch, indexing available `.osu` beatmaps and associated `.mp3`/`.ogg` files asynchronously to avoid freezing the main thread.

---

## 4. Non-Functional Requirements

### 4.1 Performance & Memory Requirements
*   **NFR-1 (Framerate):** The game must maintain a stable 60 FPS (16.6ms frame time) during gameplay on an iPhone 6.
*   **NFR-2 (Memory Limit):** Total runtime memory allocation must not exceed 200 MB to avoid triggering the iOS out-of-memory (OOM) watchdog.
*   **NFR-3 (Zero-Allocation Loop):** The gameplay loop must generate **0 bytes** of garbage collection (GC) allocation per frame. All notes, hit effects, and audio sources must be fully object-pooled during the map loading screen.
*   **NFR-4 (Rendering Slides):** Slide paths must be pre-calculated during the loading phase. Dynamic bezier evaluation is prohibited during gameplay. Visual ribbons must be rendered using highly optimized, low-vertex techniques (e.g., simplified `LineRenderer` or stretched 2D sprites).

### 4.2 Usability Requirements
*   **NFR-5 (UI Responsiveness):** The user interface must be easily tappable on a 4.7-inch screen and provide immediate visual feedback (e.g., brief hit flashes) upon input.
*   **NFR-6 (Fail State):** The game will utilize a "No Fail" system by default, allowing players to experience the full converted map regardless of performance.

---

## 5. System Architecture

| Module | Responsibility |
| :--- | :--- |
| **BeatmapParser** | Deserializes the `.osu` plaintext into C# structures. |
| **MappingConverter** | Transforms *osu!* coordinates/curves into PulseShift lanes, pre-calculating Slide arrays and Ticks. |
| **Conductor / AudioManager** | Tracks `dspTime`, calculates song position, and acts as the central clock. |
| **NoteSpawner** | Fetches objects from the `NotePool` and places them on screen based on the Conductor's time. |
| **JudgementEngine** | Compares `Input.GetTouch()` for discrete taps. **Continuously polls touch coordinates during active slides to validate position against active Ticks.** |

---

## 6. Development Phasing

*   **Phase 1 - Core Sync & Architecture:** Implement the Audio Conductor, basic BeatmapParser, Object Pooling, and zero-allocation game loop. *Goal: A dot moving perfectly to the beat at 60fps.*
*   **Phase 2 - Basic Gameplay:** Implement static tap-note spawning and tap input judgement. *Goal: Playable tap-only maps.*
*   **Phase 3 - The Slide Mechanic:** Implement the *Project Sekai*-style slide conversion. Build the path pre-calculation logic, the visual rendering (ribbons), and the continuous touch/Tick evaluation in the JudgementEngine.
*   **Phase 4 - Spinners & UI:** Convert spinners to rapid-taps/swipes. Implement the combo and scoring UI.
*   **Phase 5 - File I/O & Aesthetic:** Implement iOS file sharing, menu systems, map selection, and apply the final minimalistic visual polish.
*   **Phase 6 - Final Profiling:** Intensive stress testing on the physical iPhone 6 using Unity Profiler on high-density maps to ensure strict adherence to the 0-byte GC rule.