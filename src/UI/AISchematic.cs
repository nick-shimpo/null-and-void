using Godot;
using System;
using NullAndVoid.Components;
using NullAndVoid.Items;

namespace NullAndVoid.UI;

/// <summary>
/// Schematic-style visualization of the AI core with equipment slots.
/// Displays a wireframe diagram with the AI "brain" at center and
/// equipment slots positioned around it based on their function.
/// </summary>
public partial class AISchematic : Control
{
    private Equipment? _equipment;

    // Slot buttons
    private Button[] _coreSlots = new Button[2];
    private Button[] _utilitySlots = new Button[2];
    private Button[] _baseSlots = new Button[2];

    // Animation state
    private float _pulsePhase = 0f;

    // Layout constants (based on 350px width panel)
    private const float SchematicWidth = 350f;
    private const float SchematicHeight = 400f;

    // AI Core position (center of schematic)
    private Vector2 CoreCenter => new(Size.X / 2, 180);
    private const float CoreOuterRadius = 45f;
    private const float CoreInnerRadius = 25f;
    private const float CoreEyeRadius = 8f;

    // Slot positions (relative to control)
    private Vector2 CoreSlot1Pos => new(Size.X / 2 - 85, 40);
    private Vector2 CoreSlot2Pos => new(Size.X / 2 + 15, 40);
    private Vector2 UtilitySlot1Pos => new(15, 155);
    private Vector2 UtilitySlot2Pos => new(Size.X - 70, 155);
    private Vector2 BaseSlot1Pos => new(Size.X / 2 - 95, 320);
    private Vector2 BaseSlot2Pos => new(Size.X / 2 + 15, 320);

    // Slot sizes
    private static readonly Vector2 CoreSlotSize = new(70, 40);
    private static readonly Vector2 UtilitySlotSize = new(55, 50);
    private static readonly Vector2 BaseSlotSize = new(80, 45);

    // Events
    public event Action<Item?, bool, EquipmentSlotType, int>? SlotSelected;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(SchematicWidth, SchematicHeight);
        CreateSlotButtons();
    }

    public override void _Process(double delta)
    {
        // Animate pulse
        _pulsePhase += (float)delta;
        QueueRedraw();
    }

    /// <summary>
    /// Connect to equipment component for data.
    /// </summary>
    public void SetEquipment(Equipment equipment)
    {
        if (_equipment != null)
            _equipment.EquipmentChanged -= OnEquipmentChanged;

        _equipment = equipment;

        if (_equipment != null)
        {
            _equipment.EquipmentChanged += OnEquipmentChanged;
            UpdateSlotDisplay();
        }
    }

    private void OnEquipmentChanged()
    {
        UpdateSlotDisplay();
    }

    private void CreateSlotButtons()
    {
        // Create Core slots (Logic - top, near brain)
        for (int i = 0; i < 2; i++)
        {
            _coreSlots[i] = CreateSlotButton(EquipmentSlotType.Core, i);
            _coreSlots[i].Position = i == 0 ? CoreSlot1Pos : CoreSlot2Pos;
            _coreSlots[i].Size = CoreSlotSize;
            AddChild(_coreSlots[i]);
        }

        // Create Utility slots (External - sides)
        for (int i = 0; i < 2; i++)
        {
            _utilitySlots[i] = CreateSlotButton(EquipmentSlotType.Utility, i);
            _utilitySlots[i].Position = i == 0 ? UtilitySlot1Pos : UtilitySlot2Pos;
            _utilitySlots[i].Size = UtilitySlotSize;
            AddChild(_utilitySlots[i]);
        }

        // Create Base slots (Chassis - bottom)
        for (int i = 0; i < 2; i++)
        {
            _baseSlots[i] = CreateSlotButton(EquipmentSlotType.Base, i);
            _baseSlots[i].Position = i == 0 ? BaseSlot1Pos : BaseSlot2Pos;
            _baseSlots[i].Size = BaseSlotSize;
            AddChild(_baseSlots[i]);
        }
    }

    private Button CreateSlotButton(EquipmentSlotType slotType, int index)
    {
        var button = new Button
        {
            ClipText = true,
            Text = TerminalTheme.FormatEmpty()
        };

        // Apply terminal styling
        TerminalTheme.StyleButton(button);

        // Color border based on slot type
        var slotColor = TerminalTheme.GetSlotColor(slotType);
        button.AddThemeColorOverride("font_color", slotColor);

        // Connect press event
        int capturedIndex = index;
        var capturedType = slotType;
        button.Pressed += () => OnSlotPressed(capturedType, capturedIndex);

        return button;
    }

    private void OnSlotPressed(EquipmentSlotType slotType, int index)
    {
        if (_equipment == null) return;

        var item = _equipment.GetItemInSlot(slotType, index);
        SlotSelected?.Invoke(item, true, slotType, index);
    }

    private void UpdateSlotDisplay()
    {
        if (_equipment == null) return;

        // Update Core slots
        for (int i = 0; i < 2; i++)
        {
            UpdateSlotButton(_coreSlots[i], EquipmentSlotType.Core, i);
        }

        // Update Utility slots
        for (int i = 0; i < 2; i++)
        {
            UpdateSlotButton(_utilitySlots[i], EquipmentSlotType.Utility, i);
        }

        // Update Base slots
        for (int i = 0; i < 2; i++)
        {
            UpdateSlotButton(_baseSlots[i], EquipmentSlotType.Base, i);
        }
    }

    private void UpdateSlotButton(Button button, EquipmentSlotType slotType, int index)
    {
        if (_equipment == null) return;

        var item = _equipment.GetItemInSlot(slotType, index);
        var slotColor = TerminalTheme.GetSlotColor(slotType);

        if (item != null)
        {
            button.Text = item.Name;
            button.AddThemeColorOverride("font_color", TerminalTheme.GetRarityColor(item.Rarity));
            button.AddThemeColorOverride("font_hover_color", TerminalTheme.PrimaryBright);
        }
        else
        {
            button.Text = TerminalTheme.FormatEmpty();
            button.AddThemeColorOverride("font_color", slotColor * 0.6f);
            button.AddThemeColorOverride("font_hover_color", slotColor);
        }
    }

    public override void _Draw()
    {
        DrawConnectionLines();
        DrawAICore();
        DrawSlotLabels();
    }

    private void DrawConnectionLines()
    {
        var lineColor = TerminalTheme.PrimaryDim;
        var lineWidth = 1.5f;

        // Core slot connections (vertical down to brain)
        var core1Bottom = CoreSlot1Pos + new Vector2(CoreSlotSize.X / 2, CoreSlotSize.Y);
        var core2Bottom = CoreSlot2Pos + new Vector2(CoreSlotSize.X / 2, CoreSlotSize.Y);
        var coreTop = CoreCenter - new Vector2(0, CoreOuterRadius);

        DrawLine(core1Bottom, new Vector2(core1Bottom.X, coreTop.Y - 10), TerminalTheme.SlotCore * 0.7f, lineWidth);
        DrawLine(new Vector2(core1Bottom.X, coreTop.Y - 10), coreTop, TerminalTheme.SlotCore * 0.7f, lineWidth);

        DrawLine(core2Bottom, new Vector2(core2Bottom.X, coreTop.Y - 10), TerminalTheme.SlotCore * 0.7f, lineWidth);
        DrawLine(new Vector2(core2Bottom.X, coreTop.Y - 10), coreTop, TerminalTheme.SlotCore * 0.7f, lineWidth);

        // Utility slot connections (horizontal to brain sides)
        var util1Right = UtilitySlot1Pos + new Vector2(UtilitySlotSize.X, UtilitySlotSize.Y / 2);
        var util2Left = UtilitySlot2Pos + new Vector2(0, UtilitySlotSize.Y / 2);
        var coreLeft = CoreCenter - new Vector2(CoreOuterRadius, 0);
        var coreRight = CoreCenter + new Vector2(CoreOuterRadius, 0);

        DrawLine(util1Right, coreLeft, TerminalTheme.SlotUtility * 0.7f, lineWidth);
        DrawLine(util2Left, coreRight, TerminalTheme.SlotUtility * 0.7f, lineWidth);

        // Base slot connections (through data bus)
        var base1Top = BaseSlot1Pos + new Vector2(BaseSlotSize.X / 2, 0);
        var base2Top = BaseSlot2Pos + new Vector2(BaseSlotSize.X / 2, 0);
        var coreBottom = CoreCenter + new Vector2(0, CoreOuterRadius);
        var busY = BaseSlot1Pos.Y - 25;

        // Draw data bus line
        DrawLine(new Vector2(base1Top.X - 20, busY), new Vector2(base2Top.X + 20, busY), TerminalTheme.SlotBase * 0.5f, lineWidth);

        // Bus label
        DrawCircle(new Vector2(Size.X / 2, busY), 4, TerminalTheme.SlotBase * 0.7f);

        // Connections from bus to slots and core
        DrawLine(base1Top, new Vector2(base1Top.X, busY), TerminalTheme.SlotBase * 0.7f, lineWidth);
        DrawLine(base2Top, new Vector2(base2Top.X, busY), TerminalTheme.SlotBase * 0.7f, lineWidth);
        DrawLine(new Vector2(Size.X / 2, busY), coreBottom, TerminalTheme.SlotBase * 0.7f, lineWidth);
    }

    private void DrawAICore()
    {
        var center = CoreCenter;

        // Outer hexagon shape (simplified as circle for now)
        DrawArc(center, CoreOuterRadius, 0, Mathf.Tau, 32, TerminalTheme.Primary, 2f);

        // Middle ring with rotation effect
        float rotation = _pulsePhase * 0.3f;
        DrawArc(center, CoreOuterRadius - 8, rotation, rotation + Mathf.Tau, 32, TerminalTheme.PrimaryDim, 1.5f);

        // Inner pulsing core
        float pulse = 0.6f + 0.4f * Mathf.Sin(_pulsePhase * 2f);
        var pulseColor = new Color(
            TerminalTheme.Primary.R * pulse,
            TerminalTheme.Primary.G * pulse,
            TerminalTheme.Primary.B * pulse
        );
        DrawCircle(center, CoreInnerRadius, pulseColor * 0.3f);
        DrawArc(center, CoreInnerRadius, 0, Mathf.Tau, 24, pulseColor, 2f);

        // Central "eye"
        float eyePulse = 0.8f + 0.2f * Mathf.Sin(_pulsePhase * 3f);
        var eyeColor = new Color(
            TerminalTheme.PrimaryBright.R,
            TerminalTheme.PrimaryBright.G,
            TerminalTheme.PrimaryBright.B,
            eyePulse
        );
        DrawCircle(center, CoreEyeRadius, eyeColor);

        // "AI" label below core
        // Note: DrawString requires a font, using simple indicator instead
        var labelPos = center + new Vector2(0, CoreOuterRadius + 15);
        DrawCircle(labelPos, 3, TerminalTheme.Primary);
    }

    private void DrawSlotLabels()
    {
        // Draw slot type indicators near each slot region
        // These are small visual markers showing the slot category

        // Core label area (top)
        var coreLabelY = CoreSlot1Pos.Y - 15;
        DrawLine(
            new Vector2(CoreSlot1Pos.X, coreLabelY),
            new Vector2(CoreSlot2Pos.X + CoreSlotSize.X, coreLabelY),
            TerminalTheme.SlotCore * 0.5f, 1f
        );

        // Utility indicators (side markers)
        DrawRect(new Rect2(UtilitySlot1Pos.X - 8, UtilitySlot1Pos.Y + 15, 4, 20), TerminalTheme.SlotUtility * 0.5f);
        DrawRect(new Rect2(UtilitySlot2Pos.X + UtilitySlotSize.X + 4, UtilitySlot2Pos.Y + 15, 4, 20), TerminalTheme.SlotUtility * 0.5f);

        // Base label area (bottom)
        var baseLabelY = BaseSlot1Pos.Y + BaseSlotSize.Y + 10;
        DrawLine(
            new Vector2(BaseSlot1Pos.X, baseLabelY),
            new Vector2(BaseSlot2Pos.X + BaseSlotSize.X, baseLabelY),
            TerminalTheme.SlotBase * 0.5f, 1f
        );
    }

    public override void _ExitTree()
    {
        if (_equipment != null)
            _equipment.EquipmentChanged -= OnEquipmentChanged;
    }
}
