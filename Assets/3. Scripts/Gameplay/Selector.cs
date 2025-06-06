using UnityEngine;

/// <summary>
/// The selector is an indicator allowing the player to 
/// interact with something close by
/// </summary>
[ExecuteAlways]     // Run the script's logic in play mode and edit mode
public class Selector : MonoBehaviour
{
    public static Selector Main;        // Singleton

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
        // Handle singleton
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        Show(false);        // Do not show selector by default
    }

    // Show in the editor while game is not playing 
    private void Update()
    {
        if (!testMode && !Application.isPlaying) 
            Show(showInEditor);
    }

    // Right clicking on this component in the inspector will bring up a "Reset" option in the context menu
    [ContextMenu("Reset")]      // Adding this to the Reset method does not automatically reset the component when adding a new component
    public void Reset()         // Reset component to default values
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

    // Move the selector to a desired location
    public void MoveTo(Vector2 position, bool show = true)
    {
        transform.position = position;
        Show(show);
    }

    // Show the selector outline and button
    public void Show(bool show)
    {
        borderRenderer.enabled = show;
        buttonRenderer.enabled = show;
    }

    // Scale the selector boarder to a desired size
    public void SetSelectorSize(Vector2 size)
    {
        borderRenderer.size = size;
        buttonRenderer.transform.localEulerAngles = new Vector3(0, 2 + (size.y - 3) * 0.5f, 0);
    }

    // Configure the selector position and size
    public void Select(Vector2 position, Vector2 size)
    {
        SetSelectorSize(size);
        MoveTo(position);
    }

    // Select a gameobject with the selector
    public void Select(GameObject gameObject)
    {
        if (gameObject.TryGetComponent(out SelectorModifier modifier))
        {
            if (modifier.CanBeSelected)
                Select(modifier.Position, modifier.Size);
        }
        else
        {
            Select(gameObject.transform.position, defaultSize);
        }
    }
}
