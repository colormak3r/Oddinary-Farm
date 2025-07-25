/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/24/2025 (Khoa)
 * Notes:           <write here>
*/

using ColorMak3r.Utility;
using UnityEngine;

public class PickAxe : MeleeWeapon
{
    private PickaxeProperty pickaxeProperty;

    public override void Initialize(ItemProperty baseProperty)
    {
        base.Initialize(baseProperty);
        pickaxeProperty = (PickaxeProperty)baseProperty;
    }

    private Vector2 position_cached;
    public override void OnPreview(Vector2 position, Previewer previewer)
    {
        position = position.SnapToGrid(pickaxeProperty.Size);
        if (position_cached == position) return;
        position_cached = position;

        var hit = Physics2D.OverlapPoint(position, LayerManager.Main.MineableLayer);
        if (hit)
        {
            previewer.MoveTo(position);
            previewer.Show(true);
            previewer.SetIconOffset(pickaxeProperty.PreviewIconOffset);
            previewer.SetIcon(pickaxeProperty.PreviewIconSprite);
            previewer.SetSize(pickaxeProperty.Size);
            if (CanPrimaryAction(position))
            {
                previewer.SetColor(pickaxeProperty.PreviewValidColor);
            }
            else
            {
                previewer.SetColor(pickaxeProperty.PreviewInvalidColor);
            }
        }
        else
        {

            previewer.Show(false);
        }
    }

    public override void OnPrimaryAction(Vector2 position)
    {
        base.OnPrimaryAction(position);
        ItemSystem.Mine(position);
    }
}
