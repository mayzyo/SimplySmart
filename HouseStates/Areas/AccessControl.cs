using SimplySmart.Core.Abstractions;
using SimplySmart.DeviceStates.Devices;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.HouseStates.Areas;

public interface IAccessControl
{
    AccessControlState State { get; }
    void SetAccess(bool isClosed);
}

public class AccessControl(IStateStore stateStore, string name) : IAccessControl
{
    public AccessControlState State { get { return stateMachine.State; } }
    public readonly StateMachine<AccessControlState, AccessControlCommand> stateMachine = new(
        () =>
        {
            var stateString = stateStore.GetState(name);
            if (Enum.TryParse(stateString, out AccessControlState state))
            {
                return state;
            }

            return AccessControlState.CLOSED;
        },
        s => stateStore.UpdateState(name, s.ToString())
    );

    public IAccessControl Connect()
    {
        stateMachine.Configure(AccessControlState.OPENED)
            .Permit(AccessControlCommand.SET_CLOSE, AccessControlState.CLOSED);

        stateMachine.Configure(AccessControlState.CLOSED)
            .Permit(AccessControlCommand.SET_OPEN, AccessControlState.OPENED);

        return this;
    }

    public IAccessControl Connect(IGarageDoor garageDoor)
    {
        Connect();

        stateMachine.Configure(AccessControlState.OPENED)
            .OnEntry(() =>
            {
                if(garageDoor.State == GarageDoorState.OPENING)
                {
                    garageDoor.SetToComplete();
                }
            });

        stateMachine.Configure(AccessControlState.CLOSED)
            .OnEntry(() =>
            {
                if (garageDoor.State == GarageDoorState.CLOSING)
                {
                    garageDoor.SetToComplete();
                }
            });

        return this;
    }

    public void SetAccess(bool isClosed)
    {
        stateMachine.Fire(isClosed ? AccessControlCommand.SET_CLOSE : AccessControlCommand.SET_OPEN);
    }
}

public enum AccessControlState
{
    OPENED,
    CLOSED
}

public enum AccessControlCommand
{
    SET_OPEN,
    SET_CLOSE
}