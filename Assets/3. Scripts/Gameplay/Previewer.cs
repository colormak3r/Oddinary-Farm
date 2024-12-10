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
    private Vector2 borderDefaultSize = new Vector2(3, 3);
    [SerializeField]
    private Vector2 backgroundDefaultSize = new Vector2(1, 1);
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

    public void SetSize(int size = 1)
    {
        size -= 1;
        var scale = Vector2.one * size * 2;
        borderRenderer.size = borderDefaultSize + scale;
        backgroundRenderer.size = backgroundDefaultSize + scale;
    }

    public void SetColor(Color color)
    {
        borderRenderer.color = color;
        iconRenderer.color = color;
    }
}
