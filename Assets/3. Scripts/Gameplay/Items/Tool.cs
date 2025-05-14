using ColorMak3r.Utility;
using UnityEngine;

public class Tool : Item
{
    private ToolProperty toolProperty;

    public override void Initialize(ItemProperty baseProperty)
    {
        base.Initialize(baseProperty);
        toolProperty = baseProperty as ToolProperty;
    }

    public override void OnPreview(Vector2 position, Previewer previewer)
    {
        if (toolProperty == null) return;

        position = position.SnapToGrid(toolProperty.Size);
        previewer.MoveTo(position);
        previewer.Show(true);
        previewer.SetIconOffset(toolProperty.PreviewIconOffset);
        previewer.SetIcon(toolProperty.PreviewIconSprite);
        previewer.SetSize(toolProperty.Size);
        if (CanPrimaryAction(position))
        {
            previewer.SetColor(toolProperty.PreviewValidColor);
        }
        else
        {
            previewer.SetColor(toolProperty.PreviewInvalidColor);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, toolProperty.Range);
    }
}
