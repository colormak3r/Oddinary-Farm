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

    [SerializeField]
    private LayerMask animalLayer;
    public LayerMask AnimalLayer => animalLayer;

    [SerializeField]
    private LayerMask playerLayer;
    public LayerMask PlayerLayer => playerLayer;

    [SerializeField]
    private LayerMask animalFeedLayer;
    public LayerMask AnimalFeedLayer => animalFeedLayer;

    [SerializeField]
    private LayerMask resourceLayer;
    public LayerMask ResourceLayer => resourceLayer;

    [SerializeField]
    private LayerMask damageableLayer;
    public LayerMask DamageableLayer => damageableLayer;
}
