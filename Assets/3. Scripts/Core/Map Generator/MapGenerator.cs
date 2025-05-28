using Unity.Netcode;
using UnityEngine;

public abstract class MapGenerator : NetworkBehaviour
{
    private static Vector2Int MOCK_MAP_SIZE = new Vector2Int(400, 400);

    [Header("Map Generator Preview")]
    [SerializeField]
    protected Texture2D mapTexture;
    public Texture2D MapTexture { get => mapTexture; }

    [SerializeField]
    protected ColorMapping[] mapColors =
    {
        new ColorMapping(new Color32(40,204,223,255),0.4f),
        new ColorMapping(new Color32(238,161,96,255),0.5f),
        new ColorMapping(new Color32(122,68,74,255),0.55f),
        new ColorMapping(new Color32(113,170,52,255),0.85f),
        new ColorMapping(new Color32(160,147,142,255),1.0f)
    };

    protected Offset2DArray<float> rawMap;
    public Offset2DArray<float> RawMap { get => rawMap; }

    public abstract void GenerateMap(Vector2Int mapSize);

    [ContextMenu("Generate Preview")]
    public void GeneratePreview() => GenerateMap(MOCK_MAP_SIZE);

    public virtual void RandomizeMap() { }
}
