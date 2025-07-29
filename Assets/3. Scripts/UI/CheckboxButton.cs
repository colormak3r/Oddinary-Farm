/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/21/2025
 * Last Modified:   07/21/2025 (Khoa)
 * Notes:           <write here>
*/

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[ExecuteAlways]
public class CheckboxButton : MonoBehaviour
{
    [Header("Checkbox Settings")]
    [SerializeField]
    private Image checkmarkImage;
    [Space]
    [SerializeField]
    private UnityEvent<bool> onCheckedChanged;

    [Header("Checkbox Debugs")]
    [SerializeField]
    private bool isChecked;
    public bool IsChecked
    {
        get => isChecked;
        set
        {
            isChecked = value;
            checkmarkImage.gameObject.SetActive(isChecked);
            onCheckedChanged?.Invoke(isChecked);
        }
    }

    public void OnCheckBoxClicked()
    {
        AudioManager.Main.PlayClickSound();
    }

    private void Awake()
    {
        IsChecked = isChecked; // Initialize checkbox state
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (checkmarkImage == null) return;
        IsChecked = isChecked; // Ensure checkbox state is updated in the editor
    }
#endif
}