# Architecture Overview

This document describes the technical architecture of Null & Void.

## Technology Stack

- **Engine**: Godot 4.3 with C#/.NET 8.0
- **Platforms**: Windows, Linux (Steam Deck), macOS
- **CI/CD**: GitHub Actions

## Core Principles

### 1. Entity-Component Pattern

While Godot uses a scene/node hierarchy, we apply ECS principles for game entities:

- **Entities** (`src/Entities/`): Game objects with identity (Player, Enemy, Item)
- **Components**: Reusable behaviors attached to entities (Health, Inventory, AI)
- **Systems** (`src/Systems/`): Logic that operates on entities (Combat, Movement, FOV)

### 2. Event-Driven Architecture

Systems communicate through the central **EventBus** rather than direct references:

```csharp
// Publishing an event
EventBus.Instance.EmitEntityDamaged(entity, damage, remainingHealth);

// Subscribing to an event
EventBus.Instance.EntityDamaged += OnEntityDamaged;
```

Benefits:
- Loose coupling between systems
- Easy to add new features without modifying existing code
- Facilitates parallel development by different AI agents

### 3. Turn-Based Game Loop

The **TurnManager** coordinates the turn-based flow:

1. Player turn starts
2. Player performs action (move, attack, wait)
3. Player turn ends
4. All other actors process their turns (based on speed)
5. Next turn starts
6. Return to step 1

### 4. State Machine

The **GameState** manages high-level game states:

```
MainMenu -> Loading -> Playing <-> Paused
                         |
                         v
                    Inventory
                         |
                         v
                   GameOver/Victory
```

## Directory Structure

```
src/
├── Core/
│   ├── EventBus.cs      # Central event system
│   ├── TurnManager.cs   # Turn-based game loop
│   └── GameState.cs     # Game state machine
├── Entities/
│   ├── Entity.cs        # Base entity class
│   └── Player.cs        # Player character
├── Components/
│   ├── Health.cs        # Health/damage component
│   └── Inventory.cs     # Item storage component
├── Systems/
│   ├── CombatSystem.cs  # Combat resolution
│   ├── MovementSystem.cs # Movement/collision
│   └── FOVSystem.cs     # Field of view
├── World/
│   ├── TileMap.cs       # Tile-based world
│   └── MapGenerator.cs  # Procedural generation
└── UI/
    ├── HUD.cs           # In-game HUD
    └── InventoryUI.cs   # Inventory screen
```

## Parallelization Strategy

The architecture supports parallel development by AI agents:

| Agent | Responsibility | Dependencies |
|-------|---------------|--------------|
| 1 | Core (TurnManager, EventBus) | None |
| 2 | Entity/Component framework | Core |
| 3 | World generation | Core, Entity |
| 4 | Combat system | Core, Entity |
| 5 | AI behaviors | Core, Entity |
| 6 | UI/UX | Core, Entity |
| 7 | Equipment/modules | Core, Entity |

Each system communicates only through the EventBus, enabling independent development.

## Data-Driven Design

Game data is stored in resource files:

- `resources/entities/` - Entity definitions
- `resources/modules/` - Equipment/module stats
- `resources/world/` - World generation rules

This allows balancing and content changes without code modifications.
