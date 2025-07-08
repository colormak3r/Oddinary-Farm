using System.Collections;
using UnityEngine;

[ExecuteAlways]
public class Selector : MonoBehaviour
{
    public static Selector Main;

    [Header("Selector Settings")]
    [SerializeField]
    private bool showInEditor;
    [SerializeField]
    private Vector2 defaultSize = new Vector2(3, 3);
    [SerializeField]
    private SpriteRenderer borderRenderer;
    [SerializeField]
    private SpriteRenderer buttonRenderer;

    [Header("Fill Settings")]
    [SerializeField]
    private Transform fillTransform;
    [SerializeField]
    private float fill0Position = -0.875f;
    [SerializeField]
    private float fill1Position = -0.125f;
    /*[SerializeField]
    private Color fillingColor = Color.yellow;
    [SerializeField]
    private Color finishedColor = Color.green;*/

    [Header("Debugs")]
    [SerializeField]
    private float currentFill = 0f;
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
        buttonRenderer.transform.localPosition = new Vector3(0, 2 + (size.y - 3) * 0.5f, 0);
    }

    public void Select(Vector2 position, Vector2 size)
    {
        SetSelectorSize(size);
        MoveTo(position);
        fillTransform.localPosition = new Vector3(0, fill0Position, 0);
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

    public IEnumerator AnimateFill(float duration, float startPercent = 0.1f)
    {
        float time = duration * startPercent;

        // Precompute eased start value
        float easedStartT = 1f - Mathf.Pow(1f - startPercent, 2f);
        float startY = Mathf.Lerp(fill0Position, fill1Position, easedStartT);

        Vector3 currentPos = fillTransform.localPosition;
        currentPos.y = startY;
        fillTransform.localPosition = currentPos;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            // Ease-out interpolation
            float easedT = 1f - Mathf.Pow(1f - t, 2f);

            float newY = Mathf.Lerp(fill0Position, fill1Position, easedT);
            currentPos = fillTransform.localPosition;
            currentPos.y = newY;
            fillTransform.localPosition = currentPos;

            yield return null;
        }

        // Final correction
        currentPos = fillTransform.localPosition;
        currentPos.y = fill1Position;
        fillTransform.localPosition = currentPos;
    }

    public void SetCurrentFill(float fill)
    {
        currentFill = fill;
        if (fillTransform != null)
        {
            Vector3 pos = fillTransform.localPosition;
            pos.y = Mathf.Lerp(fill0Position, fill1Position, currentFill);
            fillTransform.localPosition = pos;
        }
    }
}
