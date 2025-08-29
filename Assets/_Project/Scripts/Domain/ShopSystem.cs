using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class ShopSystem
{
    [Header("Shop Item Database")]
    [SerializeField] private ShopItemData[] shopItemDatabase;
    
    private readonly ITimeSource _timeSource;
    private readonly QuestManager _questManager;
    private readonly SaveScheduler _saveScheduler;
    private GameState _gameState;

    public ShopSystem(ITimeSource timeSource, QuestManager questManager, SaveScheduler saveScheduler, GameState state)
    {
        _timeSource = timeSource;
        _questManager = questManager;
        _saveScheduler = saveScheduler;
        _gameState = state;

        LoadShopItemsFromResources();
    }
    private void LoadShopItemsFromResources()
    {
        shopItemDatabase = Resources.LoadAll<ShopItemData>("ShopItems");

        if (_gameState.shopItems == null || _gameState.shopItems.Count == 0)
        {
            _gameState.shopItems = new List<ShopItemState>();

            foreach (var itemData in shopItemDatabase)
            {
                _gameState.shopItems.Add(new ShopItemState(itemData.itemId));
            }
        }
    }
    public void InitializeShopItems(GameState state)
    {
        // Load shop database from Resources if not assigned
        if (shopItemDatabase == null || shopItemDatabase.Length == 0)
        {
            shopItemDatabase = Resources.LoadAll<ShopItemData>("ShopItems");
        }
        
        // Initialize shop items if empty
        if (state.shopItems == null || state.shopItems.Count == 0)
        {
            state.shopItems = new List<ShopItemState>();
            
            foreach (var itemData in shopItemDatabase)
            {
                state.shopItems.Add(new ShopItemState(itemData.itemId));
            }
        }
        
        // Ensure all items from database exist in save
        foreach (var itemData in shopItemDatabase)
        {
            if (!state.shopItems.Any(item => item.itemId == itemData.itemId))
            {
                state.shopItems.Add(new ShopItemState(itemData.itemId));
            }
        }
    }
    
    public double CalculateNextCost(string itemId, GameState state)
    {
        var itemData = GetItemData(itemId);
        var itemState = GetItemState(itemId, state);
        
        if (itemData == null || itemState == null) return 0;
        
        return itemData.baseCost * Math.Pow(itemData.costMultiplier, itemState.quantity);
    }
    
    public bool CanAfford(string itemId, GameState state)
    {
        var itemData = GetItemData(itemId);
        if (itemData == null || itemData.isLocked || itemData.itemType == ShopItemType.Locked)
            return false;
            
        double cost = CalculateNextCost(itemId, state);
        return state.followersTotal >= cost;
    }
    
    public PurchaseResult TryPurchase(string itemId, GameState state)
    {
        var itemData = GetItemData(itemId);
        var itemState = GetItemState(itemId, state);
        
        if (itemData == null)
            return new PurchaseResult { success = false, message = "Item not found" };
            
        if (itemData.isLocked || itemData.itemType == ShopItemType.Locked)
            return new PurchaseResult { success = false, message = "Item is locked" };
            
        int cost = (int)CalculateNextCost(itemId, state);
        
        if (state.followersTotal < cost)
            return new PurchaseResult { success = false, message = "Not enough followers" };
        Debug.Log($"Item Purchase {state.followersTotal} with {cost}");
        // Execute purchase
        state.followersTotal -= cost;
        itemState.quantity++;
        
        // Update statistics
        //state.totalFollowersSpent += cost; 
        //state.totalPurchases++;
        
        // Recompute derived values
        RecomputeDerivedValues(state);
        
        // Notify quest system
        NotifyQuestProgress(state, itemId, cost);
        
        // Mark for save
        _saveScheduler.MarkDirty();
        
        return new PurchaseResult 
        { 
            success = true, 
            message = $"Purchased {itemData.displayName}",
            costPaid = cost
        };
    }
    
    public void RecomputeDerivedValues(GameState state)
    {
        // Recompute followers per second
        state.followersPerSecondCache = 0;
        foreach (var itemState in state.shopItems)
        {
            var itemData = GetItemData(itemState.itemId);
            if (itemData != null && itemData.itemType == ShopItemType.Producer)
            {
                state.followersPerSecondCache += itemData.baseProduction * itemState.quantity;
            }
        }
        
        // Recompute click bonus
        state.followersPerClickCache = 1; // Base click value
        foreach (var itemState in state.shopItems)
        {
            var itemData = GetItemData(itemState.itemId);
            if (itemData != null && itemData.itemType == ShopItemType.ClickUpgrade && itemState.quantity > 0)
            {
                // Each purchase doubles the bonus for that upgrade line
                state.followersPerClickCache += itemData.baseClickBonus * Math.Pow(2, itemState.quantity);
            }
        }
    }
    
    private void NotifyQuestProgress(GameState state, string itemId, double cost)
    {
        // Notify various quest types
        _questManager.NotifyPurchase(state, 1);
        //_questManager.NotifyProgress(state, "purchase_any", 1);
        //_questManager.NotifyProgress(state, $"purchase_{itemId}", 1);
        //_questManager.NotifyProgress(state, "spend_followers", (int)cost);
        //_questManager.NotifyProgress(state, "reach_production", (int)state.followersPerSecond);
    }
    
    public ShopItemData GetItemData(string itemId)
    {
        return shopItemDatabase?.FirstOrDefault(item => item.itemId == itemId);
    }
    
    public ShopItemState GetItemState(string itemId, GameState state)
    {
        return state.shopItems?.FirstOrDefault(item => item.itemId == itemId);
    }
    
    public ShopItemData[] GetAllItems()
    {
        return shopItemDatabase;
    }
}

[Serializable]
public class PurchaseResult
{
    public bool success;
    public string message;
    public double costPaid;
}
