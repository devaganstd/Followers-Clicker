using System;
using UnityEngine.SceneManagement;

public class SaveScheduler
{
    private bool _dirty;
    private readonly double _autosaveIntervalMs;
    private double _lastAutosaveAttemptMs;

    private readonly ITimeSource _time;
    private readonly ISerializer _serializer;
    private readonly IStorage _storage;
    private readonly string _key;

    public SaveScheduler(ITimeSource time, ISerializer serializer, IStorage storage, string key = "game_state", double autosaveIntervalSeconds = 30)
    {
        _time = time;
        _serializer = serializer;
        _storage = storage;
        _key = key;
        _autosaveIntervalMs = autosaveIntervalSeconds * 1000;
    }

    public void MarkDirty() => _dirty = true;
    public void ClearDirty() => _dirty = false;

    public void AutosaveIfDue(GameState state)
    {
        double now = _time.NowUnixMs();
        if (!_dirty) return;
        if (now - _lastAutosaveAttemptMs < _autosaveIntervalMs) return;

        _lastAutosaveAttemptMs = now;
        Persist(state, isManual: false);
    }

    public void ManualSave(GameState state)
    {
        Persist(state, isManual: true);
    }

    private void Persist(GameState state, bool isManual)
    {
        string json = _serializer.Serialize(state);
        _storage.Save(_key, json);
        ClearDirty();
        double now = _time.NowUnixMs();
        if (isManual) state.lastManualSaveUnixMs = (long)now;
        else state.lastAutosaveUnixMs = (long)now;
    }

    public GameState TryLoad()
    {
        if (!_storage.Exists(_key)) return null;
        try
        {
            string data = _storage.Load(_key);
            return _serializer.Deserialize(data);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"Load failed, new state. {e}");
            return null;
        }
    }

    public void ResetSave(GameState currentState)
    {
        try
        {
            // Delete the save file
            if (_storage.Exists(_key))
            {
                _storage.Delete(_key);
                UnityEngine.Debug.Log($"Save file '{_key}' has been deleted.");
            }

            // Reset dirty flag
            _dirty = false;

            // If a current state is provided, reset its save timestamps
            if (currentState != null)
            {
                currentState.lastManualSaveUnixMs = 0;
                //currentState.lastAutosaveUnixMs = 0;
            }

            // Reset last autosave attempt
            _lastAutosaveAttemptMs = 0;

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to reset save: {e.Message}");
        }
    }
}