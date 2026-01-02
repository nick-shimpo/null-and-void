using Godot;

namespace NullAndVoid.Rendering;

/// <summary>
/// Color palette for the ASCII rendering system.
/// Inspired by Brogue's terminal aesthetic with green/cyan primary colors.
/// </summary>
public static class ASCIIColors
{
    // Primary UI Colors (Terminal Green)
    public static readonly Color Primary = Color.Color8(34, 230, 102);           // Bright green
    public static readonly Color PrimaryDim = Color.Color8(20, 140, 60);         // Dim green
    public static readonly Color PrimaryBright = Color.Color8(102, 255, 153);    // Glow green
    public static readonly Color PrimaryDark = Color.Color8(10, 80, 30);         // Dark green

    // Background Colors
    public static readonly Color BgDark = Color.Color8(5, 10, 5);                // Near black (slight green tint)
    public static readonly Color BgPanel = Color.Color8(10, 20, 10);             // Dark panel background
    public static readonly Color BgHighlight = Color.Color8(20, 40, 20);         // Highlighted background

    // Text Colors
    public static readonly Color TextPrimary = Primary;
    public static readonly Color TextSecondary = Color.Color8(150, 180, 150);    // Muted green-gray
    public static readonly Color TextMuted = Color.Color8(80, 100, 80);          // Very muted
    public static readonly Color TextDisabled = Color.Color8(40, 50, 40);        // Disabled state
    public static readonly Color TextWhite = Color.Color8(220, 230, 220);        // Near white (green tint)

    // Creature Colors
    public static readonly Color Player = Color.Color8(0, 200, 255);             // Cyan
    public static readonly Color PlayerBright = Color.Color8(100, 230, 255);     // Bright cyan
    public static readonly Color Enemy = Color.Color8(255, 80, 80);              // Red
    public static readonly Color EnemyDim = Color.Color8(180, 50, 50);           // Dim red
    public static readonly Color Neutral = Color.Color8(200, 200, 80);           // Yellow

    // Terrain Colors - Basic
    public static readonly Color Floor = Color.Color8(60, 65, 70);               // Gray (slight blue)
    public static readonly Color FloorLit = Color.Color8(80, 85, 90);            // Lit floor
    public static readonly Color Wall = Color.Color8(100, 90, 80);               // Brown-gray
    public static readonly Color WallLit = Color.Color8(130, 115, 100);          // Lit wall
    public static readonly Color Door = Color.Color8(140, 100, 60);              // Brown
    public static readonly Color DoorOpen = Color.Color8(100, 80, 50);           // Darker brown
    public static readonly Color Stairs = Color.Color8(200, 200, 220);           // White-blue
    public static readonly Color Water = Color.Color8(30, 80, 150);              // Blue
    public static readonly Color WaterFg = Color.Color8(60, 120, 200);           // Light blue for fg
    public static readonly Color DeepWater = Color.Color8(20, 50, 120);          // Dark blue
    public static readonly Color Trap = Color.Color8(200, 50, 50);               // Red

    // Terrain Colors - Vegetation
    public static readonly Color Grass = Color.Color8(60, 120, 50);              // Base grass
    public static readonly Color GrassDry = Color.Color8(140, 130, 60);          // Dried/autumn
    public static readonly Color GrassLush = Color.Color8(40, 150, 60);          // Healthy
    public static readonly Color TreeCanopy = Color.Color8(30, 100, 40);         // Tree tops
    public static readonly Color TreeTrunk = Color.Color8(100, 70, 40);          // Bark brown
    public static readonly Color TreePine = Color.Color8(20, 80, 50);            // Evergreen
    public static readonly Color TreeDead = Color.Color8(80, 70, 60);            // Dead wood
    public static readonly Color Scrub = Color.Color8(74, 110, 60);              // Muted green

    // Terrain Colors - Earth & Stone
    public static readonly Color Dirt = Color.Color8(90, 70, 50);                // Bare earth
    public static readonly Color Sand = Color.Color8(180, 160, 120);             // Desert/beach
    public static readonly Color Rock = Color.Color8(100, 100, 110);             // Gray stone
    public static readonly Color RockDark = Color.Color8(60, 60, 70);            // Dark stone
    public static readonly Color RockRed = Color.Color8(140, 80, 60);            // Red rock
    public static readonly Color MountainPeak = Color.Color8(200, 200, 210);     // Snow-capped
    public static readonly Color MountainSlope = Color.Color8(140, 140, 150);    // Mid elevation
    public static readonly Color HillBrown = Color.Color8(140, 120, 96);         // Hill color

    // Terrain Colors - Water Variations
    public static readonly Color WaterShallow = Color.Color8(60, 130, 180);      // Clear shallow
    public static readonly Color WaterDeepOcean = Color.Color8(24, 56, 96);      // Deep ocean
    public static readonly Color WaterMurky = Color.Color8(50, 80, 70);          // Swamp water
    public static readonly Color WaterFoam = Color.Color8(180, 200, 220);        // Foam/rapids
    public static readonly Color MarshWater = Color.Color8(64, 96, 96);          // Marsh tint
    public static readonly Color Reeds = Color.Color8(112, 128, 64);             // Yellow-green

    // Terrain Colors - Paths
    public static readonly Color Road = Color.Color8(144, 128, 112);             // Tan/brown
    public static readonly Color Trail = Color.Color8(160, 144, 112);            // Light path
    public static readonly Color Bridge = Color.Color8(110, 80, 48);             // Wood brown

    // Terrain Colors - Ruins & Decay
    public static readonly Color RuinStone = Color.Color8(80, 80, 85);           // Old stone
    public static readonly Color RuinMetal = Color.Color8(100, 90, 80);          // Rusted metal
    public static readonly Color RuinOvergrown = Color.Color8(50, 90, 60);       // Moss-covered
    public static readonly Color Rubble = Color.Color8(96, 88, 80);              // Debris

    // Terrain Colors - Tech Structures
    public static readonly Color TechWall = Color.Color8(128, 136, 144);         // Metal gray
    public static readonly Color TechWallDark = Color.Color8(96, 96, 104);       // Darker metal
    public static readonly Color TechFloor = Color.Color8(32, 32, 40);           // Dark floor
    public static readonly Color TechTerminal = Color.Color8(64, 176, 192);      // Cyan glow
    public static readonly Color TechGenerator = Color.Color8(192, 128, 64);     // Orange
    public static readonly Color TechLight = Color.Color8(224, 208, 128);        // Yellow light

    // Item Rarity Colors
    public static readonly Color Common = Color.Color8(150, 150, 150);           // Gray
    public static readonly Color Uncommon = Color.Color8(80, 220, 100);          // Green
    public static readonly Color Rare = Color.Color8(80, 140, 255);              // Blue
    public static readonly Color Epic = Color.Color8(200, 100, 255);             // Purple
    public static readonly Color Legendary = Color.Color8(255, 180, 50);         // Gold

    // Equipment Slot Colors
    public static readonly Color SlotCore = Color.Color8(255, 100, 100);         // Red - Logic
    public static readonly Color SlotUtility = Color.Color8(100, 230, 255);      // Cyan - External
    public static readonly Color SlotBase = Color.Color8(150, 230, 100);         // Green - Chassis

    // Alert/Status Colors
    public static readonly Color AlertDanger = Color.Color8(255, 60, 60);        // Bright red
    public static readonly Color AlertWarning = Color.Color8(255, 180, 50);      // Orange
    public static readonly Color AlertInfo = Color.Color8(80, 180, 255);         // Blue
    public static readonly Color AlertSuccess = Color.Color8(80, 255, 120);      // Green

    // Convenient aliases
    public static readonly Color Warning = AlertWarning;
    public static readonly Color Success = AlertSuccess;
    public static readonly Color Danger = AlertDanger;
    public static readonly Color Info = AlertInfo;

    // Text variants
    public static readonly Color TextNormal = TextSecondary;
    public static readonly Color TextDim = TextMuted;

    // Targeting Colors
    public static readonly Color TargetingCursor = Color.Color8(100, 255, 100);    // Bright green cursor
    public static readonly Color TargetingLine = Color.Color8(100, 100, 100);      // Gray line of fire
    public static readonly Color TargetingBlocked = Color.Color8(255, 80, 80);     // Red for blocked
    public static readonly Color TargetingPartial = Color.Color8(255, 200, 80);    // Yellow for partial cover
    public static readonly Color TargetingAoE = Color.Color8(80, 80, 150);         // Blue for AoE radius

    // Energy/Power Colors
    public static readonly Color Energy = Color.Color8(100, 200, 255);           // Cyan-blue
    public static readonly Color EnergyLow = Color.Color8(255, 150, 50);         // Orange warning
    public static readonly Color EnergyDepleted = Color.Color8(255, 80, 80);     // Red critical

    // Fire Colors
    public static readonly Color FireSpark = Color.Color8(255, 200, 100);        // Initial spark
    public static readonly Color FireSmolder = Color.Color8(200, 80, 40);        // Smoldering embers
    public static readonly Color FireFlame = Color.Color8(255, 120, 30);         // Active flame
    public static readonly Color FireBlaze = Color.Color8(255, 160, 60);         // Intense blaze
    public static readonly Color FireInferno = Color.Color8(255, 220, 150);      // White-hot
    public static readonly Color FireBright = Color.Color8(255, 200, 100);       // Bright glow
    public static readonly Color FireDying = Color.Color8(180, 60, 30);          // Dying embers
    public static readonly Color FireAsh = Color.Color8(80, 70, 60);             // Ashes/char

    // Explosion Colors
    public static readonly Color ExplosionCore = Color.Color8(255, 255, 200);    // White center
    public static readonly Color ExplosionOuter = Color.Color8(255, 100, 30);    // Orange ring
    public static readonly Color ExplosionSmoke = Color.Color8(100, 90, 80);     // Smoke
    public static readonly Color ExplosionPlasma = Color.Color8(100, 200, 255);  // Plasma blue

    // UI Element Colors
    public static readonly Color Border = PrimaryDim;
    public static readonly Color BorderBright = Primary;
    public static readonly Color Selection = Color.Color8(40, 80, 40);           // Selection highlight

    /// <summary>
    /// Get color for item rarity.
    /// </summary>
    public static Color GetRarityColor(Items.ItemRarity rarity)
    {
        return rarity switch
        {
            Items.ItemRarity.Common => Common,
            Items.ItemRarity.Uncommon => Uncommon,
            Items.ItemRarity.Rare => Rare,
            Items.ItemRarity.Epic => Epic,
            Items.ItemRarity.Legendary => Legendary,
            _ => Common
        };
    }

    /// <summary>
    /// Get color for equipment slot type.
    /// </summary>
    public static Color GetSlotColor(Items.EquipmentSlotType slotType)
    {
        return slotType switch
        {
            Items.EquipmentSlotType.Core => SlotCore,
            Items.EquipmentSlotType.Utility => SlotUtility,
            Items.EquipmentSlotType.Base => SlotBase,
            _ => TextMuted
        };
    }

    /// <summary>
    /// Interpolate health color from green to yellow to red.
    /// </summary>
    public static Color GetHealthColor(float healthPercent)
    {
        if (healthPercent > 0.6f)
            return Primary;
        else if (healthPercent > 0.3f)
            return AlertWarning;
        else
            return AlertDanger;
    }

    /// <summary>
    /// Dim a color for memory/fog rendering.
    /// </summary>
    public static Color Dimmed(Color color, float amount = 0.3f)
    {
        return new Color(
            color.R * amount,
            color.G * amount,
            color.B * amount,
            color.A
        );
    }

    /// <summary>
    /// Brighten a color for highlight effects.
    /// </summary>
    public static Color Brightened(Color color, float amount = 1.3f)
    {
        return new Color(
            Mathf.Min(color.R * amount, 1f),
            Mathf.Min(color.G * amount, 1f),
            Mathf.Min(color.B * amount, 1f),
            color.A
        );
    }
}
