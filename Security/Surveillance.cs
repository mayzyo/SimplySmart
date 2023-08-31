using SimpleFrigateSorter.Core;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SimpleFrigateSorter.Security.Surveillance;

namespace SimpleFrigateSorter.Security;

public interface ISurveillance
{
    StateMachine<State, Command> State { get; set; }
}

public class Surveillance : BaseState<State, Command>
{
    public Surveillance(IRedisClient redisClient) : base(redisClient)
    {
        //base(redisClient);
        Console.WriteLine("Created");
    }

    public enum State
    {
        HOME, AWAY, NIGHT, OFF
    }

    public enum Command
    {

    }
}
