using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using WorldShapingWandsMod.Common.Configs;
using WorldShapingWandsMod.Common.Enums;
using WorldShapingWandsMod.Common.Selection;
using WorldShapingWandsMod.Common.Settings;

namespace WorldShapingWandsMod.Common.Players;

public class PlanificationWandPlayer : ModPlayer
{
    public const int StencilSlotCount = 5;

    private const string TagActiveEditSlot = "Planification_ActiveEditSlot";
    private const string TagActiveSlotsMask = "Planification_ActiveSlotsMask";
    private const string TagRenderCfgMaskPrefix = "Planification_RenderCfgMask_";

    public StoredMagicWandShape[] CanvasSlots { get; } = new StoredMagicWandShape[StencilSlotCount];

    public StoredMagicWandShape[] SelectionSlots { get; } = new StoredMagicWandShape[StencilSlotCount];

    public int ActiveEditSlot { get; private set; }

    public byte ActiveSlotsMask { get; private set; } = 0b00001;

    public PlanificationRenderConfig[] PerStencilRenderConfigs { get; }
        = new PlanificationRenderConfig[StencilSlotCount];

    public PlanificationWandSettings Settings { get; private set; } = new();

    public override void Initialize()
    {
        for (int i = 0; i < StencilSlotCount; i++)
            PerStencilRenderConfigs[i] = PlanificationRenderConfig.Default;
    }

    public bool IsSlotVisible(int slot)
    {
        if (slot < 0 || slot >= StencilSlotCount)
            return false;

        return (ActiveSlotsMask & (1 << slot)) != 0;
    }

    public void SetActiveEditSlot(int slot)
    {
        if (slot < 0 || slot >= StencilSlotCount)
            return;

        ActiveEditSlot = slot;
    }

    public void ToggleSlotVisibility(int slot)
    {
        if (slot < 0 || slot >= StencilSlotCount)
            return;

        ActiveSlotsMask ^= (byte)(1 << slot);
    }

    public void SetSlotVisibility(int slot, bool visible)
    {
        if (slot < 0 || slot >= StencilSlotCount)
            return;

        if (visible)
            ActiveSlotsMask |= (byte)(1 << slot);
        else
            ActiveSlotsMask &= (byte)~(1 << slot);
    }

    public PlanificationRenderConfig GetRenderConfig(int slot)
    {
        if (slot < 0 || slot >= StencilSlotCount)
            return PlanificationRenderConfig.Default;

        return PerStencilRenderConfigs[slot];
    }

    public void SetRenderConfig(int slot, PlanificationRenderConfig config)
    {
        if (slot < 0 || slot >= StencilSlotCount)
            return;

        PerStencilRenderConfigs[slot] = config;
    }

    public void SetSlotCanvasShape(int slot, HashSet<Point> worldTiles)
    {
        SetStoredSlot(CanvasSlots, slot, worldTiles);
        if (worldTiles?.Count > 0)
            SetSlotVisibility(slot, true);
    }

    public void SetSlotSelectionShape(int slot, HashSet<Point> worldTiles)
    {
        SetStoredSlot(SelectionSlots, slot, worldTiles);
        if (worldTiles?.Count > 0)
            SetSlotVisibility(slot, true);
    }

    private static void SetStoredSlot(StoredMagicWandShape[] slots, int slot, HashSet<Point> worldTiles)
    {
        if (slot < 0 || slot >= StencilSlotCount || worldTiles == null || worldTiles.Count == 0)
            return;

        int minX = int.MaxValue;
        int minY = int.MaxValue;
        foreach (var tile in worldTiles)
        {
            if (tile.X < minX) minX = tile.X;
            if (tile.Y < minY) minY = tile.Y;
        }

        var origin = new Point(minX, minY);
        var relative = new HashSet<Point>(worldTiles.Count);
        foreach (var tile in worldTiles)
            relative.Add(new Point(tile.X - origin.X, tile.Y - origin.Y));

        slots[slot] = new StoredMagicWandShape(
            relative,
            origin,
            configAtCapture: MagicWandReadConfig.Default,
            capturedAtTicks: DateTime.UtcNow.Ticks);
    }

    public void ClearSlot(int slot)
    {
        if (slot < 0 || slot >= StencilSlotCount)
            return;

        CanvasSlots[slot] = null;
        SelectionSlots[slot] = null;
    }

    public void ReplaceSlotWorldTiles(int slot, HashSet<Point> worldTiles)
    {
        if (slot < 0 || slot >= StencilSlotCount)
            return;

        if (worldTiles == null || worldTiles.Count == 0)
        {
            SelectionSlots[slot] = null;
            return;
        }

        SetSlotSelectionShape(slot, worldTiles);
    }

    public HashSet<Point> GetSlotCanvasWorldTiles(int slot)
        => GetStoredSlotWorldTiles(CanvasSlots, slot);

    public HashSet<Point> GetSlotSelectionWorldTiles(int slot)
        => GetStoredSlotWorldTiles(SelectionSlots, slot);

    private static HashSet<Point> GetStoredSlotWorldTiles(StoredMagicWandShape[] slots, int slot)
    {
        var result = new HashSet<Point>();
        if (slot < 0 || slot >= StencilSlotCount)
            return result;

        var stored = slots[slot];
        if (stored?.Tiles == null || stored.Tiles.Count == 0)
            return result;

        foreach (var rel in stored.Tiles)
            result.Add(new Point(stored.Origin.X + rel.X, stored.Origin.Y + rel.Y));

        return result;
    }

    public void ApplyCanvasOperationToSlot(int slot, HashSet<Point> operandTiles, SelectionOperation operation)
    {
        if (slot < 0 || slot >= StencilSlotCount)
            return;

        operandTiles ??= new HashSet<Point>();
        var current = GetSlotCanvasWorldTiles(slot);
        var result = new HashSet<Point>(current);

        switch (operation)
        {
            case SelectionOperation.Add:
                result.UnionWith(operandTiles);
                break;
            case SelectionOperation.Remove:
                result.ExceptWith(operandTiles);
                break;
            case SelectionOperation.Clear:
                result.Clear();
                break;
            default:
                result.UnionWith(operandTiles);
                break;
        }

        if (result.Count == 0)
        {
            CanvasSlots[slot] = null;
            return;
        }

        SetSlotCanvasShape(slot, result);
    }

    public void ApplySelectionOperationToSlot(int slot, HashSet<Point> operandTiles, SelectionOperation operation)
    {
        if (slot < 0 || slot >= StencilSlotCount)
            return;

        operandTiles ??= new HashSet<Point>();
        var current = GetSlotSelectionWorldTiles(slot);

        var result = new HashSet<Point>(current);
        switch (operation)
        {
            case SelectionOperation.Add:
                result.UnionWith(operandTiles);
                break;
            case SelectionOperation.Remove:
                result.ExceptWith(operandTiles);
                break;
            case SelectionOperation.Intersect:
                result.IntersectWith(operandTiles);
                break;
            case SelectionOperation.XOR:
                result.SymmetricExceptWith(operandTiles);
                break;
            case SelectionOperation.Clear:
                result.Clear();
                break;
            default:
                result = new HashSet<Point>(operandTiles);
                break;
        }

        if (result.Count == 0)
        {
            SelectionSlots[slot] = null;
            return;
        }

        SetSlotSelectionShape(slot, result);
    }

    public void ClipSelectionToCanvas(int slot)
    {
        if (slot < 0 || slot >= StencilSlotCount)
            return;

        var canvas = GetSlotCanvasWorldTiles(slot);
        if (canvas.Count == 0)
        {
            SelectionSlots[slot] = null;
            return;
        }

        var selection = GetSlotSelectionWorldTiles(slot);
        if (selection.Count == 0)
            return;

        selection.IntersectWith(canvas);
        if (selection.Count == 0)
        {
            SelectionSlots[slot] = null;
            return;
        }

        SetSlotSelectionShape(slot, selection);
    }

    public void ClearAllSlots()
    {
        for (int i = 0; i < StencilSlotCount; i++)
        {
            CanvasSlots[i] = null;
            SelectionSlots[i] = null;
        }
    }

    public override void OnEnterWorld()
    {
        ClearAllSlots();
        Settings.ResetToDefaults();
    }

    public override void SaveData(TagCompound tag)
    {
        tag[TagActiveEditSlot] = ActiveEditSlot;
        tag[TagActiveSlotsMask] = (int)ActiveSlotsMask;

        for (int i = 0; i < StencilSlotCount; i++)
        {
            int packed = 0;
            var cfg = PerStencilRenderConfigs[i];
            if (cfg.ShowOutline) packed |= 1;
            if (cfg.ShowGrid) packed |= 2;
            if (cfg.ShowFill) packed |= 4;
            tag[$"{TagRenderCfgMaskPrefix}{i}"] = packed;
        }
    }

    public override void LoadData(TagCompound tag)
    {
        ActiveEditSlot = 0;
        ActiveSlotsMask = 0b00001;

        if (tag.ContainsKey(TagActiveEditSlot))
            ActiveEditSlot = Math.Clamp(tag.GetInt(TagActiveEditSlot), 0, StencilSlotCount - 1);

        if (tag.ContainsKey(TagActiveSlotsMask))
            ActiveSlotsMask = (byte)tag.GetInt(TagActiveSlotsMask);

        for (int i = 0; i < StencilSlotCount; i++)
        {
            int packed = 1;
            string key = $"{TagRenderCfgMaskPrefix}{i}";
            if (tag.ContainsKey(key))
                packed = tag.GetInt(key);

            PerStencilRenderConfigs[i] = new PlanificationRenderConfig
            {
                ShowOutline = (packed & 1) != 0,
                ShowGrid = (packed & 2) != 0,
                ShowFill = (packed & 4) != 0,
            };
        }
    }
}
