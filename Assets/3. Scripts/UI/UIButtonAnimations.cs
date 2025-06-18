using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using TMPro;
public class UIButtonAnimations : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler

{
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Hover Variables")]
    [SerializeField] private Color hoverColor = Color.white;
    [SerializeField] private Color hoverTextColor = Color.white;
    [SerializeField] private Vector3 hoverScale = Vector3.one * 1.05f;
    [SerializeField] private Vector3 hoverRotation = new Vector3(0, 0, -1.5f);
    
    [Header("Click Variables")]
    [SerializeField] private Color clickColor = Color.white;
    [SerializeField] private Color clickTextColor = Color.white;
    [SerializeField] private Vector3 clickScale = Vector3.one * 0.9f;
    [SerializeField] private Vector3 clickRotation = Vector3.zero;
    [SerializeField] private float clickDuration = 0.1f;
    //after clicking -> returns to different state
    [SerializeField] private Color postClickColor = Color.white;
    [SerializeField] private Color postClickTextColor = Color.white;
    [SerializeField] private Vector3 postClickScale = Vector3.one * 1.05f;
    [SerializeField] private Vector3 postClickRotation = new Vector3(0, 0, -1.5f);
    
    [Header("Button Components")]
    [SerializeField] private Image targetImage;
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Color originalColor = Color.white;
    [SerializeField] private Color originalTextColor = Color.white;
    [SerializeField] private Vector3 originalScale = Vector3.one;
    [SerializeField] private Vector3 originalRotation = Vector3.zero;
    
    private Coroutine currentAnimation;

    private void Awake()
    {
        if (targetImage != null)
            originalColor = targetImage.color;
        
        if (targetText != null)
            originalTextColor = targetText.color;
        
        if (targetTransform != null)
        {
            originalScale = targetTransform.localScale;
            originalRotation = targetTransform.localEulerAngles;
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        AnimateToState(hoverColor, hoverTextColor, hoverScale, hoverRotation, animationDuration);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateToState(originalColor, originalTextColor, originalScale, originalRotation, animationDuration);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        StartCoroutine(ClickAnimation());
    }

    private IEnumerator ClickAnimation()
    {
        AnimateToState(clickColor, clickTextColor, clickScale, clickRotation, clickDuration);
        yield return new WaitForSeconds(clickDuration);

        //1) returns to hover state after clicking
        //AnimateToState(hoverColor, hoverTextColor, hoverScale, hoverRotation, clickDuration);
        //2) returns to original state after clicking
        //AnimateToState(originalColor, originalTextColor, originalScale, originalRotation, animationDuration);
        AnimateToState(postClickColor, postClickTextColor, postClickScale, postClickRotation, animationDuration);
    }
    
    private void AnimateToState(Color targetColor, Color targetTextColor, Vector3 targetScale, Vector3 targetRotation, float duration)
    {
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);
            
        currentAnimation = StartCoroutine(AnimateToStateCoroutine(targetColor, targetTextColor, targetScale, targetRotation, duration));
    }

    private IEnumerator AnimateToStateCoroutine(Color targetColor, Color targetTextColor, Vector3 targetScale, Vector3 targetRotation, float duration)
    {
        Color startColor = targetImage != null ? targetImage.color : Color.white;
        Color startTextColor = targetText != null ? targetText.color : Color.white;
        Vector3 startScale = targetTransform.localScale;
        Vector3 startRotation = targetTransform.localEulerAngles;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = animationCurve.Evaluate(elapsed / duration);

            if (targetImage != null)
                targetImage.color = Color.Lerp(startColor, targetColor, t);

            if (targetText != null)
                targetText.color = Color.Lerp(startTextColor, targetTextColor, t);

            targetTransform.localScale = Vector3.Lerp(startScale, targetScale, t);

            //targetTransform.localEulerAngles = Vector3.Lerp(startRotation, targetRotation, t);
            //new approach avoids clockwise rotation
            targetTransform.localRotation = Quaternion.Lerp(
                Quaternion.Euler(startRotation),
                Quaternion.Euler(targetRotation),
                t
            );

            yield return null;
        }

        if (targetImage != null)
            targetImage.color = targetColor;

        if (targetText != null)
            targetText.color = targetTextColor;

        targetTransform.localScale = targetScale;
        //targetTransform.localEulerAngles = targetRotation;
        targetTransform.localRotation = Quaternion.Euler(targetRotation);
    }
}
