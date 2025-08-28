using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private GameEngine gameEngine;
    [Header("Text UI Elements")]
    [SerializeField] private TextMeshProUGUI followersText; // text total followers available / current followers
    [SerializeField] private TextMeshProUGUI rateText; // text followers per second
    [SerializeField] private TextMeshProUGUI clickValueText; // text followers per click
    [SerializeField] private TextMeshProUGUI totalClicksText; // text total clicks
    [SerializeField] private TextMeshProUGUI totalFollowersText; // text total followers earned lifetime
    [Header("Buttons & Containers")]
    [SerializeField] private Button clickButton;
    [SerializeField] private Button saveButton;
    [Header("Quest UI Elements")]
    [SerializeField] private Transform questsContainer;
    [SerializeField] private QuestItem questItemPrefab;

    private void Start()
    {
        gameEngine.OnStateUpdated += Refresh;

        clickButton.onClick.AddListener(() => gameEngine.OnClick());
        saveButton.onClick.AddListener(() => gameEngine.ManualSave());
    }

    private void Refresh(GameState state)
    {
        followersText.text = $"Followers: {state.followersTotal:F0}";
        rateText.text = $"Production : + {state.followersPerSecondCache:F2}/s";
        clickValueText.text = $"Click : + {state.followersPerClickCache:F0}/click";
        totalClicksText.text = $"Lifetime Clicks : {state.clickCountLifetime}";
        totalFollowersText.text = $"Lifefime Followers : {state.followersLifetime}";

        // Quest list refresh (destroy & rebuild)
        foreach (Transform c in questsContainer)
            Destroy(c.gameObject);

        //foreach (var q in state.quests.Where(q => q.status == QuestStatus.Active || q.status == QuestStatus.Completed))
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        // Show Active, Completed, and Expired (if within retention time)
        var visibleQuests = state.quests.Where(q =>
            q.status == QuestStatus.Active
            || q.status == QuestStatus.Completed
            || (q.status == QuestStatus.Expired
                && q.expiredDisplayUntilUnixMs > 0
                && nowMs < q.expiredDisplayUntilUnixMs)
        );

        foreach (var q in visibleQuests)
        {
            var go = Instantiate(questItemPrefab, questsContainer);
            var item = go.GetComponent<QuestItem>();
            item.Bind(q, gameEngine);

            item.WireClaim(() => gameEngine.ClaimQuest(q.id)); // pass claim action
        }
    }
}