using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColorMak3r.UI
{
    public class TabButton : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField]
        private Sprite selectedSprite;
        [SerializeField]
        private Sprite unselectedSprite;

        private Image buttonImage;
        private TMP_Text buttonText;

        private bool isSelected = false;
        private int id = 0;
        private TutorialUI tutorialUI;

        public void Initialize(string text, int id, TutorialUI tutorialUI)
        {
            buttonImage = GetComponent<Image>();
            buttonText = GetComponentInChildren<TMP_Text>();

            this.tutorialUI = tutorialUI;
            buttonText.text = text;
            this.id = id;
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            buttonImage.sprite = selected ? selectedSprite : unselectedSprite;
        }

        public void OnClick()
        {
            tutorialUI.OnTabButton(id);
        }
    }
}
