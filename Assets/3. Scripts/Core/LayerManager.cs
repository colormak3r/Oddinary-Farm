using UnityEngine;

public class LayerManager : MonoBehaviour
{
    public static LayerManager Main { get; private set; }
    private void Awake()
    {
        if (Main == null)
        {
            Main = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("Layers")]
    [SerializeField]
    private LayerMask structureLayer;
    public LayerMask StructureLayer => structureLayer;

    [SerializeField]
    private LayerMask plantLayer;
    public LayerMask PlantLayer => plantLayer;

    [SerializeField]
    private LayerMask farmPlotLayer;
    public LayerMask FarmPlotLayer => farmPlotLayer;
}
