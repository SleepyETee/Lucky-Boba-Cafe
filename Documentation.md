# Lucky Boba Cafe — Game Project 2 Documentation

**Author:** Long  
**Course:** Creating Functional Games with Unity  
**Date:** March 2026  

---

## Table of Contents

1. [Introduction](#introduction)
2. [Script Summaries](#script-summaries)
3. [Scene Descriptions](#scene-descriptions)
4. [Key GameObjects](#key-gameobjects)
5. [Project Requirements](#project-requirements)
6. [Known Issues & Incomplete Features](#known-issues--incomplete-features)
7. [Referenced Material](#referenced-material)

---

## Introduction

**Lucky Boba Cafe** is a 2D cafe management game where the player runs a boba tea shop as a cat barista. Each in-game day, customers arrive and place drink orders. The player walks to the crafting station, completes a 5-step crafting minigame (Brew → Mix → Shake → Top → Serve), and earns tips based on drink quality and speed. The game spans multiple days with escalating goals, a reputation system, an upgrade shop, and a delivery minigame for bonus earnings.

### Controls

| Input | Action |
|-------|--------|
| WASD / Arrow Keys | Move player |
| Left Shift | Sprint |
| E | Interact with stations, NPCs, buildings |
| Space | Crafting actions (hold/tap/time depending on step) |
| 1 / 2 / 3 | Choose topping during crafting Top step |
| Escape | Pause / Unpause |
| T | Debug: end current day early |

### Goals

- Serve customers quickly and accurately to earn PawCoins (tips).
- Meet the daily earnings goal before the day ends; failing means retrying the day.
- Manage angry customers — too many (3) ends the day in failure.
- Build reputation (1–5 stars) by providing high-quality service.
- Purchase upgrades between days to improve performance.
- Complete all 5 days to win the game.

---

## Script Summaries

### 1. GameManager.cs (`Assets/Scripts/Core/`)
Core singleton that persists across all scenes. Manages PawCoins (currency), pause state, customer tracking, daily stats (tips, satisfaction, angry customers), an upgrade system backed by ScriptableObjects, and provides save/load data snapshots. All other systems read from and write to GameManager.

### 2. SaveManager.cs (`Assets/Scripts/Core/`)
Handles saving and loading game progress. Uses `JsonUtility` to serialize `GameData` to a JSON file in `Application.persistentDataPath`, with PlayerPrefs as a lightweight fallback. Supports New Game vs. Continue flow by queuing a pending start mode that is resolved when the gameplay scene loads. Auto-saves on application quit.

### 3. AudioManager.cs (`Assets/Scripts/Core/`)
Singleton that manages background music and sound effects. Creates separate AudioSources for music and SFX at runtime. Provides helper methods for every game event (brew, coin, customer arrive/angry, button click, etc.) and per-period music (morning, lunch rush, afternoon, evening). Volume levels (master, music, SFX) are persisted via PlayerPrefs.

### 4. DaySummaryUI.cs (`Assets/Scripts/UI/`)
Drives the day timer and win/lose evaluation in the gameplay scene. Converts elapsed real-time into game hours (6 AM–7 PM), updates the HUD each frame, changes music when the time period shifts (Morning → Lunch Rush → Afternoon → Closing), detects win/lose conditions (goal met, too many angry customers, time expired), triggers auto-save, and displays an end-of-day summary panel with rating (S through D rank) and navigation options (next day, retry, main menu).

### 5. CraftingMinigame.cs (`Assets/Scripts/Crafting/`)
Implements the 5-step drink crafting minigame: **Brew** (hold Space and release at the target time), **Mix** (tap Space rapidly within a timer), **Shake** (press Space when an oscillating bar is centered), **Top** (choose a topping via buttons or keys 1–3), and **Serve** (auto-scores and delivers the drink to the waiting customer). Each step scores independently; the average determines final drink quality, which affects tips and reputation.

### 6. Customer.cs (`Assets/Scripts/NPCs/Customer/`)
Customer AI using a state machine (Entering → WaitingToOrder → WaitingForDrink → Receiving → Leaving). Features a patience timer with a color-gradient bar (green → yellow → red), order bubble text, type variants (Regular, Rusher, Foodie, VIP) with different patience/tip multipliers, and visual reactions. Integrates with GameManager for tip calculation and ReputationSystem for service quality tracking.

### 7. PlayerController.cs (`Assets/Scripts/Player/`)
Handles WASD movement with sprint (Shift), interaction via the E key, and animation parameter updates. Maintains a list of nearby `IInteractable` objects detected through trigger colliders and always targets the closest one, showing an HUD prompt. Freezes movement during the crafting minigame.

### 8. SettingsManager.cs (`Assets/Scripts/UI/`)
Manages the settings panel UI with three volume sliders (master, music, SFX), a resolution dropdown, and a fullscreen toggle. Slider changes are applied in real-time and persisted to PlayerPrefs. On load, it reads saved values and applies them immediately so audio is correct from startup.

### 9. DeliveryGameManager.cs (`Assets/Scripts/Delivery/`)
Controller for the delivery racing minigame — a separate scene where the player drives a scooter to deliver boba orders under a time limit. Features a start screen, countdown timer with bonus time per delivery, tip calculation based on remaining time, crash penalties, a results screen, and pause support. Earnings are saved back to the main game via SaveManager.

### 10. ReputationSystem.cs (`Assets/Scripts/Systems/`)
Tracks cafe reputation as points mapped to a 1–5 star rating. Perfect service grants large point gains; poor service or angry departures cause losses. Star thresholds gate content unlocks (new recipes, delivery access, etc.). Fires events consumed by the HUD to display star icons. Reputation is included in save data.

---

## Scene Descriptions

### Scene 0: Main Menu (`Main.unity`)

The entry point of the game. Contains:
- **MainMenuPanel** — Title, Play/Continue button, New Game button, Settings button, Credits button, Quit button.
- **SettingsPanel** — Volume sliders (master, music, SFX), resolution dropdown, fullscreen toggle, Apply and Back buttons.
- **CreditsPanel** — Attribution text and Back button.
- **Persistent managers** are instantiated here: GameManager, SaveManager, AudioManager (all marked `DontDestroyOnLoad`).

**Key interactions:** If a save file exists, the Play button becomes "Continue" and a "New Game" button appears below it. Continue loads saved data; New Game deletes the save and starts fresh. Settings changes apply immediately and persist. Quit exits the application.

### Scene 1: Cafe / Gameplay (`GameScene.unity`)

The core gameplay scene. The player (cat barista) moves around a 2D cafe interior. Customers spawn at a door, walk to the counter queue, place orders, and wait with decreasing patience. The player approaches the CraftingStation, presses E to start the 5-step minigame, then the drink is served to the front customer.

**Key interactions:**
- DaySummaryUI drives the day timer (6 AM → 7 PM over ~180 real seconds).
- DayNightCycle provides visual lighting transitions (dawn gold → bright day → sunset orange → twilight blue).
- CustomerSpawner spawns customers at rates that increase during Lunch Rush and scale with day number.
- HUDController displays money, day/timer, goal progress, angry-customer strikes, and reputation stars.
- PauseMenuController handles Escape → pause overlay with Resume, Settings, Main Menu, and Quit buttons.
- At day's end, a summary panel shows stats, rating, and options (next day via shop, retry, or main menu).

### Scene 2: Shop (`ShopScene.unity`)

A between-day upgrade shop. The player spends earned PawCoins on upgrades defined by `UpgradeData` ScriptableObjects (e.g., "Tip Jar" for +15% tips, "Cat Charm" for +5s customer patience, "Quick Paws" for wider crafting tolerances, "Extra Counter" for +1 max customers). Each upgrade has multiple levels with increasing costs.

**Key interactions:** ShopManager dynamically builds upgrade cards from the UpgradeData array. Buying an upgrade deducts PawCoins, increments the upgrade level, and auto-saves. "Start Next Day" resets daily stats and loads GameScene.

### Scene 3: Village (`VillageScene.unity`)

An explorable village area accessible after successful days. Contains buildings with trigger-collider entrances (Shop, neighbor houses, goal board). The player walks around and interacts with VillageEntrance objects using E. Neighbor visits build friendships; the goal board tracks overall progress.

**Key interactions:** VillageManager handles returning to the cafe. NeighborSystem manages NPC friendships and dialogue. ShopSystem (village variant) lets the player buy ingredients.

### Scene 4: Delivery (`DeliveryScene.unity`)

A top-down driving minigame. The player controls a scooter (WASD + Space for boost) and races to deliver boba orders to marked delivery points before time runs out. Crashing into obstacles or traffic cars costs time. Completing deliveries earns tips and bonus time.

**Key interactions:** DeliveryScooter handles physics-based driving with boost, drift, and crash/invulnerability. DeliveryGameManager manages the timer, delivery targets, results screen, and pause. Earnings are added to SaveManager and carry over to the cafe.

---

## Key GameObjects

### 1. GameManager (persistent)
- **Components:** GameManager.cs
- **Purpose:** Central singleton managing all game state — currency, day progression, pause, customer stats, upgrades. Persists across all scenes via `DontDestroyOnLoad`. Every other system reads from or writes to this object.

### 2. Player (GameScene)
- **Components:** SpriteRenderer, Rigidbody2D, BoxCollider2D (trigger for interaction range), PlayerController.cs, Animator (optional), SimpleAnimator.cs (fallback)
- **Purpose:** The player-controlled cat barista. Moves via WASD, sprints with Shift, detects nearby IInteractable objects through trigger overlap, and interacts with E. Frozen during crafting minigame. Animation parameters (MoveX, MoveY, IsMoving) drive walk/idle visuals.

### 3. Customer (prefab, spawned at runtime)
- **Components:** SpriteRenderer, Customer.cs, Animator (optional), auto-created child objects (OrderBubble with TextMeshPro, PatienceBar with SpriteRenderers, AngryIndicator, ReactionText)
- **Purpose:** AI-driven cafe patrons. State machine cycles through Entering → WaitingToOrder → WaitingForDrink → Leaving. Shows order text, a patience bar that shrinks and changes color, and post-service reactions. Customer types (Regular, Rusher, Foodie, VIP) have different patience, speed, and tip multipliers. Interacts with GameManager (tips, stats) and ReputationSystem.

### 4. CraftingStation (GameScene)
- **Components:** SpriteRenderer, BoxCollider2D (trigger), CraftingStation.cs (implements IInteractable)
- **Purpose:** The interactable boba-making counter. When the player presses E in range and a customer is waiting, it freezes the player and starts the CraftingMinigame. Shows a "Press [E]" prompt when the player is nearby, or "No orders!" if no customer is waiting.

### 5. DaySummaryUI (GameScene)
- **Components:** DaySummaryUI.cs, references to HUD text elements and summary panel
- **Purpose:** The day's clock and win/lose arbiter. Tracks elapsed time, maps it to game hours, updates the HUD (timer, goal, strikes, period text), triggers period-based music changes and DayNightCycle lighting, evaluates win/lose at day's end, displays a summary panel with grade rating (S–D), and handles navigation to shop/retry/menu. Also applies difficulty scaling (spawn rate, patience, move speed) based on the current day number.

### 6. DeliveryScooter (DeliveryScene)
- **Components:** SpriteRenderer, Rigidbody2D, DeliveryScooter.cs, ParticleSystem (boost/smoke), TrailRenderer (drift trails), AudioSource (engine)
- **Purpose:** Player vehicle in the delivery minigame. Physics-based top-down driving with acceleration, braking, drift, and a boost mechanic (Space, with cooldown). Detects collisions with obstacles (crash → slowdown + time penalty + invulnerability flash) and delivery points (trigger → complete delivery). Engine sound pitch scales with speed.

### 7. AudioManager (persistent)
- **Components:** AudioManager.cs, two runtime-created AudioSources (music loop, SFX one-shot)
- **Purpose:** Singleton handling all game audio. Plays background music per period (morning, lunch rush, afternoon, evening, menu) and sound effects for every game event (brew, coin, customer arrive/angry, button click, success/fail). Volume levels (master, music, SFX) are independently adjustable and persisted to PlayerPrefs.

---

## Project Requirements

| Requirement | How It Is Satisfied |
|---|---|
| **Player Input** | WASD movement, Shift sprint, E interaction, Space crafting actions, 1/2/3 topping selection, Escape pause — all handled in `PlayerController.cs`, `CraftingMinigame.cs`, `PauseMenuController.cs`, and `DeliveryScooter.cs`. |
| **Saving and Loading** | `SaveManager.cs` serializes `GameData` (day, coins, reputation, upgrades, volumes) to JSON via `JsonUtility` + `File.WriteAllText`. Saves trigger at day end, upgrade purchase, pause→menu, and application quit. Continue button on main menu loads the save file and applies it via `GameManager.ApplySaveData()`. |
| **Sound** | `AudioManager.cs` singleton plays looping background music per time period and one-shot SFX for crafting steps (brew start, perfect/good/bad), coins, customer arrive/angry, button clicks, success/fail jingles, and delivery events. Music adds atmosphere; SFX provide immediate gameplay feedback. |
| **Settings (including volume)** | `SettingsManager.cs` provides UI sliders for Master, Music, and SFX volume, plus resolution dropdown and fullscreen toggle. Changes apply in real-time and are persisted to PlayerPrefs. Settings panel is accessible from both the Main Menu and the in-game Pause Menu. |
| **Animation Controller / Animation Clip** | `AnimationSetupTool.cs` (Editor script) generates `PlayerAnimator.controller` and `CustomerAnimator.controller` with Idle, Walk, Interact, Happy, and Angry animation clips (scale bobbing, squash-stretch, bounce, shake). `SimpleAnimator.cs` provides a runtime code-driven fallback. `PlayerController.cs` and `Customer.cs` both drive animator parameters each frame. |
| **Main Menu, Gameplay Scene(s), and Pausing** | **Main Menu:** `Main.unity` with play/continue/new game/settings/credits/quit via `MainMenuController.cs`. **Gameplay:** `GameScene.unity` (cafe), `ShopScene.unity` (upgrades), `VillageScene.unity` (exploration), `DeliveryScene.unity` (driving minigame). **Pausing:** `PauseMenuController.cs` toggles on Escape, freezes `Time.timeScale`, shows pause overlay with resume/settings/menu/quit. |
| **Ability to close application at any time** | Quit button on Main Menu (`MainMenuController.OnQuitClicked`), Quit button in Pause Menu (`PauseMenuController.QuitGame`), and Return to Main Menu button in Pause Menu (auto-saves first, then loads Main scene where Quit is available). `Application.Quit()` is called in builds; `EditorApplication.isPlaying = false` in editor. |
| **Emergent gameplay / progression** | The game spans 5 days with escalating daily goals, faster customer spawns, shorter patience, and varied customer types (Regular/Rusher/Foodie/VIP with weighted probabilities shifting each day). Between days, the player purchases upgrades (Tip Jar, Cat Charm, Quick Paws, Extra Counter) that change gameplay dynamics. The delivery minigame offers an alternate income source. Reputation (1–5 stars) gates content. |
| **Win and/or Lose conditions** | **Win:** Meet the daily earnings goal before time expires. Completing all 5 days triggers a final "YOU WIN" screen with S Rank. **Lose:** Fail to meet the earnings goal by day's end, OR accumulate 3+ angry customers (patience expired). Failed days can be retried. Different play styles and upgrade paths lead to different outcomes and ratings (S/A/B/C/D). |

---

## Known Issues & Incomplete Features

- **Audio clips not fully assigned:** The AudioManager has serialized fields for many music tracks and SFX clips (morning, lunch rush, afternoon, evening, customer happy, button click, etc.), but only "Open Morning Cafe.mp3", "Coin.mp3", and "Tea.mp3" exist in the Audio folder. Missing clips will simply not play (null-checked), but assigning more audio would improve the experience.
- **Animations require Editor setup:** The `AnimationSetupTool.cs` generates animation controllers and clips via the Unity menu (Tools > Lucky Boba > Generate Animations), but the resulting controllers must then be assigned to the Player and Customer prefab Animator components in the Inspector. `SimpleAnimator.cs` provides a code-based fallback if this step is skipped.
- **Village scene content:** The VillageScene has basic layout but limited interactable content. Neighbor friendships and the goal board are functional in code but may lack full UI hookup in the scene.
- **Delivery scene setup:** The DeliveryScene script logic is complete, but the scene may need delivery points, obstacles, and traffic cars placed and tagged properly in the editor to be fully playable.
- **Resolution dropdown:** On some platforms, the resolution list from `Screen.resolutions` may contain duplicates or unexpected entries.

---

## Referenced Material

### Assets

| Asset | Source | Usage |
|---|---|---|
| Open Morning Cafe.mp3 | Free music resource (royalty-free) | Background music — used as-is |
| Coin.mp3 | Free SFX resource (royalty-free) | Coin/tip sound effect — used as-is |
| Tea.mp3 | Free SFX resource (royalty-free) | Brewing/pour sound effect — used as-is |
| TextMesh Pro | Unity Technologies (built-in package) | All UI text rendering — used as-is |
| Cat sprites (Black cat.png, Cat Format.png, cat png backside.png) | Custom / student-created | Player and customer visuals |

### Packages

| Package | Usage |
|---|---|
| TextMesh Pro (TMP) | UI text throughout all scenes — used as-is via Unity Package Manager |
| Unity Input System | Input action maps (InputSystem_Actions) — generated, slightly modified |

### Code & Guides

| Source | Contribution | Degree of Use |
|---|---|---|
| Claude (AI assistant) | Co-authored scripts with student; provided architecture suggestions, debugging, and code generation for manager singletons, crafting minigame, customer AI, save system, delivery minigame, day/night cycle, and UI controllers. | Significant — AI-generated code was reviewed, modified, and integrated by the student. Each script header identifies "Long + Claude" as co-authors. |
| Unity Documentation (docs.unity3d.com) | Reference for `JsonUtility`, `SceneManager`, `AudioSource.PlayOneShot`, `Animator` API, `PlayerPrefs`, `Screen.resolutions`, coroutines. | Light usage — consulted for API correctness. |
| Unity Learn Tutorials | General guidance on singleton patterns, 2D movement, UI Canvas setup. | Light usage — concepts applied but code written independently. |

---

*End of Documentation*
