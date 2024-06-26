﻿using Microsoft.Extensions.Configuration;
using SimplySmart.Core.Abstractions;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Core.Services;

internal class RedisStateStore : IDisposable, IStateStore
{
    readonly ConnectionMultiplexer redis;
    readonly IDatabase db;

    public RedisStateStore()
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

    public string? GetState(string key)
    {
        return db.StringGet(key);
    }

    public void UpdateState(string key, string value)
    {
        db.StringSet(key, value);
    }

    public void SetExpiringState(string key, string value, TimeSpan duration)
    {
        db.StringSet(key, value, duration);
    }

    static string GetCredentials()
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
