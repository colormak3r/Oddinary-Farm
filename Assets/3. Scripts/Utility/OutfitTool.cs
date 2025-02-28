#if UNITY_EDITOR
using UnityEditor.U2D.Sprites;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class OutfitTool
{
    private static Vector2[] PIVOT =
    {
        new Vector2(0.40625f, 0.046875f),     // Left Leg
        new Vector2(0.59375f, 0.046875f),     // Right Leg
        new Vector2(0.3125f,  0.140625f),   // Left Arm
        new Vector2(0.6875f,  0.140625f),   // Right Arm
        new Vector2(0.5f,     0.125f),      // Torso
        new Vector2(0.5f,     0.171875f),   // Head
        new Vector2(0.5f,     0.171875f),   // Face
        new Vector2(0.5f,     0.34375f),    // Hat
    };

    private static string FACE_PATH = "Assets/4. Scriptable Object/Appearance/Face";
    private static string HAT_PATH = "Assets/4. Scriptable Object/Appearance/Hat";
    private static string HEAD_PATH = "Assets/4. Scriptable Object/Appearance/Head";
    private static string OUTFIT_PATH = "Assets/4. Scriptable Object/Appearance/Outfit";

    [MenuItem("Outfit/Auto Slice")]
    private static void AutoSlice()
    {
        var factory = new SpriteDataProviderFactories();
        factory.Init();

        // Process every selected Texture2D asset
        foreach (var obj in Selection.objects)
        {
            if (obj is Texture2D texture)
            {
                string path = AssetDatabase.GetAssetPath(texture);
                TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

                if (textureImporter == null || textureImporter.textureType != TextureImporterType.Sprite)
                {
                    Debug.LogError($"Selected asset at {path} is not a sprite.");
                    continue;
                }

                // Ensure the asset is readable and set to multiple sprites
                textureImporter.isReadable = true;
                textureImporter.spriteImportMode = SpriteImportMode.Multiple;
                textureImporter.spritePixelsPerUnit = 16;
                textureImporter.filterMode = FilterMode.Point;
                textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                var provider = factory.GetSpriteEditorDataProviderFromObject(obj);
                if (provider == null)
                {
                    Debug.LogError($"Failed to get ISpriteEditorDataProvider for {path}");
                    continue;
                }

                provider.InitSpriteEditorDataProvider();

                // Define slice dimensions (32x64) and create 8 sprite rects
                int sliceWidth = 32;
                int sliceHeight = 64;
                int slices = 8;
                SpriteRect[] newRects = new SpriteRect[slices];

                for (int i = 0; i < slices; i++)
                {
                    newRects[i] = new SpriteRect
                    {
                        name = $"{obj.name}_{i}",
                        // Assuming slices are arranged horizontally:
                        rect = new Rect(i * sliceWidth, 0, sliceWidth, sliceHeight),
                        pivot = PIVOT[i],
                        alignment = SpriteAlignment.Custom
                    };
                }

                // Apply the new slicing data
                provider.SetSpriteRects(newRects);
                provider.Apply();

                // Re-import asset to update changes in the editor
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Debug.Log($"Sprite slicing completed for {path}");
            }
        }
    }

    [MenuItem("Outfit/Export Appearance")]
    private static void ExportAppearance()
    {
        // Ensure the target folders exist.
        EnsureFolder(HEAD_PATH);
        EnsureFolder(FACE_PATH);
        EnsureFolder(HAT_PATH);
        EnsureFolder(OUTFIT_PATH);

        // Process each selected texture asset.
        foreach (var obj in Selection.objects)
        {
            if (!(obj is Texture2D texture))
            {
                Debug.LogError("Please select a Texture2D asset with sliced sprites.");
                continue;
            }

            string texturePath = AssetDatabase.GetAssetPath(texture);
            // Load all sprites from the texture asset (sliced sprites are sub-assets).
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
                                .OfType<Sprite>()
                                .ToArray();

            // Build a lookup dictionary using the naming scheme: {texture.name}_{index}
            Dictionary<int, Sprite> spriteMap = new Dictionary<int, Sprite>();
            for (int i = 0; i < 8; i++)
            {
                Sprite found = sprites.FirstOrDefault(s => s.name == $"{texture.name}_{i}");
                if (found == null)
                {
                    Debug.LogError($"Could not find sprite: {texture.name}_{i}");
                    break;
                }
                spriteMap[i] = found;
            }

            if (spriteMap.Count < 8)
            {
                Debug.LogError("Not all required sprites were found. Ensure that Auto Slice has been executed.");
                continue;
            }

            var textureName = texture.name.Replace("Outfit_", "").Replace("_", " ");

            // --- Update or Create Head asset (using sprite index 5) ---
            string headAssetPath = Path.Combine(HEAD_PATH, textureName + " Head.asset");
            Head headAsset = AssetDatabase.LoadAssetAtPath<Head>(headAssetPath);
            if (headAsset == null)
            {
                headAsset = ScriptableObject.CreateInstance<Head>();
                AssetDatabase.CreateAsset(headAsset, headAssetPath);
            }
            SerializedObject headSO = new SerializedObject(headAsset);
            headSO.FindProperty("displaySprite").objectReferenceValue = spriteMap[5];
            headSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(headAsset);

            // --- Update or Create Face asset (using sprite index 6) ---
            string faceAssetPath = Path.Combine(FACE_PATH, textureName + " Face.asset");
            Face faceAsset = AssetDatabase.LoadAssetAtPath<Face>(faceAssetPath);
            if (faceAsset == null)
            {
                faceAsset = ScriptableObject.CreateInstance<Face>();
                AssetDatabase.CreateAsset(faceAsset, faceAssetPath);
            }
            SerializedObject faceSO = new SerializedObject(faceAsset);
            faceSO.FindProperty("displaySprite").objectReferenceValue = spriteMap[6];
            faceSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(faceAsset);

            // --- Update or Create Hat asset (using sprite index 7) ---
            string hatAssetPath = Path.Combine(HAT_PATH, textureName + " Hat.asset");
            Hat hatAsset = AssetDatabase.LoadAssetAtPath<Hat>(hatAssetPath);
            if (hatAsset == null)
            {
                hatAsset = ScriptableObject.CreateInstance<Hat>();
                AssetDatabase.CreateAsset(hatAsset, hatAssetPath);
            }
            SerializedObject hatSO = new SerializedObject(hatAsset);
            hatSO.FindProperty("displaySprite").objectReferenceValue = spriteMap[7];
            hatSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(hatAsset);

            // --- Update or Create Outfit asset ---
            // The Outfit asset uses the torso sprite (index 4) for its display.
            string outfitAssetPath = Path.Combine(OUTFIT_PATH, textureName + " Outfit.asset");
            Outfit outfitAsset = AssetDatabase.LoadAssetAtPath<Outfit>(outfitAssetPath);
            if (outfitAsset == null)
            {
                outfitAsset = ScriptableObject.CreateInstance<Outfit>();
                AssetDatabase.CreateAsset(outfitAsset, outfitAssetPath);
            }
            SerializedObject outfitSO = new SerializedObject(outfitAsset);
            outfitSO.FindProperty("displaySprite").objectReferenceValue = spriteMap[4];
            outfitSO.FindProperty("torsoSprite").objectReferenceValue = spriteMap[4];
            outfitSO.FindProperty("leftArmSprite").objectReferenceValue = spriteMap[2];
            outfitSO.FindProperty("rightArmSprite").objectReferenceValue = spriteMap[3];
            outfitSO.FindProperty("leftLegSprite").objectReferenceValue = spriteMap[0];
            outfitSO.FindProperty("rightLegSprite").objectReferenceValue = spriteMap[1];
            outfitSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(outfitAsset);

            Debug.Log($"Exported appearance assets for {textureName}");
        }

        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Ensures that the given folder exists in the AssetDatabase.
    /// </summary>
    private static void EnsureFolder(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            // Split the folder path into its parent folder and new folder name.
            string parentFolder = Path.GetDirectoryName(folderPath).Replace("\\", "/");
            string newFolderName = Path.GetFileName(folderPath);
            AssetDatabase.CreateFolder(parentFolder, newFolderName);
        }
    }
}
#endif
