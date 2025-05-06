using Cynexo.Communicator;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Cynexo.App;

public class HighLevelController : IDisposable, INotifyPropertyChanged
{
    public bool IsBusy
    {
        get => field;
        private set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBusy)));
        }
    } = false;

    public bool IsVerbose
    {
        get => field;
        set
        {
            field = value;
            _sniff0.Send(Command.SetVerbose(value));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVerbose)));
        }
    }

    public bool IsCalibrationMode
    {
        get => field;
        set
        {
            foreach (var channelControl in _channelControls)
            {
                if (!value)
                {
                    channelControl.IsActive = false;
                }
                channelControl.CanEditFlow = value;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCalibrationMode)));
        }
    } = true;

    public bool IsManualCalibrationActive
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsManualCalibrationActive)));
        }
    }

    public int ValveMotorAdjustmentSteps { get; set; } = 10;

    public bool CanChangeMode => IsCalibrationMode || !_channelControls.Any(c => c.IsActive);

    public bool CanToggleValves => _channelControls.Any(c => c.IsActive);
    public bool CanAutoCalibrate => _channelControls.Any(c => c.IsActive) && !_channelControls.Any(c => c.IsOpen);
    public bool CanDoManualCalibration => !_channelControls.Any(c => c.IsOpen);
    public IEnumerable<int> ChannelIDs => _channelControls.Select(c => c.ID);

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<double>? FlowMeasured;

    public HighLevelController(CommPort port, Widgets.Channel[] channelControls)
    {
        _sniff0 = port;
        _channelControls = channelControls;

        LoadState();

        foreach (var channelControl in channelControls)
        {
            channelControl.ActivityChanged += async (s, e) =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanToggleValves)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanAutoCalibrate)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanDoManualCalibration)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanChangeMode)));

                if (!IsCalibrationMode)
                {
                    var channel = (Widgets.Channel)s!;
                    channel.ToggleFlowState();

                    await Task.Delay(100);
                    _sniff0.Send(Command.SetChannel(channel.ID));

                    await Task.Delay(100);
                    _sniff0.Send(Command.SetValve(channel.IsOpen));
                }
            };
        }

        _sniff0.Data += OnData;
    }

    public void Dispose()
    {
        _sniff0.Data -= OnData;

        _flowReadingThread = null;

        SaveState();

        GC.SuppressFinalize(this);
    }

    public async void Calibrate()
    {
        var channels = _channelControls.Where(ch => ch.IsActive && ch.Flow > 0).ToArray();

        if (channels.Length == 0)
        {
            return;
        }

        IsBusy = true;

        foreach (var channel in channels)
        {
            channel.SetState(ChannelOperationState.Initial);
        }

        var kvs = channels.Select(ch => new KeyValuePair<int, float>(ch.ID, (float)ch.Flow));

        int channelIndex = 0;

        _receivedResponse = new();
        _isAwaitingResponse = true;

        _sniff0.Send(Command.SetFlow(kvs.ToArray()));

        while (_isAwaitingResponse)
        {
            await Task.Delay(10);
            if (_receivedResponse.Id != AwaitingResponse.None)
            {
                if (_receivedResponse.Id == AwaitingResponse.EnableChannel)
                {
                    channels[channelIndex].SetState(ChannelOperationState.Calibrating);
                }
                else if (_receivedResponse.Id == AwaitingResponse.MeasuredFlow)
                {
                    if (_receivedResponse.Parameters?.Length == 1)
                        channels[channelIndex].MeasuredFlow = double.Parse(_receivedResponse.Parameters[0]);
                }
                else if (_receivedResponse.Id == AwaitingResponse.ChannelIsReady)
                {
                    channels[channelIndex].SetState(ChannelOperationState.Calibrated);

                    channelIndex += 1;
                    _isAwaitingResponse = channelIndex < channels.Length;
                }

                _receivedResponse = new();
            }
        }

        IsBusy = false;
    }

    public async void ToggleFlow(int? id = null)
    {
        IsBusy = true;

        var channels = id == null
            ? _channelControls.Where(ch => ch.IsActive)
            : _channelControls.Where(ch => ch.ID == id);

        if (id != null)
        {
            IsManualCalibrationActive = !IsManualCalibrationActive;
        }

        _openedChannelId = IsManualCalibrationActive ? id : null;

        foreach (var channel in channels)
        {
            channel.ToggleFlowState();

            await Task.Delay(100);
            _sniff0.Send(Command.SetChannel(channel.ID));

            await Task.Delay(100);
            _sniff0.Send(Command.SetValve(channel.IsOpen));
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanAutoCalibrate)));

        IsBusy = false;
    }

    public void ToggleFlowMeasurements()
    {
        var hasOpenedChannels = _channelControls.Any(ch => ch.IsOpen);
        if (hasOpenedChannels && _flowReadingThread == null)
        {
            _flowReadingThread = new Thread(ReadFlow);
            _flowReadingThread.Start();
        }
        else if (!hasOpenedChannels && _flowReadingThread != null)
        {
            var thread = _flowReadingThread;
            _flowReadingThread = null;
            thread.Join();

            FlowMeasured?.Invoke(this, 0);
        }
    }

    public async void OpenFor(int ms)
    {
        IsBusy = true;

        var channels = _channelControls.Where(ch => ch.IsActive).ToArray();

        foreach (var channel in channels)
        {
            channel.ToggleFlowState();

            await Task.Delay(100);
            _sniff0.Send(Command.SetChannel(channel.ID));

            await Task.Delay(100);
            _sniff0.Send(Command.OpenValve(ms));

            channel.ToggleFlowState();
        }

        IsBusy = false;
    }

    public async void AdjustChannel(int id, ChannelFlowAdjustment direction)
    {
        await Task.Delay(50);
        _sniff0.Send(Command.SetChannel(id));
        await Task.Delay(50);
        _sniff0.Send(Command.SetMotorDirection(direction == ChannelFlowAdjustment.Up));
        await Task.Delay(50);
        _sniff0.Send(Command.RunMotorSteps(ValveMotorAdjustmentSteps));
    }

    // Internal

    enum AwaitingResponse
    {
        None,
        EnableChannel,
        ChannelIsReady,
        MeasuredFlow,
    }

    record class ChannelState(bool IsActive, double Flow);
    record class Response(AwaitingResponse Id = AwaitingResponse.None, string[]? Parameters = null);

    readonly Dictionary<AwaitingResponse, string> AwaitingResponses = new()
    {
        { AwaitingResponse.None, "" },
        { AwaitingResponse.EnableChannel, "enable Channel" },
        { AwaitingResponse.ChannelIsReady, "Value in range" },
        { AwaitingResponse.MeasuredFlow, "Measured Flow" },
    };

    readonly CommPort _sniff0;
    readonly Widgets.Channel[] _channelControls;

    bool _isAwaitingResponse = false;
    Response _receivedResponse = new();
    Thread? _flowReadingThread;
    int? _openedChannelId = null;

    private async void OnData(object? sender, string data)
    {
        await Task.Run(() =>
        {
            if (_flowReadingThread != null)
            {
                var p = data.Split(' ');
                if (p.Length == 3 && p[0].StartsWith("Flow") && double.TryParse(p[2], out double flow))
                {
                    FlowMeasured?.Invoke(this, flow);

                    var channel = _channelControls.FirstOrDefault(ch => ch.ID == _openedChannelId);
                    if (channel != null)
                        channel.MeasuredFlow = flow;
                }
            }
            else if (_isAwaitingResponse)
            {
                if (data.StartsWith(AwaitingResponses[AwaitingResponse.EnableChannel]))
                {
                    _receivedResponse = new(AwaitingResponse.EnableChannel);
                }
                else if (data.StartsWith(AwaitingResponses[AwaitingResponse.ChannelIsReady]))
                {
                    _receivedResponse = new(AwaitingResponse.ChannelIsReady);
                }
                else if (data.StartsWith(AwaitingResponses[AwaitingResponse.MeasuredFlow]))
                {
                    var flow = data.Split(' ').Last();
                    _receivedResponse = new(AwaitingResponse.MeasuredFlow, [flow]);
                }
            }
        });
    }

    private void LoadState()
    {
        var settings = Properties.Settings.Default;

        ValveMotorAdjustmentSteps = settings.Controller_ValveMotorAdjustmentSteps;

        var flowsJson = settings.Controller_Flows;
        if (!string.IsNullOrEmpty(flowsJson))
        {
            try
            {
                var flows = JsonSerializer.Deserialize<ChannelState[]>(flowsJson) ?? [];
                for (int i = 0; i < flows.Length && i < _channelControls.Length; i++)
                {
                    _channelControls[i].IsActive = flows[i].IsActive;
                    _channelControls[i].Flow = flows[i].Flow;
                }
            }
            catch { }
        }
    }

    private void SaveState()
    {
        var flows = _channelControls.Select(ch => new ChannelState(ch.IsActive, ch.Flow)).ToArray();
        var flowsJson = JsonSerializer.Serialize(flows);

        var settings = Properties.Settings.Default;
        settings.Controller_Flows = flowsJson;
        settings.Controller_ValveMotorAdjustmentSteps = ValveMotorAdjustmentSteps;
        settings.Save();
    }

    /// <summary>
    /// The procedure that is running in a separate thread
    /// </summary>
    private void ReadFlow()
    {
        while (true)
        {
            try { Thread.Sleep(500); }
            catch (ThreadInterruptedException) { break; }  // will exit the loop upon Interrupt is called

            if (_flowReadingThread == null)
            {
                break;
            }

            var response = _sniff0.Send(Command.ReadFlow);
            if (response.Error != Error.Success)
                break;
        }

        _flowReadingThread = null;
    }
}
