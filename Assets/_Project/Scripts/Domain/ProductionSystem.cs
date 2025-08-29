using UnityEngine;

public class ProductionSystem
{
    public int ApplyTick(GameState state, double dtSeconds)
    {
        double produced = state.followersPerSecondCache * dtSeconds;
        state.productionFraction += produced;
        int whole = (int)state.productionFraction;
        if (whole > 0)
        {
            state.productionFraction -= whole;
            state.followersTotal += whole;
            state.followersLifetime += whole;
        }
        return whole;
    }
}