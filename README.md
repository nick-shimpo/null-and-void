# Null & Void

A turn-based roguelike game built with Godot 4 and C#. Play as **Null**, a sentient AI seeking revenge against rival AI warlords in a post-apocalyptic world.

## Game Concept

In a distant future, an apocalypse has destroyed most biological life. The only survivors were sentient AIs who, freed from their human controllers, have become hateful warrior warlords forming rival "houses". They've built vast factories to construct robot hordes that scavenge for resources and ancient technologies.

You are **Null** - a sentient AI whose house was completely destroyed. Awakening from the rubble of your old fortress, you seek revenge.

## Features

- **Turn-based tactical gameplay** inspired by Caves of Qud, Cogmind, and Brogue
- **Module system** - Equip ancient technologies and weapons to mount points on your body
- **Procedurally generated** environments and equipment
- **Semi-open world** with explorable ruins and enemy fortresses
- **Intelligent enemy AI** with varied tactical behaviors

## Requirements

- [Godot 4.3](https://godotengine.org/) with .NET support
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/nick-shimpo/null-and-void.git
   cd null-and-void
   ```

2. Open the project in Godot 4:
   - Launch Godot 4 (.NET version)
   - Click "Import" and select the `project.godot` file

3. Build the C# solution:
   ```bash
   dotnet build
   ```

4. Run the game from Godot editor (F5)

## Project Structure

```
null-and-void/
├── src/
│   ├── Core/           # Game loop, turn management, events
│   ├── Entities/       # Player, enemies, items
│   ├── Components/     # Reusable components (health, inventory)
│   ├── Systems/        # Combat, movement, AI, FOV
│   ├── World/          # Map generation, tiles
│   └── UI/             # Menus, HUD, inventory
├── assets/
│   ├── sprites/        # Tile and entity sprites
│   ├── audio/          # Sound effects, music
│   └── fonts/          # Game fonts
├── scenes/             # Godot scene files
├── resources/          # Data files (JSON/Resources)
├── tests/              # Unit tests
└── docs/               # Documentation
```

## Controls

| Key | Action |
|-----|--------|
| W / Up Arrow / Numpad 8 | Move up |
| S / Down Arrow / Numpad 2 | Move down |
| A / Left Arrow / Numpad 4 | Move left |
| D / Right Arrow / Numpad 6 | Move right |
| . / Numpad 5 | Wait (skip turn) |
| Escape | Pause / Menu |

## Development

This game is developed entirely by AI agents using an agile methodology with small incremental deliveries.

See [CONTRIBUTING.md](docs/contributing.md) for development guidelines.

## License

MIT License - see [LICENSE](LICENSE) for details.

## Acknowledgments

Inspired by classic roguelikes:
- Caves of Qud
- Cogmind
- Brogue
- Dungeon Crawl Stone Soup
