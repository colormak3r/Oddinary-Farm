/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/22/2025
 * Last Modified:   07/22/2025 (Khoa)
 * Notes:           <write here>
*/

using ColorMak3r.Utility;
using TMPro;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public struct CurseData
{
    public PlayerCurse CurseEffect;
    public string CurseTitle;
    [TextArea(3, 10)]
    public string CurseDescription;
}

public class HypnoFrogUI : UIBehaviour
{
    public static HypnoFrogUI Main { get; private set; }

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

        titleWaveTextEffect = titleText.GetComponent<WaveTextEffect>();
        curseWaveTextEffect = curseText.GetComponent<WaveTextEffect>();
    }

    [Header("Frog UI Settings")]
    [SerializeField]
    private TMP_Text titleText;
    [SerializeField]
    private TMP_Text curseText;
    [SerializeField]
    private CurseData[] curseDatas = new CurseData[]
    {
        new CurseData
        {
            CurseEffect = PlayerCurse.GoldenCarrot,
            CurseTitle = "Golden Carrot Curse",
            CurseDescription = "\"Riches await from the golden carrot, but all other crops shall grant but a single coin.\" - The Hypno Frog proclaims... \r\n\r\nDo you accept this curse?"
        },
        new CurseData
        {
            CurseEffect = PlayerCurse.GlassCannon,
            CurseTitle = "Glass Cannon Curse",
            CurseDescription = "\"You shall move swiftly, strike fiercely, but healing is forbidden - and three blows shall end you.\" - The Hypno Frog degrees...\r\n\r\nDo you accept this curse?"
        },
        new CurseData
        {
            CurseEffect = PlayerCurse.VacationMode,
            CurseTitle = "Vacation Mode Curse",
            CurseDescription = "\"Fortune comes to those who stand still - but move, and you shall lose 1% of your coin purse with every step.\" - The Hypno Frog declares... \r\n\r\nDo you accept this curse?"
        }
    };

    [Header("Frog UI Debugs")]
    [SerializeField]
    private CurseData currentCurseData;

    private WaveTextEffect titleWaveTextEffect;
    private WaveTextEffect curseWaveTextEffect;

    protected override void Start()
    {
        base.Start();
        TimeManager.Main.OnHourChanged.AddListener(HandleOnHourChanged);
        HandleOnHourChanged(TimeManager.Main.CurrentHour);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        TimeManager.Main.OnHourChanged.RemoveListener(HandleOnHourChanged);
    }

    int currentDate_cached = -1;
    private void HandleOnHourChanged(int currentHour)
    {
        if (currentHour == HypnoFrogManager.Main.CurseChangeHour && currentDate_cached != TimeManager.Main.CurrentDate)
        {
            currentDate_cached = TimeManager.Main.CurrentDate;
            currentCurseData = curseDatas.GetRandomElementNot(currentCurseData);
            titleText.text = currentCurseData.CurseTitle;
            curseText.text = currentCurseData.CurseDescription;
        }
    }

    public void OnAcceptClicked()
    {
        var playerObject = NetworkManager.Singleton.LocalClient?.PlayerObject;

        if (playerObject != null && playerObject.TryGetComponent(out PlayerStatus playerStatus))
        {
            if (showDebugs) Debug.Log($"Applying curse: {currentCurseData.CurseEffect} to player: {playerObject.name}");
            playerStatus.SetCurse(currentCurseData.CurseEffect);
        }
        else
        {
            Debug.LogError("Player object or PlayerStatus component not found.");
        }

        Hide();
        AudioManager.Main.PlayClickSound();
    }

    public void OnRefuseClicked()
    {
        Hide();
        AudioManager.Main.PlayClickSound();
    }
}
