using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFrigateSorter.Core;

public abstract class BaseState<T, K>
{
    protected readonly IRedisClient redisClient;

    public StateMachine<T, K> State;
    private T? innerState;

    public BaseState(IRedisClient redisClient)
    {
        this.redisClient = redisClient;
        State = new(ReadState, WriteState);
    }

    protected T ReadState()
    {
        ValidateState(innerState);
        return innerState;
    }

    protected void WriteState(T newState)
    {
        innerState = newState;
        var dbValue = newState?.ToString();
        redisClient.Db.HashSetAsync("simply-smart:surveillance", "hey", dbValue);
    }

    private async void ValidateState(T currentState)
    {
        //var dbValue = await redisClient.Db.HashGetAsync("simply-smart:surveillance", "hey");
        //_ = Enum.TryParse(dbValue, out T dbState);

        //if (currentState != null && currentState != dbState)
        //{
        //    innerState = dbState;
        //    // TODO: Trigger a notification
        //}
    }
}
