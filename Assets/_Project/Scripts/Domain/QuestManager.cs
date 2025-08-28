using System;
using System.Diagnostics;
using System.Linq;
using TMPro;

public class QuestManager
{
    private const long questLifetimeMs = 10_000;      // 5 minutes
    private const long expiredRetentionMs = 2_000;     // Keep expired quest visible for 3 seconds
    private readonly IRandom _random;
    private readonly ITimeSource _time;


    public QuestManager(IRandom random, ITimeSource time)
    {
        _random = random;
        _time = time;
    }
    public long QuestLifetimeMs => questLifetimeMs;
    public long ExpiredRetentionMs => expiredRetentionMs;

    #region InitialQuests

    public void EnsureInitialQuests(GameState state)
    {
        long now = NowMs(); // current time in ms
        EnsureQuestType(state, QuestType.Click, now);
        EnsureQuestType(state, QuestType.Production, now);
        EnsureQuestType(state, QuestType.Purchase, now);
    }

    public void ExpireDue(GameState state)
    {
        long now = NowMs();
        //bool anyExpired = false;

        foreach (var q in state.quests)
        {
            if (q.status == QuestStatus.Active && now >= q.expiresAtUnixMs)
            {
                q.status = QuestStatus.Expired;
                q.expiredDisplayUntilUnixMs = now + ExpiredRetentionMs; // set retention time
            }
        }

        bool removedAny = false;
        var typeNeedingCheck = new System.Collections.Generic.HashSet<QuestType>();

        // Remove old expired quests past their display retention time
        for (int i = state.quests.Count - 1; i >= 0; i--)
        {
            var q = state.quests[i];
            if (q.status == QuestStatus.Expired && q.expiredDisplayUntilUnixMs > 0 && now > q.expiredDisplayUntilUnixMs) // time to remove
            {
                typeNeedingCheck.Add(q.type);
                state.quests.RemoveAt(i);
                removedAny = true;
            }
        }

        if (removedAny)
        {
            // For each type ensure we still have exactly one Active
            EnsureQuestType(state, QuestType.Click, now);
            EnsureQuestType(state, QuestType.Production, now);
            EnsureQuestType(state, QuestType.Purchase, now);

            // Optional pruning
            //state.quests.RemoveAll(x => x.status == QuestStatus.Expired && state.quests.Count > 12);
        }
    }

    // Ensure at least one active quest of the given type
    private void EnsureQuestType(GameState state, QuestType type, long nowMs)
    {
        bool existsActive = state.quests.Any(q => q.type == type && (q.status == QuestStatus.Active || q.status == QuestStatus.Completed));

        if (!existsActive)
        {
            GenerateReplacement(state, type, nowMs);
        }
    }

    // Initial quest parameters
    private (int target, int reward) InitialParams(QuestType type)
    {
        return type switch
        {
            QuestType.Click => (10, 50),
            QuestType.Production => (25, 60),
            QuestType.Purchase => (3, 80),
            _ => (10, 50)
        };
    }
    // Next quest parameters based on completition count
    private (int target, int reward) NextParams(GameState state, QuestType type)
    {
        // Simple scaling based on per-type completion counters
        int completed = type switch
        {
            QuestType.Click => state.completedClickQuests,
            QuestType.Production => state.completedProductionQuests,
            QuestType.Purchase => state.completedPurchaseQuests,
            _ => 0
        };

        // Base target per type
        int baseTarget = type switch
        {
            QuestType.Click => 10,
            QuestType.Production => 25,
            QuestType.Purchase => 3,
            _ => 10
        };

        // Scaling step per completition per type
        int step = type switch
        {
            QuestType.Click => 5,       // additional 5 clicks per completion
            QuestType.Production => 15, // additional 15 production per completion
            QuestType.Purchase => 2,    // additional 2 purchases per completion
            _ => 5
        };

        // Difficulty multiplier per type (not currently used)
        float multiplier = type switch
        {
            QuestType.Click => 1.0f,
            QuestType.Production => 1.2f,
            QuestType.Purchase => 1.5f,
            _ => 1.0f
        };

        // Compute target and reward
        int target = baseTarget + step * completed;
        int reward = (int)(target * 5);
        return (target, reward);
    }

    #endregion

    // Notify click event to quests
    #region NotifyClick

    // Notify click event to quests
    public void NotifyClick(GameState state, int amount)
    {
        if (amount <= 0) return;
        foreach (var q in state.quests)
        {
            if (q.status != QuestStatus.Active) continue;
            if (q.type == QuestType.Click)
            {
                q.progress = Math.Min(q.target, q.progress + amount);
                if (q.progress >= q.target)
                    q.status = QuestStatus.Completed;
            }
        }
    }

    // Notify production event to quests
    public void NotifyProduction(GameState state, int producedAmount)
    {
        if (producedAmount <= 0) return;
        foreach (var q in state.quests)
        {
            if (q.status != QuestStatus.Active) continue;
            if (q.type == QuestType.Production)
            {
                q.progress = Math.Min(q.target, q.progress + producedAmount);
                if (q.progress >= q.target)
                    q.status = QuestStatus.Completed;
            }
        }
    }

    // Notify purchase event to quests
    public void NotifyPurchase(GameState state, int purchaseCount = 1)
    {
        if (purchaseCount <= 0) return;
        foreach (var q in state.quests)
        {
            if (q.status != QuestStatus.Active) continue;
            if (q.type == QuestType.Purchase)
            {
                q.progress = Math.Min(q.target, q.progress + purchaseCount);
                if (q.progress >= q.target)
                    q.status = QuestStatus.Completed;
            }
        }
    }

    #endregion

    public bool Claim(GameState state, string questId, out int reward)
    {
        reward = 0;
        var q = state.quests.FirstOrDefault(x => x.id == questId);
        if (q == null || q.status != QuestStatus.Completed) return false;

        reward = q.rewardFollowers;
        q.status = QuestStatus.Expired;

        // Increment per-type counter
        switch (q.type)
        {
            case QuestType.Click: state.completedClickQuests++; break;
            case QuestType.Production: state.completedProductionQuests++; break;
            case QuestType.Purchase: state.completedPurchaseQuests++; break;
        }

        long now = NowMs();
        GenerateReplacement(state, q.type, now);

        // Prune old expired quests (keep recent ones for inspection, optional)
        state.quests.RemoveAll(old => old.status == QuestStatus.Expired && state.quests.Count > 9);

        return true;
    }
    #region QuestGeneration
    private void GenerateReplacement(GameState state, QuestType type, long nowMs)
    {
        var (target, reward) = NextParams(state, type);
        long ttlMs = DurationMs(type);
        state.quests.Add(new Quest
        {
            id = Guid.NewGuid().ToString("N"),
            type = type,
            target = target,
            progress = 0,
            rewardFollowers = reward,
            status = QuestStatus.Active,
            expiresAtUnixMs = nowMs + ttlMs
        });
    }

    private long DurationMs(QuestType type)
    {
        // uniform duration for now, could vary by type or scale with completions
        return questLifetimeMs;
    }

    /* // Use for Different durations per type if desired (no use currently)
    private long DurationMs(QuestType type)
    {
        // scalable by completed count if desired
        return type switch
        {
            QuestType.Click => 300_000,       // 300s
            QuestType.Production => 300_000,  // 300s
            QuestType.Purchase => 300_000,    // 300s
            _ => 300_000 // default 5 minutes
        };
    }
    */
    private long NowMs() => (long)_time.NowUnixMs();
    #endregion
}