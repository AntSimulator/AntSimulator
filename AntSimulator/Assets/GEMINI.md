# GEMINI.md - Ant Simulator Project Overview

## Project Summary
**Ant Simulator** is a stock market simulation game built in Unity. Players navigate a daily game loop consisting of market analysis, trading, and financial settlement. The game features a realistic market engine with price volatility, a banking system, and a "jail" mechanic for failing to meet financial obligations.

## Core Technologies
- **Engine:** Unity (Targeting 30 FPS)
- **Language:** C#
- **UI System:** Unity UI (UGUI) + TextMeshPro
- **Input:** Unity Input System
- **Rendering:** Universal Render Pipeline (URP) 2D
- **Libraries:** xCharts (for financial charting)
- **Data Handling:** ScriptableObjects for static data, JsonUtility for serialization.

## Key Architecture & Patterns

### 1. Game Loop (State Machine)
The game is managed by a `GameStateController` using a State pattern.
- **States:** `PreMarketState` (Preparation), `MarketOpenState` (Trading), `SettlementState` (Expenses/Results), `JailState` (Penalty).
- **Interface:** `IGameState` with `Enter()`, `Tick()`, and `Exit()` methods.
- **Transitions:** Managed centrally by the controller; states do not trigger their own transitions.

### 2. Module Structure (`Assets/Scripts/`)
| Directory | Description |
|-----------|-------------|
| `Banking/` | Clean Architecture based banking/transfer system. |
| `Player/` | Trading engine, portfolio management, and player state. |
| `Stocks/` | Stock definitions, price history, and market logic. |
| `kne/` | Core game systems: Game states, Save/Load, and Calendar. |
| `Delivery/` | Mechanics for delivery-related mini-games or jobs. |
| `Raddit/` | Community/Social media feed simulation. |
| `Utils/` | Shared utilities and JSON loaders. |

### 3. Data Management
- **Static Data:** Defined in `ScriptableObjects` (e.g., `StockDefinition`, `EventDatabaseSO`).
- **Runtime Data:** Stored in pure C# classes (e.g., `PlayerState`, `StockState`).
- **Save System:** `SaveManager` serializes state to JSON in `Application.persistentDataPath`. **Important:** Save data uses IDs to reference ScriptableObjects, never the objects themselves.

### 4. Communication Pattern
- **Observer Pattern:** Systems communicate via C# Events or ScriptableObject-based channels to minimize direct coupling.
- **Event Example:** `StockSelectionEvents.RaiseSelected()` triggers UI updates and controller logic when a stock is clicked.

## Development & Build Guidelines

### Building and Running
- **Editor:** Open the root `AntSimulator` folder in Unity Editor.
- **Main Scene:** `TitleScene.unity` or `MainScene.unity`.
- **Test Scenes:** Developers use individual scenes (e.g., `dkTestScene`, `kneTestScene`) for isolated feature testing.
- **Build Target:** Windows/PC (implied by `win32` environment).

### Coding Conventions
- **Naming:** `PascalCase` for classes/files, `camelCase` for methods/fields, `UPPER_SNAKE` for constants.
- **Namespaces:** Follow folder structure (e.g., `Banking.Core`). Use `Banking.Debugging` to avoid conflicts with `UnityEngine.Debug`.
- **Logging:** Use tagged logs: `Debug.Log($"[State] {message}")`. Common tags: `[State]`, `[Market]`, `[Transfer]`, `[Save]`.
- **Language:** Code is in English, but comments, UI strings, and debug logs are often in Korean.

### Git Workflow
- **Branches:** Work on feature branches. Never commit directly to `main`.
- **Assets Root:** Avoid modifying shared files in the `Assets/` root without coordination.
- **Assembly:** All scripts are in the default `Assembly-CSharp` (no `.asmdef` files used).

## Directory Roadmap
- `Assets/Prefab/`: UI components and gameplay objects.
- `Assets/Scenes/`: Game levels and developer test scenes.
- `Assets/ScriptableObjects/`: Data definitions and game rules.
- `Assets/Scripts/`: Primary source code location.
