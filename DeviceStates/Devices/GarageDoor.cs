using Microsoft.Extensions.Logging;
using Quartz;
using SimplySmart.Core.Abstractions;
using SimplySmart.DeviceStates.Jobs;
using SimplySmart.Frigate.Services;
using SimplySmart.Homebridge.Services;
using SimplySmart.Zwave.Abstractions;
using SimplySmart.Zwave.Services;
using Stateless;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimplySmart.DeviceStates.Devices;

public interface IGarageDoor : IBinarySwitch
{
    IGarageDoor Connect();
    GarageDoorState State { get; }
    Task Publish();
    Task Open();
    Task Close();
    Task Toggle();
    Task StateVerified(bool isClosed);
    Task SetToComplete();
}

internal class GarageDoor(ILogger<IGarageDoor> logger, IStateStore stateStore, ISchedulerFactory schedulerFactory, IHomebridgeEventSender homebridgeEventSender, IZwaveEventSender zwaveEventSender, IFrigateWebhookSender frigateWebhookSender, string name) : IGarageDoor
{
    const int DELAY_DEFAULT = 20000;

    public GarageDoorState State { get { return stateMachine.State; } }
    public readonly StateMachine<GarageDoorState, GarageDoorCommand> stateMachine = new(
        () =>
        {
            var stateString = stateStore.GetState(name);
            if (Enum.TryParse(stateString, out GarageDoorState state))
            {
                return state;
            }

            return GarageDoorState.CLOSED;
        },
        s => stateStore.UpdateState(name, s.ToString())
    );

    int RetryAttempts
    {
        get
        {
            var countString = stateStore.GetState(name + "_retries");
            if (int.TryParse(countString, out int count))
            {
                return count;
            }

            return 0;
        }
        set { stateStore.UpdateState(name + "_retries", value.ToString()); }
    }

    public async Task Publish()
    {
        await stateMachine.ActivateAsync();

        if(stateMachine.IsInState(GarageDoorState.OPENED))
        {
            await homebridgeEventSender.GarageDoorOpenerOn(name);
        }
        else if(stateMachine.IsInState(GarageDoorState.CLOSED))
        {
            await homebridgeEventSender.GarageDoorOpenerOff(name);
        }
    }

    public IGarageDoor Connect()
    {
        stateMachine.Configure(GarageDoorState.CLOSED)
            .OnEntryAsync(CompleteGarageDoorActivity)
            .OnActivateAsync(CompleteGarageDoorActivity)
            .Permit(GarageDoorCommand.OPEN, GarageDoorState.OPENING)
            .Ignore(GarageDoorCommand.CLOSE);

        stateMachine.Configure(GarageDoorState.OPENED)
            .OnEntryAsync(CompleteGarageDoorActivity)
            .OnActivateAsync(CompleteGarageDoorActivity)
            .Permit(GarageDoorCommand.CLOSE, GarageDoorState.CLOSING)
            .Ignore(GarageDoorCommand.OPEN);

        stateMachine.Configure(GarageDoorState.CLOSING)
            .OnEntryAsync(SetToClosing)
            .OnEntryAsync(ScheduleMovingJob)
            .OnActivateAsync(ScheduleMovingJob)
            .OnExitAsync(CancelMovingJob)
            .Permit(GarageDoorCommand.COMPLETE, GarageDoorState.CLOSED)
            .Ignore(GarageDoorCommand.CLOSE);

        stateMachine.Configure(GarageDoorState.OPENING)
            .OnEntryAsync(SetToOpening)
            .OnEntryAsync(ScheduleMovingJob)
            .OnActivateAsync(ScheduleMovingJob)
            .OnExitAsync(CancelMovingJob)
            .Permit(GarageDoorCommand.COMPLETE, GarageDoorState.OPENED)
            .Ignore(GarageDoorCommand.OPEN);

        return this;
    }

    public Task SetToOn(bool isOn)
    {
        // Ignored because Smart Implant on push doesn't return the correct state of on/off.
        return Task.CompletedTask;
    }

    public async Task Open()
    {
        await stateMachine.FireAsync(GarageDoorCommand.OPEN);
    }

    public async Task Close()
    {
        await stateMachine.FireAsync(GarageDoorCommand.CLOSE);
    }

    // Unused. Waiting for classifier model.
    public async Task StateVerified(bool isClosed)
    {
        var detectedState = isClosed ? GarageDoorState.CLOSED : GarageDoorState.OPENED;
        if (!stateMachine.IsInState(detectedState))
        {
            logger.LogInformation($"State mismatch at {name}");
            if (RetryAttempts <= 3)
            {
                RetryAttempts += 1;
                await zwaveEventSender.BinarySwitchOn(name);
            }
            else
            {
                RetryAttempts = 0;
                logger.LogError($"State continue to mismatch({detectedState}) after 3 attempts at {name}");
            }
        }
        else
        {
            logger.LogInformation($"State in sync at {name}");
            RetryAttempts = 0;
        }
    }

    public async Task Toggle()
    {
        if (State == GarageDoorState.OPENED)
        {
            await stateMachine.FireAsync(GarageDoorCommand.CLOSE);
        }
        else if (State == GarageDoorState.CLOSED)
        {
            await stateMachine.FireAsync(GarageDoorCommand.OPEN);
        }
    }

    async Task SetToOpening()
    {
        await homebridgeEventSender.GarageDoorOpenerOn(name);
        await homebridgeEventSender.GarageDoorOpenerMoving(name);
        // Smart implant on push triggers the device regardless of the state it is in.
        await zwaveEventSender.BinarySwitchOn(name);
    }

    async Task SetToClosing()
    {
        await homebridgeEventSender.GarageDoorOpenerOff(name);
        await homebridgeEventSender.GarageDoorOpenerMoving(name);
        // Smart implant on push triggers the device regardless of the state it is in.
        await zwaveEventSender.BinarySwitchOn(name);
    }

    async Task CompleteGarageDoorActivity()
    {
        await homebridgeEventSender.GarageDoorOpenerStopped(name);

        // Unused. Waiting for classifier model.
        //if (name.Contains("Driveway"))
        //{
        //    await frigateWebhookSender.CreateGarageDoorSnapshot("driveway_cam");
        //}
    }

    public async Task SetToComplete()
    {
        await stateMachine.FireAsync(GarageDoorCommand.COMPLETE);
    }

    async Task ScheduleMovingJob()
    {
        var scheduler = await schedulerFactory.GetScheduler();
        var triggerDelayJob = JobBuilder.Create<GarageDoorMovingJob>()
            .WithIdentity($"{name}_MovingJob")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"{name}_MovingTrigger")
            .StartAt(DateTimeOffset.Now.AddMilliseconds(DELAY_DEFAULT))
            .Build();

        await scheduler.ScheduleJob(triggerDelayJob, trigger);
    }

    async Task CancelMovingJob()
    {
        var scheduler = await schedulerFactory.GetScheduler();
        var jobKey = new JobKey($"{name}_MovingJob");
        if (await scheduler.CheckExists(jobKey))
        {
            await scheduler.DeleteJob(jobKey);
        }
    }
}

public enum GarageDoorState
{
    OPENED,
    CLOSED,
    OPENING,
    CLOSING
}

public enum GarageDoorCommand
{
    OPEN,
    CLOSE,
    COMPLETE
}