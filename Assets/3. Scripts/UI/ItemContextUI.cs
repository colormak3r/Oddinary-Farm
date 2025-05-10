using TMPro;
using UnityEngine;

public class ItemContextUI : UIBehaviour
{
    public static ItemContextUI Main { get; private set; }

    [Header("Item Context")]
    [SerializeField]
    private TMP_Text itemContextText;

    private void Awake()
    {
        if (Main == null)
        {
            Main = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetItemContext(string context)
    {
        itemContextText.text = context;
    }
}
