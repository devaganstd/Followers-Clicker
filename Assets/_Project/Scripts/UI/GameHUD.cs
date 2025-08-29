using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private GameEngine gameEngine;
    [Header("Text UI Element Monitor")]
    [SerializeField] private TextMeshProUGUI followersText; // text total followers available / current followers
    [SerializeField] private TextMeshProUGUI rateText; // text followers per second
    [SerializeField] private TextMeshProUGUI clickValueText; // text followers per click
    [SerializeField] private TextMeshProUGUI totalClicksText; // text total clicks
    [SerializeField] private TextMeshProUGUI totalFollowersText; // text total followers earned lifetime
    [SerializeField] private TextMeshProUGUI totalFollowersTextShop; // text total followers earned lifetime
    [Header("Buttons & Containers")]
    [SerializeField] private Button clickButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button resetButton;
    [Header("Quest UI Elements")]
    [SerializeField] private Transform questsContainer;
    [SerializeField] private QuestItem questItemPrefab;
    [Header("Shop Panel")]
    [SerializeField] private Transform shopItemParent;
    [SerializeField] private GameObject shopItemPrefab;


    private ShopItemUI[] shopItemUIs;

    private void Start()
    {
        gameEngine.OnStateUpdated += Refresh;

        clickButton.onClick.AddListener(() => gameEngine.OnClick());
        saveButton.onClick.AddListener(() => gameEngine.ManualSave());
        loadButton.onClick.AddListener(() => gameEngine.ManualLoad());
        resetButton.onClick.AddListener(() => gameEngine.ResetGame());
    }

    private void Refresh(GameState state)
    {
        RefCurrency(state);
        RefQuest(state);
        UpdateShopDisplay(state, gameEngine.GetShopSystem());
    }

    private void RefCurrency(GameState state)
    {
        followersText.text = $"Followers: {state.followersTotal:F0}";
        rateText.text = $"Production : + {state.followersPerSecondCache:F2}/s";
        clickValueText.text = $"Click : + {state.followersPerClickCache:F0}/click";
        totalClicksText.text = $"Lifetime Clicks : {state.clickCountLifetime}";
        totalFollowersText.text = $"Lifefime Followers : {state.followersLifetime}";
        totalFollowersTextShop.text = $"followers : {state.followersTotal}";
    }

    private void RefQuest(GameState state)
    {
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

    public void UpdateShopDisplay(GameState gameState, ShopSystem shopSystem)
    {
        Debug.Log($"UpdateShopDisplay called. Items count: {shopSystem.GetAllItems()?.Length ?? 0}");
        // Initialize shop UI if needed
        if (shopItemUIs == null)
        {
            InitializeShopUI(shopSystem);
        }
        
        var allItems = shopSystem.GetAllItems();
        for (int i = 0; i < shopItemUIs.Length && i < allItems.Length; i++)
        {
            var itemData = allItems[i];
            var itemState = shopSystem.GetItemState(itemData.itemId, gameState);
            var itemUI = shopItemUIs[i];

            UpdateShopItemUI(itemUI, itemData, itemState, gameState, shopSystem);
        }
        //Debug.Log($"[GameHUD] Updated {shopItemUIs.Length} shop items. {shopSystem}");
    }

    private void InitializeShopUI(ShopSystem shopSystem)
    {
        var allItems = shopSystem.GetAllItems();
        shopItemUIs = new ShopItemUI[allItems.Length];

        for (int i = 0; i < allItems.Length; i++)
        {
            var itemData = allItems[i];
            var itemObj = Instantiate(shopItemPrefab, shopItemParent);
            var itemUI = itemObj.GetComponent<ShopItemUI>();

            if (itemUI != null)
            {
                itemUI.Initialize(itemData.itemId);
                shopItemUIs[i] = itemUI;
            }
        }
    }

    private void UpdateShopItemUI(ShopItemUI itemUI, ShopItemData itemData, ShopItemState itemState, GameState gameState, ShopSystem shopSystem)
    {
        if (itemUI == null || itemData == null || itemState == null) return;

        // Basic info
        itemUI.SetName(itemData.displayName);
        itemUI.SetQuantity(itemState.quantity);
        itemUI.SetIcon(itemData.icon);

        // Cost and affordability
        if (itemData.isLocked || itemData.itemType == ShopItemType.Locked)
        {
            itemUI.SetCost("Coming Soon");
            itemUI.SetInteractable(false);
        }
        else
        {
            double cost = shopSystem.CalculateNextCost(itemData.itemId, gameState);
            itemUI.SetCost(FormatNumber(cost));
            itemUI.SetInteractable(shopSystem.CanAfford(itemData.itemId, gameState));
        }

        // Production info
        if (itemData.itemType == ShopItemType.Producer)
        {
            double production = itemData.baseProduction * itemState.quantity;
            itemUI.SetProductionInfo($"{FormatNumber(production)}/s");
        }
        else if (itemData.itemType == ShopItemType.ClickUpgrade)
        {
            double bonus = itemState.quantity > 0 ? itemData.baseClickBonus * System.Math.Pow(2, itemState.quantity) : 0;
            itemUI.SetProductionInfo($"+{FormatNumber(bonus)} click");
        }

        itemUI.SetDescription(itemData.description);
    }

    private string FormatNumber(double value)
    {
        if (value >= 1_000_000_000_000) return (value / 1_000_000_000_000).ToString("0.##") + "T";
        if (value >= 1_000_000_000) return (value / 1_000_000_000).ToString("0.##") + "B";
        if (value >= 1_000_000) return (value / 1_000_000).ToString("0.##") + "M";
        if (value >= 1_000) return (value / 1_000).ToString("0.##") + "K";
        return value.ToString("0");
    }
}