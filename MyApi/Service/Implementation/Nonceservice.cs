using System;
using MyApi.Service.Interface;
using StackExchange.Redis;
namespace MyApi.Service.Implementation;

public class Nonceservice:INonceservice
{
    private readonly IDatabase _db;

    public Nonceservice(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task<bool> IsReplayAsync(string nonce)
    {
        return await _db.KeyExistsAsync(nonce);
    }

    public async Task StoreNonceAsync(string nonce, TimeSpan expiry)
    {
        await _db.StringSetAsync(nonce, "1", expiry);
    }
}
