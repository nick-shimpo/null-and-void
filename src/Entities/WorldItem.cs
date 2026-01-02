using Godot;
using NullAndVoid.Combat;
using NullAndVoid.Items;
using NullAndVoid.Rendering;

namespace NullAndVoid.Entities;

/// <summary>
/// An item placed in the world that can be picked up by the player.
/// </summary>
public partial class WorldItem : Entity
{
    /// <summary>
    /// The item contained in this pickup (weapon, module, etc.).
    /// </summary>
    public Item? ContainedItem { get; set; }

    /// <summary>
    /// Ammunition contained in this pickup (if it's an ammo drop).
    /// </summary>
    public Ammunition? ContainedAmmo { get; set; }

    private char _displayChar = '?';
    private Color _displayColor = Colors.White;

    /// <summary>
    /// Default constructor for scene instantiation.
    /// </summary>
    public WorldItem() { }

    /// <summary>
    /// Create a world item containing an Item.
    /// </summary>
    public WorldItem(Item item, Vector2I position)
    {
        ContainedItem = item;
        GridPosition = position;
        EntityName = item.Name;
        SetDisplayFromItem(item);
    }

    /// <summary>
    /// Create a world item containing ammunition.
    /// </summary>
    public WorldItem(Ammunition ammo, Vector2I position)
    {
        ContainedAmmo = ammo;
        GridPosition = position;
        EntityName = ammo.Name;
        SetDisplayFromAmmo(ammo);
    }

    public override void _Ready()
    {
        base._Ready();
        AddToGroup("WorldItems");
        // Rendered by ASCII EntityRenderer - no visual nodes needed
    }

    private void SetDisplayFromItem(Item item)
    {
        // Determine display character based on item type
        if (item.WeaponData != null)
        {
            _displayChar = item.WeaponData.Category switch
            {
                WeaponCategory.Melee => '/',
                WeaponCategory.Projectile => '}',
                WeaponCategory.Energy => '~',
                WeaponCategory.AreaEffect => '*',
                _ => '?'
            };
        }
        else
        {
            _displayChar = item.ModuleCategory switch
            {
                ModuleType.Generator => 'G',
                ModuleType.Shield => 'S',
                ModuleType.Sensor => '?',
                ModuleType.Treads => 'T',
                ModuleType.Legs => 'L',
                _ => '+'
            };
        }

        _displayColor = item.DisplayColor;
    }

    private void SetDisplayFromAmmo(Ammunition ammo)
    {
        _displayChar = ammo.Type switch
        {
            AmmoType.Basic => '=',
            AmmoType.Energy => '%',
            AmmoType.Seeker => '>',
            AmmoType.Orbital => '!',
            _ => '='
        };

        _displayColor = ammo.DisplayColor;
    }

    /// <summary>
    /// Attempt to pick up this item. Returns the item and removes from world.
    /// </summary>
    public Item? PickupItem()
    {
        var item = ContainedItem;
        ContainedItem = null;
        QueueFree();
        return item;
    }

    /// <summary>
    /// Attempt to pick up this ammo. Returns the ammo and removes from world.
    /// </summary>
    public Ammunition? PickupAmmo()
    {
        var ammo = ContainedAmmo;
        ContainedAmmo = null;
        QueueFree();
        return ammo;
    }

    /// <summary>
    /// Check if this is an ammo pickup.
    /// </summary>
    public bool IsAmmoPickup => ContainedAmmo != null;

    /// <summary>
    /// Check if this is an item pickup.
    /// </summary>
    public bool IsItemPickup => ContainedItem != null;

    /// <summary>
    /// Get display character for rendering.
    /// </summary>
    public char DisplayChar => _displayChar;

    /// <summary>
    /// Get display color for rendering.
    /// </summary>
    public Color DisplayColor => _displayColor;

    /// <summary>
    /// Get a description of the pickup.
    /// </summary>
    public string GetDescription()
    {
        if (ContainedItem != null)
            return $"{ContainedItem.Name} [{ContainedItem.Rarity}]";
        if (ContainedAmmo != null)
            return $"{ContainedAmmo.Name} x{ContainedAmmo.Quantity}";
        return "Empty pickup";
    }
}
