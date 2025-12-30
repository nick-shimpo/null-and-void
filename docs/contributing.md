# Contributing Guidelines

This project is developed by AI agents. These guidelines ensure consistent, high-quality contributions.

## Development Workflow

### 1. Understand the Task

Before starting work:
- Read the relevant milestone documentation in `docs/milestones/`
- Understand dependencies on other systems
- Review existing code patterns in similar areas

### 2. Follow Existing Patterns

- Use the **EventBus** for cross-system communication
- Extend **Entity** for new game objects
- Follow the naming conventions in existing code
- Add XML documentation comments to public APIs

### 3. Code Organization

```
Feature implementation should:
├── Add scripts to appropriate src/ subdirectory
├── Create scenes in scenes/ directory
├── Add resources to resources/ directory
└── Update documentation if needed
```

### 4. Testing

- Run `dotnet build` to verify compilation
- Test in Godot editor before committing
- Verify no regressions in existing functionality

## Coding Standards

### C# Conventions

```csharp
// Use PascalCase for public members
public int MaxHealth { get; set; }

// Use _camelCase for private fields
private bool _isActive;

// Use XML documentation for public APIs
/// <summary>
/// Applies damage to the entity.
/// </summary>
/// <param name="damage">Amount of damage to apply</param>
public void TakeDamage(int damage) { }
```

### Godot Conventions

- Scene files: `PascalCase.tscn`
- Resource files: `snake_case.tres`
- Script files: `PascalCase.cs` (matching class name)

### Event Naming

Events follow the pattern: `{Subject}{Action}EventHandler`

```csharp
// Examples
EntityDamagedEventHandler
TurnStartedEventHandler
PlayerTurnEndedEventHandler
```

## Commit Messages

Use conventional commit format:

```
type(scope): description

feat(combat): add melee attack system
fix(player): correct movement on diagonal tiles
refactor(core): simplify turn manager logic
docs(readme): update installation instructions
```

Types: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`

## Pull Request Process

1. Create feature branch from `main`
2. Implement feature with tests
3. Update documentation if needed
4. Create PR with description of changes
5. Ensure CI passes
6. Merge after review

## Architecture Guidelines

### Adding New Entities

1. Create class extending `Entity` in `src/Entities/`
2. Create scene in `scenes/entities/`
3. Emit appropriate events in lifecycle methods
4. Register with TurnManager if turn-based actor

### Adding New Systems

1. Create system class in `src/Systems/`
2. Subscribe to relevant EventBus signals
3. Emit events for actions that affect other systems
4. Document public API

### Adding New UI

1. Create UI scene in `scenes/ui/`
2. Create script in `src/UI/`
3. Connect to GameState for visibility
4. Subscribe to events for data updates

## Milestone Development

Each milestone follows this process:

1. **Planning**: Define detailed requirements
2. **Implementation**: Build features incrementally
3. **Integration**: Connect with existing systems
4. **Testing**: Verify functionality
5. **Documentation**: Update docs and comments
6. **Review**: Quality checkpoint before next milestone
