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
    [SerializeField]
    private SpriteRenderer borderRenderer;
    [SerializeField]
    private SpriteRenderer buttonRenderer;

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
        Show(false);
    }

    private void Update()
    {
        if (!testMode && !Application.isPlaying) Show(showInEditor);
    }

    [ContextMenu("Reset")]
    public void Reset()
    {
        testMode = false;
        transform.position = Vector2.zero;
        borderRenderer.size = defaultSize;
    }

    public void Test(Vector2 position, Vector2 size)
    {
        testMode = true;
        Select(position, size);
    }

    public void MoveTo(Vector2 position, bool show = true)
    {
        transform.position = position;
        Show(show);
    }

    public void Show(bool show)
    {
        borderRenderer.enabled = show;
        buttonRenderer.enabled = show;
    }

    public void SetSelectorSize(Vector2 size)
    {
        borderRenderer.size = size;
        buttonRenderer.transform.localEulerAngles = new Vector3(0, 2 + (size.y - 3) * 0.5f, 0);
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
            if (modifier.CanBeSelected) Select(modifier.Position, modifier.Size);
        }
        else
        {
            Select(gameObject.transform.position, defaultSize);
        }
    }
}
