using System;
using System.Collections.Generic;

namespace Cynexo.Controller;

public static class Command
{
    #region General

    /// <summary>
    /// Controls serial port echo
    /// </summary>
    /// <param name="isVerbose">Flag to set</param>
    /// <returns>String to send to the port</returns>
    public static string SetVerbose(bool isVerbose)
    {
        var numValue = isVerbose ? 1 : 0;
        return $"setVerbose {numValue}";
    }

    /// <summary>
    /// Controls echo to the LCD display
    /// </summary>
    /// <param name="isVerbose">Flag to set</param>
    /// <returns>String to send to the port</returns>
    public static string SetVerboseLCD(bool isVerbose)
    {
        var numValue = isVerbose ? 0 : 1;
        return $"setExperiment {numValue}";
    }

    /// <summary>
    /// Sets the channel to be used as the Clean Air channel
    /// </summary>
    /// <param name="channel">channel ID from the range <see cref="MIN_CHANNEL_ID"/>..<see cref="MAX_CHANNEL_ID"/></param>
    /// <returns>String to send to the port</returns>
    public static string SetCAChannel(int channel)
    {
        if (channel < MIN_CHANNEL_ID || channel > MAX_CHANNEL_ID)
        {
            throw new ArgumentException($"Channel must be in the range {MIN_CHANNEL_ID}..{MAX_CHANNEL_ID}");
        }

        return $"setCAChannel {channel}";
    }

    /// <summary>
    /// Opens or closed are solenoid valves
    /// </summary>
    /// <param name="areOpened">Opening flag</param>
    /// <returns>String to send to the port</returns>
    public static string SetAllSolenoidValves(bool areOpened) => areOpened ? "enableAllValves" : "disableAllValves";

    #endregion

    #region Calibration and tests

    /// <summary>
    /// Automatically calibrates the flow of the given channels
    /// </summary>
    /// <param name="flows">list of channelID - channelFlow pairs</param>
    /// <returns>String to send to the port</returns>
    public static string SetFlow(KeyValuePair<int, float>[] flows)
    {
        List<string> result = new();
        foreach (var kv in flows)
        {
            result.Add($"{kv.Key}:{kv.Value}");
        }

        return "setFlow " + string.Join(';', result);
    }


    /// <summary>
    /// Interrupts the calibration process if needed
    /// </summary>
    /// <returns>String to send to the port</returns>
    public static string StopCalibration() => "stopCalibration";

    // Skipping:
    //  ManualFlow
    //  TestDelay

    #endregion

    #region Channel operations

    /// <summary>
    /// Set the active channel that is implicitely used to operate in further commands
    /// </summary>
    /// <param name="channel">channel ID from the range <see cref="MIN_CHANNEL_ID"/>..<see cref="MAX_CHANNEL_ID"/></param>
    /// <returns>String to send to the port</returns>
    public static string SetChannel(int channel)
    {
        if (channel < MIN_CHANNEL_ID || channel > MAX_CHANNEL_ID)
        {
            throw new ArgumentException($"Channel must be in the range {MIN_CHANNEL_ID}..{MAX_CHANNEL_ID}");
        }

        return $"setChannel {channel}";
    }

    /// <summary>
    /// Set the state of the valve of the active channel
    /// </summary>
    /// <param name="isOpened">Flag</param>
    /// <returns>String to send to the port</returns>
    public static string SetValve(bool isOpened, bool sendTrigger = false)
    {
        var triggerCmd = sendTrigger ? "Tout" : "";
        var numValue = isOpened ? 1 : 0;
        return $"set{triggerCmd}Valve {numValue}";
    }

    /// <summary>
    /// Read the value of the air flow rate for the currently active channel
    /// </summary>
    /// <returns>String to send to the port</returns>
    public static string ReadFlow() => "readFlow";

    /// <summary>
    /// Set the direction of the stepper valve motor
    /// </summary>
    /// <param name="toOpen">True for opening, False for closing</param>
    /// <returns>String to send to the port</returns>
    public static string SetMotorDirection(bool toOpen)
    {
        var numValue = toOpen ? 1 : 0;
        return $"setDirection {numValue}";
    }

    /// <summary>
    /// Moves the valve stepper motor by the indicated number of steps.
    /// !WARNING: use count=5-10 in tests, unless it is clear that larger count is safe!
    /// </summary>
    /// <param name="count">Number of motor steps to proceed</param>
    /// <returns>String to send to the port</returns>
    public static string RunMotorSteps(int count) => $"steps {count}";

    /// <summary>
    /// Opens the valve of the active channel for a precise amount of time
    /// </summary>
    /// <param name="ms">milliseconds</param>
    /// <param name="onTrigger">if set, the command is executed upon receiving a trigger</param>
    /// <returns>String to send to the port</returns>
    public static string OpenValve(int ms, bool onTrigger = false)
    {
        var triggerCmd = onTrigger ? "T" : "";
        return $"open{triggerCmd}ValveTimed {ms}";
    }

    /// <summary>
    /// Opens the valve of the active channel for a precise amount of time 
    /// and at the same time disables the constant flow channel.
    /// As soon as the delivery of the odour ends the constant flow channel is reactivated.
    /// We recommend the use of a clean air channel (i.e. a standard odour channel used
    /// without adding any odour) in addition to rather than purely instead of the constant flow channel.
    /// </summary>
    /// <param name="ms">milliseconds</param>
    /// <param name="onTrigger">if set, the command is executed upon receiving a trigger</param>
    /// <returns>String to send to the port</returns>
    public static string OpenValveWithoutConstantFlow(int ms, bool onTrigger = false) =>
        (onTrigger ? "T" : "") + $"CfOffOpenValveTimed {ms}";

    /// <summary>
    /// Opens the valve of the active odour channel for a precise amount of time
    /// and at the same time disables the clean air channel.
    /// As soon as the delivery of the odour ends the clean air channel is reactivated.
    /// </summary>
    /// <param name="ms">milliseconds</param>
    /// <param name="onTrigger">if set, the command is executed upon receiving a trigger</param>
    /// <returns>String to send to the port</returns>
    public static string OpenValveWithoutCleanAir(int ms, bool onTrigger = false) =>
        (onTrigger ? "T" : "") + $"CaOffOpenValveTimed {ms}";

    // Skipped and should avoid using:
    //  SetStepDelay
    //  SetPrecision

    #endregion

    #region Triggered channel operation

    /// <summary>
    /// Waits for the trigger IN from Spir-0, indicating that the subject has started **exhaling**,
    /// opens the valve of the selected channel for the selected amount of time, and 
    /// generates the trigger necessary for the Spir-0 audio module to generate the sound after the selected delay. 
    /// </summary>
    /// <param name="channel">channel ID</param>
    /// <param name="duration">in milliseconds</param>
    /// <param name="delay">sound delay, in milliseconds</param>
    /// <param name="useSecondTrigger">if set, generatesa second trigger indicating the actual start of the sound</param>
    /// <returns>String to send to the port</returns>
    public static string OpenValveOnInhale(int channel, int duration, int delay, bool useSecondTrigger = false)
    {
        var secondTriggerCmd = useSecondTrigger ? "ta_" : "";
        return $"Tb_{secondTriggerCmd}in_breathSound {channel} {duration} {delay}";
    }

    /// <summary>
    /// Waits for the trigger IN from Spir-0, indicating that the subject has started **inhaling**,
    /// opens the valve of the selected channel for the selected amount of time, and 
    /// generates the trigger necessary for the Spir-0 audio module to generate the sound after the selected delay.
    /// </summary>
    /// <param name="channel">channel ID</param>
    /// <param name="duration">in milliseconds</param>
    /// <param name="delay">sound delay, in milliseconds</param>
    /// <returns>String to send to the port</returns>
    public static string OpenValveOnExhale(int channel, int duration, int delay, bool useSecondTrigger = false)
    {
        var secondTriggerCmd = useSecondTrigger ? "ta_" : "";
        return $"Tb_{secondTriggerCmd}out_breathSound {channel} {duration} {delay}";
    }

    // Skipping:
    //  SetTriggerOutDuration
    //  SetTriggerOutDelay
    //  OutTrigger
    //  InTrigger
    //  LoopTrigger

    // Skipping:
    //  Tb_{ta_}soundValve
    //  Tb_{ta_}valveSound
    #endregion


    // Internal

    const int MIN_CHANNEL_ID = 1;
    const int MAX_CHANNEL_ID = 13;
}
