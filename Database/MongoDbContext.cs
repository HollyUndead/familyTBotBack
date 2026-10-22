using familyTBotBack.TypedClass;
using MongoDB.Driver;

namespace familyTBotBack.Database;

public sealed class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IMongoDatabase database)
    {
        _database = database;
    }

    public IMongoCollection<User> Users =>
        _database.GetCollection<User>("users");

    public IMongoCollection<Profile> Profiles =>
        _database.GetCollection<Profile>("profiles");
}
