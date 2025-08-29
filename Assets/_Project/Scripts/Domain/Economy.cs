using System;
using UnityEngine;

public class Economy
{
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