using System;
using System.Collections.Generic;

namespace Cynexo.Communicator;

/// <summary>
/// Generates commands to be sent to the device
/// </summary>
public static class Command
{
    public static int MinChannelID => 1;
    public static int MaxChannelID => 13;


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
    /// <param name="channel">channel ID from the range <see cref="MinChannelID"/>..<see cref="MaxChannelID"/></param>
    /// <returns>String to send to the port</returns>
    public static string SetCAChannel(int channel)
    {
        if (channel < 0 || channel > MaxChannelID)
        {
            throw new ArgumentException($"Channel must be in the range {0}..{MaxChannelID}");
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
    /// Manually calibrates the flow of the selected olfactometer channel.
    /// It is basically a continuous loop of the readFlow command.
    /// </summary>
    /// <param name="channel">channel ID</param>
    /// <returns>String to send to the port</returns>
    public static string ManualFlow(int channel) => $"manualFlow {channel}";

    /// <summary>
    /// Interrupts the calibration process if needed
    /// </summary>
    /// <returns>String to send to the port</returns>
    public static string StopCalibration => "stopCalibration";

    /// <summary>
    /// Measures the delay between opening of the fast-acting solenoid valve and detection of the 
    /// pressure front after the subject manifold(requires nasal adapters to be plugged
    /// into calibration ports of the device).
    /// </summary>
    /// <param name="channel">channel ID</param>
    /// <returns>String to send to the port</returns>
    public static string TestDelay(int channel) => $"testDelay {channel}";

    #endregion

    #region Channel operations

    /// <summary>
    /// Set the active channel that is implicitely used to operate in further commands
    /// </summary>
    /// <param name="channel">channel ID from the range <see cref="MinChannelID"/>..<see cref="MaxChannelID"/></param>
    /// <returns>String to send to the port</returns>
    public static string SetChannel(int channel)
    {
        if (channel < MinChannelID || channel > MaxChannelID)
        {
            throw new ArgumentException($"Channel must be in the range {MinChannelID}..{MaxChannelID}");
        }

        return $"setChannel {channel}";
    }

    /// <summary>
    /// Read the value of the air flow rate for the currently active channel
    /// </summary>
    /// <returns>String to send to the port</returns>
    public static string ReadFlow => "readFlow";

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

    #region Triggers

    /// <summary>
    /// Set the delay in milliseconds between the valve event and the subsequent generation of the trigger OUT.
    /// </summary>
    /// <param name="delay">milliseconds</param>
    /// <returns>String to send to the port</returns>
    public static string SetTriggerOutDelay(int delay) => $"setTriggerOutDelay {delay}";

    /// <summary>
    /// Set the duration of the trigger OUT signal.
    /// </summary>
    /// <param name="duration">milliseconds</param>
    /// <returns>String to send to the port</returns>
    public static string SetTriggerOutDuration(int duration) => $"setTriggerOutDuration {duration}";

    /// <summary>
    /// Checks that the Sniff-0 trigger OUT port is functioning correctly.
    /// </summary>
    /// <returns>String to send to the port</returns>
    public static string OutTrigger => $"outTrigger";

    /// <summary>
    /// Checks that the Sniff-0 trigger IN port is functioning correctly by waiting for a trigger IN signal for 10 seconds
    /// from when the command is executed. Should it not receive this signal with 10 seconds Sniff-0 will display
    /// a time out message.
    /// </summary>
    /// <returns>String to send to the port</returns>
    public static string InTrigger => $"inTrigger";

    /// <summary>
    /// Test for the correct functioning of the Sniff-0 trigger OUT and trigger IN by sending a trigger signal
    /// out one port and reading it back in through the other port.
    /// IMPORTANT! For the correct execution of the command, please connect the trigger IN connector with the 
    /// trigger OUT connector via a standard BNC male to male cable.
    /// </summary>
    /// <returns>String to send to the port</returns>
    public static string LoopTrigger => $"loopTrigger";

    #endregion

    #region Commands to be used with Spir-0

    /// <summary>
    /// Waits for the trigger IN from Spir-0, indicating that the subject has started **exhaling**,
    /// opens the valve of the selected channel for the selected amount of time, and 
    /// generates the trigger necessary for the Spir-0 audio module to generate the sound after the selected delay. 
    /// </summary>
    /// <param name="channel">channel ID</param>
    /// <param name="duration">in milliseconds</param>
    /// <param name="delay">sound delay, in milliseconds</param>
    /// <param name="useSecondTrigger">if set, generates second trigger indicating the actual start of the sound</param>
    /// <returns>String to send to the port</returns>
    public static string OpenValveOnInhale(int channel, int duration, int delay, bool useSecondTrigger = false) =>
        $"Tb_{GetSecondTriggerCmd(useSecondTrigger)}in_breathSound {channel} {duration} {delay}";

    /// <summary>
    /// Waits for the trigger IN from Spir-0, indicating that the subject has started **inhaling**,
    /// opens the valve of the selected channel for the selected amount of time, and 
    /// generates the trigger necessary for the Spir-0 audio module to generate the sound after the selected delay.
    /// </summary>
    /// <param name="channel">channel ID</param>
    /// <param name="duration">in milliseconds</param>
    /// <param name="delay">sound delay, in milliseconds</param>
    /// <param name="useSecondTrigger">if set, generates second trigger indicating the actual start of the sound</param>
    /// <returns>String to send to the port</returns>
    public static string OpenValveOnExhale(int channel, int duration, int delay, bool useSecondTrigger = false) =>
        $"Tb_{GetSecondTriggerCmd(useSecondTrigger)}out_breathSound {channel} {duration} {delay}";

    /// <summary>
    /// Generates a trigger OUT for the Spir-0 audio module to generate the sound and then opens the valve
    /// of the selected channel after delay for the selected amount of time.
    /// </summary>
    /// <param name="channel">channel ID</param>
    /// <param name="duration">in milliseconds</param>
    /// <param name="delay">sound delay, in milliseconds</param>
    /// <param name="useSecondTrigger">if set, generates second trigger indicating the actual start of the sound</param>
    /// <returns>String to send to the port</returns>
    public static string OpenValveAfterSound(int channel, int duration, int delay, bool useSecondTrigger = false) =>
        $"Tb_{GetSecondTriggerCmd(useSecondTrigger)}soundValve {channel} {duration} {delay}";

    /// <summary>
    /// Opens the valve of the selected channel for the selected amount of time and generates a trigger OUT 
    /// for the Spir-0 audio module to generate the sound based on selected delay.
    /// </summary>
    /// <param name="channel">channel ID</param>
    /// <param name="duration">in milliseconds</param>
    /// <param name="delay">sound delay, in milliseconds</param>
    /// <param name="useSecondTrigger">if set, generates second trigger indicating the actual start of the sound</param>
    /// <returns>String to send to the port</returns>
    public static string OpenValveThenSound(int channel, int duration, int delay, bool useSecondTrigger = false) =>
        $"Tb_{GetSecondTriggerCmd(useSecondTrigger)}valveSound {channel} {duration} {delay}";

    #endregion


    // Internal

    private static string GetSecondTriggerCmd(bool useSecondTrigger) => useSecondTrigger ? "ta_" : "";
}
