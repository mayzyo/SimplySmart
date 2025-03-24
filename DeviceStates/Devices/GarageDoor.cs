using Microsoft.Extensions.Logging;
using Quartz;
using SimplySmart.Core.Abstractions;
using SimplySmart.DeviceStates.Jobs;
using SimplySmart.Frigate.Services;
using SimplySmart.Homebridge.Abstractions;
using SimplySmart.Homebridge.Services;
using SimplySmart.Zwave.Abstractions;
using SimplySmart.Zwave.Services;
using Stateless;
using System;
using System.Collections.Generic;

namespace SimplySmart.DeviceStates.Devices;

public interface IGarageDoor : IBinarySwitch, ISwitch, IAccessControl
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

internal class GarageDoor(
    ILogger<IGarageDoor> logger,
    IStateStore stateStore,
    ISchedulerFactory schedulerFactory,
    IHomebridgeEventSender homebridgeEventSender,
    IZwaveEventSender zwaveEventSender,
    IFrigateWebhookSender frigateWebhookSender,
    string name,
    bool? closeDetect = false,
    bool? openDetect = false
) : IGarageDoor
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
        //await stateMachine.ActivateAsync();

        //if(stateMachine.IsInState(GarageDoorState.OPENED))
        //{
        //    await homebridgeEventSender.GarageDoorOpenerOn(name);
        //}
        //else if(stateMachine.IsInState(GarageDoorState.CLOSED))
        //{
        //    await homebridgeEventSender.GarageDoorOpenerOff(name);
        //}
    }

    public IGarageDoor Connect()
    {
        stateMachine.Configure(GarageDoorState.PENDING_CLOSING)
            .OnEntryAsync(SendSetToActiveEvent)
            .Permit(GarageDoorCommand.SET_ACTIVE, GarageDoorState.CLOSING)
            .Ignore(GarageDoorCommand.OPEN)
            .Ignore(GarageDoorCommand.CLOSE);

        var closingConfig = stateMachine.Configure(GarageDoorState.CLOSING)
            .OnEntryAsync(SendCurrentlyClosingEvents);

        if (closeDetect != true)
        {
            closingConfig = closingConfig
                .OnEntryAsync(ScheduleMovingJob)
                .OnExitAsync(CancelMovingJob);
        }

        closingConfig
            .Permit(GarageDoorCommand.COMPLETE, GarageDoorState.CLOSED)
            .Ignore(GarageDoorCommand.SET_ACTIVE)
            .Ignore(GarageDoorCommand.OPEN)
            .Ignore(GarageDoorCommand.CLOSE);

        stateMachine.Configure(GarageDoorState.PENDING_OPENING)
            .OnEntryAsync(SendSetToActiveEvent)
            .Permit(GarageDoorCommand.SET_ACTIVE, GarageDoorState.OPENING)
            .Ignore(GarageDoorCommand.OPEN)
            .Ignore(GarageDoorCommand.CLOSE);

        var openingConfig = stateMachine.Configure(GarageDoorState.OPENING)
            .OnEntryAsync(SendCurrentlyOpeningEvents);

        if (openDetect != true)
        {
            openingConfig = openingConfig
                .OnEntryAsync(ScheduleMovingJob)
                .OnExitAsync(CancelMovingJob);
        }

        openingConfig
            .Permit(GarageDoorCommand.COMPLETE, GarageDoorState.OPENED)
            .Ignore(GarageDoorCommand.SET_ACTIVE)
            .Ignore(GarageDoorCommand.OPEN)
            .Ignore(GarageDoorCommand.CLOSE);

        stateMachine.Configure(GarageDoorState.CLOSED)
            .OnEntryAsync(CompleteGarageDoorActivity)
            .Permit(GarageDoorCommand.OPEN, GarageDoorState.PENDING_OPENING)
            .Permit(GarageDoorCommand.SET_ACTIVE, GarageDoorState.OPENING)
            .Ignore(GarageDoorCommand.CLOSE);

        stateMachine.Configure(GarageDoorState.OPENED)
            .OnEntryAsync(CompleteGarageDoorActivity)
            .Permit(GarageDoorCommand.CLOSE, GarageDoorState.PENDING_CLOSING)
            .Permit(GarageDoorCommand.SET_ACTIVE, GarageDoorState.CLOSING)
            .Ignore(GarageDoorCommand.OPEN);

        return this;
    }

    public async Task Open()
    {
        await stateMachine.FireAsync(GarageDoorCommand.OPEN);
    }

    public async Task Close()
    {
        await stateMachine.FireAsync(GarageDoorCommand.CLOSE);
    }

    public async Task SetCurrentValue(bool isOn)
    {
        // Smart Implant on push doesn't return the correct state of on/off.
        await stateMachine.FireAsync(GarageDoorCommand.SET_ACTIVE);
    }

    public async Task SetToOn(bool isOn)
    {
        // Ignoring the switch state because homebridge don't have a stateless button.
        if(!stateMachine.IsInState(GarageDoorState.OPENED))
        {
            await CorrectStateToOpened();
        }
        else
        {
            await CorrectStateToClosed();
        }
    }

    public async Task HandleContactChange(bool isInContact)
    {
        await SetToCompleteWhenDetectingClose(isInContact);
        await SetToCompleteWhenDetectingOpen(isInContact);
    }

    async Task SetToCompleteWhenDetectingClose(bool isInContact)
    {
        // Ignore if not detecting.
        if(isInContact && closeDetect == true)
        {
            await SetToComplete();
        }
    }

    async Task SetToCompleteWhenDetectingOpen(bool isInContact)
    {
        // Ignore if not detecting.
        if(!isInContact && openDetect == true)
        {
            await SetToComplete();
        }
    }

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

    async Task SendSetToActiveEvent()
    {
        // Smart implant on push triggers the device regardless of the state it is in.
        await zwaveEventSender.BinarySwitchOn(name);
    }

    async Task SendCurrentlyOpeningEvents()
    {
        await homebridgeEventSender.GarageDoorOpenerOn(name);
        await homebridgeEventSender.GarageDoorOpenerMoving(name);
    }

    async Task SendCurrentlyClosingEvents()
    {
        await homebridgeEventSender.GarageDoorOpenerOff(name);
        await homebridgeEventSender.GarageDoorOpenerMoving(name);
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

    async Task CorrectStateToOpened()
    {
        stateStore.UpdateState(name, GarageDoorState.OPENED.ToString());
        await homebridgeEventSender.GarageDoorOpenerOn(name);
        await homebridgeEventSender.GarageDoorOpenerStopped(name);
    }

    async Task CorrectStateToClosed()
    {
        stateStore.UpdateState(name, GarageDoorState.CLOSED.ToString());
        await homebridgeEventSender.GarageDoorOpenerOff(name);
        await homebridgeEventSender.GarageDoorOpenerStopped(name);
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
    CLOSING,
    PENDING_OPENING,
    PENDING_CLOSING
}

public enum GarageDoorCommand
{
    OPEN,
    CLOSE,
    SET_ACTIVE,
    COMPLETE
}