using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Core;

public interface IRedisClient
{
    public IDatabase Db { get; }
}

public class RedisClient : IDisposable, IRedisClient
{
    public IDatabase Db { get { return db; } }

    private readonly ConnectionMultiplexer redis;
    private readonly IDatabase db;

    public RedisClient()
    {
        _ = int.TryParse(Environment.GetEnvironmentVariable("REDIS_DATABASE") ?? "0", out var database);

        var options = new ConfigurationOptions
        {
            EndPoints = { Environment.GetEnvironmentVariable("REDIS_URL") ?? "" },
            DefaultDatabase = database,
            Password = GetCredentials()
        };

        redis = ConnectionMultiplexer.Connect(options);
        db = redis.GetDatabase();
    }

    public void Dispose()
    {
        redis.Close();
        GC.SuppressFinalize(this);
    }

    private static string GetCredentials()
    {
        var password = Environment.GetEnvironmentVariable("REDIS_PASSWORD");

        if (password == null)
        {
            var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

            var secretProvider = config.Providers.First();
            secretProvider.TryGet("REDIS_PASSWORD", out password);
        }

        return password;
    }
}
