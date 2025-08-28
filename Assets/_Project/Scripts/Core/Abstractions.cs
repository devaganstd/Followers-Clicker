using System.Threading.Tasks;

public interface ITimeSource
{
    double NowUnixMs(); // Milliseconds precision
}

public interface IStorage
{
    bool Exists(string key);
    string Load(string key);
    void Save(string key, string data);
    void Delete(string key);
}

public interface IRandom
{
    void Seed(int seed);
    double NextDouble(); // [0,1)
}

public interface ISerializer
{
    string Serialize(GameState state);
    GameState Deserialize(string json);
}