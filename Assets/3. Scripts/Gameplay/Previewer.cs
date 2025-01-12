using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[ExecuteAlways]
public class Previewer : MonoBehaviour
{
    public static Previewer Main;

    [Header("Settings")]
    [SerializeField]
    private bool showInEditor = true;
    [SerializeField]
    private float speed = 20;
    [SerializeField]
    private Vector2 borderDefaultSize = new Vector2(1, 1);
    [SerializeField]
    private SpriteRenderer borderRenderer;
    [SerializeField]
    private SpriteRenderer backgroundRenderer;
    [SerializeField]
    private SpriteRenderer iconRenderer;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);

        Show(false);
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying)
            Show(showInEditor);
    }
#endif

    public void Show(bool value)
    {
        borderRenderer.enabled = value;
        iconRenderer.enabled = value;
        backgroundRenderer.enabled = value;
    }

    private Vector2 destination;
    public void MoveTo(Vector2 position)
    {
        destination = position;
    }

    private void FixedUpdate()
    {
        transform.position = Vector2.Lerp(transform.position, destination, Time.fixedDeltaTime * speed);
    }

    public void SetIcon(Sprite icon)
    {
        iconRenderer.sprite = icon;
    }

    public void SetIconOffset(Vector2 offset)
    {
        iconRenderer.transform.localPosition = offset;
    }

    public void SetSize(Vector2 size)
    {
        borderRenderer.size = size + Vector2.one / 2f;
        backgroundRenderer.size = size + Vector2.one / 4f;
    }

    public void SetColor(Color color)
    {
        borderRenderer.color = color;
        iconRenderer.color = color;
    }

    [ContextMenu("Reset")]
    private void Reset()
    {
        transform.position = Vector2.zero;
        SetSize(borderDefaultSize);
    }
}
