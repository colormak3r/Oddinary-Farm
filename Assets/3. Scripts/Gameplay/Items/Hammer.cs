using ColorMak3r.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public enum HammerMode : int
{
    Fix = 0,
    Move = 1,
    Remove = 2
}

public class Hammer : Tool
{
    private HammerProperty hammerProperty;
    [SerializeField]
    private Structure currentStructure;
    [SerializeField]
    private HammerMode currentMode;

    protected override void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        base.HandleOnPropertyChanged(previousValue, newValue);
        hammerProperty = (HammerProperty)newValue;
    }

    public override void OnPreview(Vector2 position, Previewer previewer)
    {
        previewer.Show(true);

        previewer.SetIconOffset(Vector2.zero);
        switch (currentMode)
        {
            case HammerMode.Fix:
                previewer.SetIcon(hammerProperty.FixIconSprite);
                break;
            case HammerMode.Move:
                if (currentStructure)
                {
                    previewer.SetIcon(currentStructure.Property.Sprite);
                    previewer.SetIconOffset(-currentStructure.Property.Offset);
                }
                else
                    previewer.SetIcon(hammerProperty.MoveIconSprite);
                break;
            case HammerMode.Remove:
                previewer.SetIcon(hammerProperty.RemoveIconSprite);
                break;
        }

        if (currentStructure)
        {
            var size = currentStructure.Property.Size;
            previewer.MoveTo(position.SnapToGrid(size));
            previewer.SetSize(size);
        }
        else
        {
            var structureHit = Physics2D.OverlapPoint(position, hammerProperty.StructureLayers);
            if (structureHit && structureHit.TryGetComponent(out Structure structure))
            {
                var size = structure.Property.Size;
                previewer.MoveTo((Vector2)structureHit.transform.position + structure.Property.Offset);
                previewer.SetSize(size);
            }
            else
            {
                previewer.MoveTo(position.SnapToGrid());
                previewer.SetSize(hammerProperty.Size);
            }
        }

        if (CanPrimaryAction(position))
        {
            previewer.SetColor(hammerProperty.PreviewValidColor);
        }
        else
        {
            previewer.SetColor(hammerProperty.PreviewInvalidColor);
        }
    }

    private Vector2 position_cached;
    public override bool CanPrimaryAction(Vector2 position)
    {
        position_cached = position;
        if (currentMode == HammerMode.Move)
        {
            if (currentStructure)
            {
                var size = currentStructure.Property.Size;
                // Check if the position is in range
                position = position.SnapToGrid(size);
                if (!IsInRange(position)) return false;

                // Check if the structure is in an invalid position
                var invalid = OverlapArea(size, position, hammerProperty.InvalidLayers);
                if (invalid) return false;

                // Check if the structure is overlapping with another structure
                var structure = OverlapArea(size, position, hammerProperty.StructureLayers);
                if (structure) return false;

                return true;
            }
            else
            {
                // Check if a valid structure exist at the position
                var structureHit = Physics2D.OverlapPoint(position, hammerProperty.StructureLayers);
                if (structureHit && structureHit.TryGetComponent(out Structure structure))
                {
                    // Check if the position of the structure is in range
                    position = (Vector2)structureHit.transform.position + structure.Property.Offset;
                    if (!IsInRange(position)) return false;

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        else
        {
            // Check if a valid structure exist at the position
            var structureHit = Physics2D.OverlapPoint(position, hammerProperty.StructureLayers);
            if (structureHit && structureHit.TryGetComponent(out Structure structure))
            {
                // Check if the position of the structure is in range
                position = (Vector2)structureHit.transform.position + structure.Property.Offset;
                if (!IsInRange(position)) return false;

                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        position = position.SnapToGrid();
        base.OnPrimaryAction(position);
        HammerPrimary(position, currentMode);
    }

    private void HammerPrimary(Vector2 position, HammerMode mode)
    {
        switch (mode)
        {
            case HammerMode.Fix:
                FixStructureOnServer(position);
                break;
            case HammerMode.Move:
                MoveStructureOnServer(position);
                break;
            case HammerMode.Remove:
                RemoveStructureOnServer(position);
                break;
        }
    }

    private void RemoveStructureOnServer(Vector2 position)
    {
        var structureHit = Physics2D.OverlapPoint(position, hammerProperty.StructureLayers);
        if (structureHit && structureHit.TryGetComponent(out Structure structure))
        {
            structure.RemoveStructure();
        }
    }

    private void MoveStructureOnServer(Vector2 position)
    {
        if (!currentStructure)
        {
            var structureHit = Physics2D.OverlapPoint(position, hammerProperty.StructureLayers);
            if (structureHit && structureHit.TryGetComponent(out Structure structure))
            {
                if (showDebug) Debug.Log("Setting current structure");
                currentStructure = structure;
            }
            else
            {
                if (showDebug) Debug.Log("No current structure: " + position);
            }
        }
        else
        {
            if (showDebug) Debug.Log("Moving current structure");
            position = position.SnapToGrid(currentStructure.Property.Size) + currentStructure.Property.StructureItemProperty.SpawnOffset;
            currentStructure.MoveTo(position);
            currentStructure = null;
        }
    }

    private void FixStructureOnServer(Vector2 position)
    {
        var structureHit = Physics2D.OverlapPoint(position, hammerProperty.StructureLayers);
        if (structureHit && structureHit.TryGetComponent(out StructureStatus structureStatus))
        {
            structureStatus.GetHealed(1);
        }
    }

    public override void OnSecondaryAction(Vector2 position)
    {
        base.OnSecondaryAction(position);
        currentMode = (HammerMode)(((int)currentMode + 1) % Enum.GetValues(typeof(HammerMode)).Length);
        if (currentMode != HammerMode.Move) currentStructure = null;
        if (showDebug) Debug.Log("Current mode: " + currentMode);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        if (currentMode == HammerMode.Move)
        {
            Gizmos.color = Color.red;
            if (currentStructure)
            {
                var size = currentStructure.Property.Size;
                Gizmos.DrawCube(position_cached.SnapToGrid(size), size);
            }
            else
            {
                // Check if a valid structure exist at the position
                var structureHit = Physics2D.OverlapPoint(position_cached, hammerProperty.StructureLayers);
                if (structureHit && structureHit.TryGetComponent(out Structure structure))
                {
                    // Check if the position of the structure is in range
                    position_cached = (Vector2)structureHit.transform.position + structure.Property.Offset;
                    Gizmos.DrawCube(position_cached, structure.Property.Size);
                }
                else
                {
                    Gizmos.DrawCube(position_cached.SnapToGrid(), hammerProperty.Size);
                }
            }
        }
    }
}