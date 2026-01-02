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
