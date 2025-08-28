using System;
using UnityEngine;

public class Economy
{
    public bool CanAfford(GameState state, string itemId)
    {
        var item = state.items.Find(i => i.id == itemId);
        if (item == null) return false;

        int cost = ComputeCurrentCost(item);
        return state.followersTotal >= cost;
    }

    public bool ApplyPurchase(GameState state, string itemId)
    {
        var item = state.items.Find(i => i.id == itemId);
        if (item == null) return false;

        int cost = ComputeCurrentCost(item);
        if (state.followersTotal < cost) return false;

        state.followersTotal -= cost;
        item.quantity += 1;
        return true;
    }

    public int ComputeCurrentCost(Item item)
    {
        // Classic incremental formula: cost = baseCost * costScaling^quantity
        return (int)(item.baseCost * Mathf.Pow(item.costScaling, item.quantity));
    }

    public void RecomputeDerived(GameState state)
    {
        double totalRate = 0;
        foreach (var item in state.items)
        {
            totalRate += item.baseProduction * item.quantity;
        }
        state.followersPerSecondCache = totalRate;
    }

    public void GrantReward(GameState state, int followers)
    {
        state.followersTotal += followers;
        state.followersLifetime += followers;
    }

    public void EnsureStarterItems(GameState state)
    {
        if (state.items.Count == 0)
        {
            state.items.Add(new Item
            {
                id = "cursor",
                quantity = 0,
                baseProduction = 1,
                baseCost = 15,
                costScaling = 1.15f
            });
        }
    }
}