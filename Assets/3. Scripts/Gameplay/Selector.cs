using UnityEngine;

[ExecuteAlways]
public class Selector : MonoBehaviour
{
    public static Selector Main;

    [Header("Settings")]
    [SerializeField]
    private bool showInEditor;
    [SerializeField]
    private Vector2 defaultSize = new Vector2(3, 3);

    private SpriteRenderer spriteRenderer;
    private bool testMode;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.enabled = false;
    }

    private void Update()
    {
        if (!testMode && !Application.isPlaying)
            spriteRenderer.enabled = showInEditor;
    }

    [ContextMenu("Reset")]
    public void Reset()
    {
        testMode = false;
        transform.position = Vector2.zero;
        spriteRenderer.size = defaultSize;
    }

    public void Test(Vector2 position, Vector2 size)
    {
        testMode = true;
        Select(position, size);
    }

    public void MoveTo(Vector2 position, bool show = true)
    {
        transform.position = position;
        spriteRenderer.enabled = show;
    }

    public void Show(bool show)
    {
        spriteRenderer.enabled = show;
    }

    public void SetSelectorSize(Vector2 size)
    {
        spriteRenderer.size = size;
    }

    public void Select(Vector2 position, Vector2 size)
    {
        SetSelectorSize(size);
        MoveTo(position);
    }

    public void Select(GameObject gameObject)
    {
        if (gameObject.TryGetComponent(out SelectorModifier modifier))
        {
            Select(modifier.Position, modifier.Size);
        }
        else
        {
            Select(gameObject.transform.position, defaultSize);
        }
    }
}
