# Development Skills and Practices

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
