using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

[ExecuteAlways]
public class Selector : MonoBehaviour
{
    public static Selector main;

    [Header("Settings")]
    [SerializeField]
    private bool showInEditor;
    [SerializeField]
    private Vector2 defaultSize = new Vector2(3, 3);

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (main == null)
            main = this;
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
        if (!Application.isPlaying)
            spriteRenderer.enabled = showInEditor;
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
