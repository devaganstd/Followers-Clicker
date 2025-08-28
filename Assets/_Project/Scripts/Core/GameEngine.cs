using System;
using UnityEngine;

public class GameEngine : MonoBehaviour
{
    [Header("Time Settings")] // Settings for time tick
    [SerializeField] private float productionTickInterval = 1f; // Interval for time tick in seconds

    [SerializeField] private GameState _state;
    private QuestManager _questManager;
    private Economy _economy;
    private ProductionSystem _production;
    private SaveScheduler _saveScheduler;

    private ITimeSource _time;
    private ISerializer _serializer;
    private IStorage _storage;
    private IRandom _random;

    private float _tickAccumulator = 0f;

    // Public event for UI binding
    public System.Action<GameState> OnStateUpdated;

    private void Awake()
    {
        // initiate core components
        _time = new UnityTimeSource();
        _serializer = new UnityJsonSerializer();
        _storage = new FileStorage();
        _random = new SystemRandomWrapper();

        // initiate game systems
        _questManager = new QuestManager(_random, _time);
        _economy = new Economy();
        _production = new ProductionSystem();
        _saveScheduler = new SaveScheduler(_time, _serializer, _storage);

        Bootstrap();
    }

    private void Bootstrap()
    {
        // Load or create new game state
        var loaded = _saveScheduler.TryLoad();
        if (loaded == null)
        {
            _state = new GameState();
            _state.followersPerClickCache = 1.0; // default 1 follower per click
            _state.followersPerSecondCache = 1.0f; // default 1 follower per second
            _economy.EnsureStarterItems(_state);
            _questManager.EnsureInitialQuests(_state);
            _economy.RecomputeDerived(_state);
            _saveScheduler.MarkDirty();
        }
        else
        {
            _state = loaded;
            _economy.RecomputeDerived(_state);
        }

        //_questManager.EnsureInitialQuests(_state);

        // Reset all active quests to have full duration on load
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (var q in _state.quests)
        {
            if (q.status == QuestStatus.Active)
            {
                q.expiresAtUnixMs = now + _questManager.QuestLifetimeMs; // reset all to 5 minutes
                q.expiredDisplayUntilUnixMs = _questManager.ExpiredRetentionMs; // reset expired display time
            }else if (q.status == QuestStatus.Expired)
            {
                // if expired, set display until to now + retention time
                if (q.expiredDisplayUntilUnixMs == 0)
                    q.expiredDisplayUntilUnixMs = now + _questManager.ExpiredRetentionMs; // show briefly then disappear
            }
        }

        NotifyUI();
    }

    private void Update()
    {
        // production time tick
        _tickAccumulator += Time.deltaTime;

        // Expire overdue quests & auto-replace
        _questManager.ExpireDue(_state);

        if (_tickAccumulator >= productionTickInterval)
        {
            double dt = _tickAccumulator;
            _tickAccumulator = 0f;

            // produce followers based on time passed
            int producedThisTick = _production.ApplyTick(_state, dt);
            if (producedThisTick > 0)
            {
                _questManager.NotifyProduction(_state, producedThisTick); // 
                _saveScheduler.MarkDirty();
            }
            NotifyUI();
        }
    }

    #region UI and Quest Interaction
    // Notify UI of state changes
    private void NotifyUI()
    {
        OnStateUpdated?.Invoke(_state);
    }

    //  Public function for UI and other systems
    public void OnClick()
    {
        _state.followersTotal += (int)_state.followersPerClickCache;
        _state.followersLifetime += (int)_state.followersPerClickCache;
        _state.clickCountLifetime++;
        _questManager.NotifyClick(_state, 1); // notify quests for click ammount 1 becouse its a single click
        _saveScheduler.MarkDirty();
        NotifyUI();
    }

    // Attempt to claim a quest reward
    public void ClaimQuest(string questId)
    {
        if (_questManager.Claim(_state, questId, out var reward))
        {
            _economy.GrantReward(_state, reward);
            _economy.RecomputeDerived(_state);
            _saveScheduler.MarkDirty();
            NotifyUI();
        }
    }

    // Attempt to purchase an item
    public void Purchase(string itemId)
    {
        if (_economy.ApplyPurchase(_state, itemId))
        {
            _economy.RecomputeDerived(_state);
            _questManager.NotifyPurchase(_state, 1);
            _saveScheduler.MarkDirty();
            NotifyUI();
        }
    }
    #endregion

    // Manually trigger a save
    public void ManualSave()
    {
        _saveScheduler.ManualSave(_state);
        NotifyUI();
    }

    // Reset the game to initial state
    public void ResetGame()
    {
        _state = new GameState();
        _economy.EnsureStarterItems(_state);
        _questManager.EnsureInitialQuests(_state);
        _economy.RecomputeDerived(_state);
        _saveScheduler.MarkDirty();
        NotifyUI();
    }

    public GameState Snapshot() => _state;
}
