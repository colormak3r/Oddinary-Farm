using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.UI.Image;


public static class SerializationExtensions
{
    private static string NULL_STRING = "null";

    public static void WriteValueSafe(this FastBufferWriter writer, in ItemProperty obj)
    {
        writer.WriteValueSafe(obj == null ? NULL_STRING : obj.name);
    }

    public static void ReadValueSafe(this FastBufferReader reader, out ItemProperty obj)
    {
        reader.ReadValueSafe(out string objName);
        if (objName == NULL_STRING)
            obj = null;
        else
            obj = AssetManager.Main.GetScriptableObjectByName<ItemProperty>(objName);
    }

    public static void DuplicateValue (in ItemProperty value, ref ItemProperty duplicatedValue)
    {
        duplicatedValue = value;
    }


    /*public static void ReadValueSafe(this FastBufferReader reader, out NetworkedScriptableObject obj)
    {
        reader.ReadValueSafe(out string val);
        if(val == NULL_STRING)
            obj = null;
        else
            obj = (NetworkedScriptableObject)AssetManager.Main.GetAssetByName(val);
    }

    public static void WriteValueSafe(this FastBufferWriter writer, in NetworkedScriptableObject obj)
    {
        writer.WriteValueSafe(obj == null? NULL_STRING: obj.name);
    }

    public static void ReadValueSafe(this FastBufferReader reader, out Sprite obj)
    {
        reader.ReadValueSafe(out string val);
        if (val == NULL_STRING)
            obj = null;
        else
            obj = SpriteManager.Main.GetAsset(val);
    }

    public static void WriteValueSafe(this FastBufferWriter writer, in Sprite obj)
    {
        writer.WriteValueSafe(obj == null ? NULL_STRING : obj.name);
    }

    public static void DuplicateValue(in Sprite original, ref Sprite duplicate)
    {
        if (original != null)
        {
            // Get the texture from the original sprite
            Texture2D originalTexture = original.texture;

            // Create a new texture with the same dimensions as the original texture
            Texture2D newTexture = new Texture2D(originalTexture.width, originalTexture.height, originalTexture.format, originalTexture.mipmapCount > 1);

            // Copy the pixel data from the original texture to the new texture
            newTexture.SetPixels(originalTexture.GetPixels());
            newTexture.Apply();

            // Create a new sprite using the copied texture
            duplicate = Sprite.Create(newTexture, original.rect, new Vector2(0.5f, 0.5f), original.pixelsPerUnit);
        }
        else
        {
            duplicate = null;
        }       
    }*/
}
