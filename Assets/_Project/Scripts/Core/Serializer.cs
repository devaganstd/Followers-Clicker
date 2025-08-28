using System;
using System.IO;
using UnityEngine;

public class UnityTimeSource : ITimeSource
{
    public double NowUnixMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

public class SystemRandomWrapper : IRandom
{
    private System.Random _rng = new();
    public void Seed(int seed) => _rng = new System.Random(seed);
    public double NextDouble() => _rng.NextDouble();
}

public class FileStorage : IStorage
{
    private readonly string _folder;
    public FileStorage(string subFolder = "")
    {
        _folder = string.IsNullOrEmpty(subFolder)
            ? Application.persistentDataPath
            : Path.Combine(Application.persistentDataPath, subFolder);
        if (!Directory.Exists(_folder))
            Directory.CreateDirectory(_folder);
    }

    private string PathFor(string key) => Path.Combine(_folder, key + ".json");

    public bool Exists(string key) => File.Exists(PathFor(key));
    public string Load(string key) => File.ReadAllText(PathFor(key));
    public void Save(string key, string data) => File.WriteAllText(PathFor(key), data);
    public void Delete(string key)
    {
        string p = PathFor(key);
        if (File.Exists(p)) File.Delete(p);
    }
}

public class UnityJsonSerializer : ISerializer
{
    public string Serialize(GameState state)
    {
        return JsonUtility.ToJson(state, prettyPrint: false);
    }

    public GameState Deserialize(string json)
    {
        var state = JsonUtility.FromJson<GameState>(json);
        return state;
    }
}