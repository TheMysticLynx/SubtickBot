using MongoDB.Driver;
using SubtickBot.Data;

namespace SubtickBot.Services;

public class MongoDbService
{
    public readonly IMongoDatabase Main;
    public readonly IMongoCollection<User> Users;


    public MongoDbService(MongoClient client)
    {
        Main = client.GetDatabase("Main");
        Users = Main.GetCollection<User>("Users");
    }
}