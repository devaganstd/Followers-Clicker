using UnityEngine;
using System;
using System.Collections.Generic;


[System.Serializable]
public class GameState
{
    public int version = 1;
    [Header("Statistics")]
    public int followersLifetime; // total followers earned over lifetime
    public int followersTotal; // current available followers
    public long clickCountLifetime;

    [Header("Shop Items")]
    public List<ShopItemState> shopItems = new List<ShopItemState>();
    public List<Item> items = new();
    
    // Active quests
    public List<Quest> quests = new();

    // Completition count for scaling quest difficulty
    public int completedClickQuests;
    public int completedProductionQuests;
    public int completedPurchaseQuests;

    public long lastManualSaveUnixMs;
    public long lastAutosaveUnixMs;

    public double followersPerSecondCache;
    public double productionFraction; // fractional followers from production
    public double followersPerClickCache; // followers per click
}

[System.Serializable]
public class Item
{
    public string id;
    public int quantity;
    public double baseProduction;  // production per second per unit
    public float baseCost;
    public float costScaling;
}

[System.Serializable]
public class Quest
{
    public string id;
    public QuestType type;
    public int target;
    public int progress;
    public int rewardFollowers;
    public QuestStatus status;
    public long expiresAtUnixMs;
    public long expiredDisplayUntilUnixMs; // 0 while active / unused
}
public enum QuestType
{
    Click,
    Production,
    Purchase
}

public enum QuestStatus
{
    Active,
    Completed,
    Expired
}

[Serializable]
public class ShopItemState
{
    public string itemId;
    public int quantity = 0;
    
    public ShopItemState(string id)
    {
        itemId = id;
        quantity = 0;
    }
}