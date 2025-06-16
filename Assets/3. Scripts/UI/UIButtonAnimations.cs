using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonAnimations : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private string hoverTrigger = "Hover";
    [SerializeField] private string clickTrigger = "Click";
    [SerializeField] private string idleTrigger = "Idle";
    [SerializeField] private Animator animator;

    

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (animator != null)
        {
            animator.ResetTrigger(clickTrigger);
            animator.SetTrigger(hoverTrigger);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (animator != null)
        {
            animator.SetTrigger(clickTrigger);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (animator != null)
        {
            animator.ResetTrigger(hoverTrigger);
            animator.SetTrigger(idleTrigger);
        }
    }
}
