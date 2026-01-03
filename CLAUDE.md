# Project Guidelines for Claude

## Display and Resolution Requirements

**Target Resolution: 2560x1440 (QHD/1440p)**

The game is optimized for 2560x1440 display:
- ASCII buffer: 180 columns x 50 rows
- Font size: 24px (auto-calculated to fill viewport)
- Character dimensions: ~14px wide x 28px tall

### Project Settings (project.godot)
```
[display]
window/size/viewport_width=2560
window/size/viewport_height=1440
window/size/mode=2  # Maximized
window/stretch/mode="canvas_items"
window/stretch/aspect="keep"
```

### Layout Constants (ASCIIBuffer.cs)
- Screen: 180x50 characters
- Map area: 149x42 characters (rows 4-45, columns 0-148)
- Sidebar: 30 characters wide (columns 150-179)
- Message log: 3 rows at top
- Weapon bar: 2 rows at bottom
- Status bar: 1 row at bottom

**Important**: Do not change these resolution settings without updating the entire rendering pipeline (ASCIIBuffer, ASCIIRenderer, MapViewport, UI screens).

---

## Test-Driven Development (TDD)

All ongoing development work must follow a Test-Driven Development approach:

1. **Write tests first** - Before implementing any new feature or fixing a bug, write failing tests that define the expected behavior
2. **Red-Green-Refactor cycle**:
   - **Red**: Write a failing test
   - **Green**: Write minimal code to make the test pass
   - **Refactor**: Improve the code while keeping tests green
3. **Test coverage requirement**: Maintain a consistent **70%+ test coverage** after every build

### Bug Fix Testing Policy

**Every bug fix must include a unit test.** When a bug is reported:

1. **Write a test that reproduces the bug** - Create a failing test that demonstrates the buggy behavior
2. **Fix the bug** - Implement the minimal fix to make the test pass
3. **Verify the test passes** - Ensure the fix resolves the issue
4. **Prevent regression** - The test will catch if the bug is reintroduced in the future

This ensures bugs don't recur and documents the expected behavior for future developers.

## Coverage Requirements

- The CI/CD pipeline enforces a minimum 70% code coverage threshold
- Coverage is measured using XPlat Code Coverage with Cobertura reports
- Godot-dependent code (UI, Rendering, Entities, etc.) is excluded from coverage metrics via `coverage.runsettings`

### What is covered:
- Combat calculations (AccuracyCalculator, DamageCalculator, WeaponStats)
- Targeting logic (LineOfFire, AoECalculator)
- AI pathfinding algorithms
- Core game mechanics (ActionCosts, EntityGrid)
- Item systems

### What is excluded (Godot runtime dependencies):
- UI components
- Rendering systems
- Entity classes that inherit from Godot nodes
- Systems requiring TileMapManager or other Godot singletons

## Pre-Commit and Pre-Push Hooks

The project uses git hooks to enforce quality:

- **Pre-commit**: Runs formatting check, build, and tests
- **Pre-push**: Runs tests with coverage and validates 70% threshold

## Running Tests Locally

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --settings coverage.runsettings --collect:"XPlat Code Coverage"

# Check formatting
dotnet format NullAndVoid.sln --verify-no-changes --exclude NullAndVoid.Tests/
```

## Adding New Tests

1. Create test files in `NullAndVoid.Tests/` mirroring the source structure
2. Use xUnit for test framework
3. Use FluentAssertions for readable assertions
4. Use `TestRandom` helper for deterministic random number testing
5. Follow the naming convention: `MethodName_Scenario_ExpectedResult`

---

## Item Configuration Schema

**IMPORTANT**: All game items are defined in `data/items.json`. This is the master definition file for game balancing.

### When to Update items.json

Always consider updating the item configuration when:
- Adding new item types or categories
- Modifying item stats (damage, armor, energy costs)
- Adding new item properties or abilities
- Balancing existing items
- Adding new rarity tiers or drop rates

### Schema Structure

```
data/items.json
├── moduleArmorByRarity - Armor values per rarity (Common=8 to Legendary=20)
├── rarityColors - Display colors for each rarity
├── starterItems - Predefined starter equipment
├── moduleTemplates - Templates for procedural generation
│   ├── powerSources - Energy generators
│   ├── batteries - Energy storage
│   ├── propulsion - Movement modules
│   └── stealth - Noise reduction
├── slotTypeModules - Per-slot module definitions
├── rarityPrefixes - Name prefixes per rarity
├── rarityDropRates - Loot drop percentages
├── consumables - Single-use items
└── resistanceDefaults - Default damage resistances
```

### Code Integration

- `ItemDefinitions.cs` - Loads and provides access to configuration
- `ItemFactory.cs` - Uses configuration for item creation
- Call `ItemDefinitions.Load()` during game initialization

### Hot Reloading

Call `ItemDefinitions.Reload()` to reload configuration during development without restarting the game.
