using UnityEngine;

[CreateAssetMenu(fileName = "ShopItemData", menuName = "Game/Shop Item")]
public class ShopItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemId;
    public string displayName;
    public Sprite icon;
    
    [Header("Item Type")]
    public ShopItemType itemType;
    
    [Header("Cost Settings")]
    public double baseCost = 100;
    public double costMultiplier = 1.25; // 1.25 for producers, 1.15 for click upgrades
    
    [Header("Production Settings (for Producers only)")]
    public double baseProduction = 1;
    
    [Header("Click Upgrade Settings (for Click Upgrades only)")]
    public double baseClickBonus = 1;
    
    [Header("Status")]
    public bool isLocked = false;
    
    [Header("UI Text")]
    [TextArea(2, 4)]
    public string description;
}

public enum ShopItemType
{
    Producer,       // Passive production
    ClickUpgrade,   // Click bonus
    Locked          // Coming Soon
}
