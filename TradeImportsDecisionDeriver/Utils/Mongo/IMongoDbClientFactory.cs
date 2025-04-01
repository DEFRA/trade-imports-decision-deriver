using MongoDB.Driver;

namespace TradeImportsDecisionDeriver.Utils.Mongo;

public interface IMongoDbClientFactory
{
    IMongoClient GetClient();

    IMongoCollection<T> GetCollection<T>(string collection);
}