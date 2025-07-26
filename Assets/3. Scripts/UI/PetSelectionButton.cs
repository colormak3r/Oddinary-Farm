/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/09/2025
 * Last Modified:   07/09/2025 (Khoa)
 * Notes:           <write here>
*/

using ColorMak3r.Utility;
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class PetSelectionButton : MonoBehaviour
{
    [Header("Components")]
    [SerializeField]
    private ImageAnimator animator;
    [SerializeField]
    private TMP_Text petText;
    [SerializeField]
    private Color selectedColor;
    [SerializeField]
    private Color unselectedColor;

    private Action<PetData, PetSelectionButton> OnSelectedCallback;
    private PetData petData;
    public PetData PetData => petData;
    private bool isSelected = false;

    public void Initialze(PetData petData, Action<PetData, PetSelectionButton> OnSelectedCallback)
    {
        this.petData = petData;
        this.OnSelectedCallback = OnSelectedCallback;

        if (PetManager.Main.IsPetCollected(petData.petType))
        {
            animator.SetSprites(petData.collectedSprites);
            petText.text = petData.petType.ToString();
        }
        else
        {
            animator.SetSprites(petData.hiddenSprites);
            petText.text = "???";
        }
    }

    public void OnClick()
    {
        OnSelectedCallback?.Invoke(petData, this);
        if (PetManager.Main.IsPetCollected(petData.petType))
        {

        }
        else
        {
            if (notSelectableCoroutine != null) StopCoroutine(notSelectableCoroutine);
            notSelectableCoroutine = StartCoroutine(NotSelectableCoroutine());
        }
    }

    private Coroutine notSelectableCoroutine;
    private IEnumerator NotSelectableCoroutine()
    {
        yield return transform.PopCoroutine(Vector3.one, Vector3.one * 1.1f, 0.1f);
        yield return transform.PopCoroutine(Vector3.one * 1.1f, Vector3.one, 0.1f);
    }

    public void SetSelected(bool isSelected)
    {
        var previousSelected = this.isSelected;
        this.isSelected = isSelected;
        if (isSelected)
        {
            if (!previousSelected)
            {
                if (gameObject.activeInHierarchy)
                    StartCoroutine(transform.PopCoroutine(Vector3.one, Vector3.one * 1.1f, 0.2f));
                else
                    transform.localScale = Vector3.one * 1.1f;

                petText.color = selectedColor;
            }
        }
        else
        {
            if (previousSelected)
            {
                if (gameObject.activeInHierarchy)
                    StartCoroutine(transform.PopCoroutine(Vector3.one * 1.1f, Vector3.one, 0.2f));
                else
                    transform.localScale = Vector3.one;

                petText.color = unselectedColor;
            }
        }
    }
}
