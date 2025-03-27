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

    public bool CanToggleValves => _channelControls.Any(c => c.IsActive);
    public bool CanCalibrate => _channelControls.Any(c => c.IsActive) && !_channelControls.Any(c => c.IsOpen);

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<string>? FlowMeasured;

    public HighLevelController(CommPort port, Channel[] channelControls)
    {
        _sniff0 = port;
        _channelControls = channelControls;

        LoadState();

        foreach (var channelControl in channelControls)
        {
            channelControl.ActivityChanged += (s, e) =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanToggleValves)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanCalibrate)));
            };
            channelControl.AdjustmentRequested += async (s, e) =>
            {
                if (s is Channel ch)
                {
                    AdjustChannel(ch.ID, e);
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
        IsBusy = true;

        foreach (var channel in _channelControls)
        {
            channel.SetState(ChannelOperationState.Initial);
        }

        var channels = _channelControls.Where(ch => ch.IsActive && ch.Flow > 0).ToArray();
        var kvs = channels.Select(ch => new KeyValuePair<int, float>(ch.ID, (float)ch.Flow));

        if (kvs.Any())
        {
            int channelIndex = 0;

            _receivedResponse = AwaitingResponse.None;
            _isAwaitingResponse = true;

            _sniff0.Send(Command.SetFlow(kvs.ToArray()));

            while (_isAwaitingResponse)
            {
                await Task.Delay(10);
                if (_receivedResponse != AwaitingResponse.None)
                {
                    if (_receivedResponse == AwaitingResponse.EnableChannel)
                    {
                        channels[channelIndex].SetState(ChannelOperationState.Calibrating);
                    }
                    else if (_receivedResponse == AwaitingResponse.ChannelIsReady)
                    {
                        channels[channelIndex].SetState(ChannelOperationState.Calibrated);

                        channelIndex += 1;
                        _isAwaitingResponse = channelIndex < channels.Length;
                    }

                    _receivedResponse = AwaitingResponse.None;
                }
            }
        }

        IsBusy = false;
    }

    public async void Toggle()
    {
        IsBusy = true;

        var channels = _channelControls.Where(ch => ch.IsActive).ToArray();

        foreach (var channel in channels)
        {
            channel.ToggleFlowState();

            await Task.Delay(100);
            _sniff0.Send(Command.SetChannel(channel.ID));

            await Task.Delay(100);
            _sniff0.Send(Command.SetValve(channel.IsOpen));
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanCalibrate)));

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

            FlowMeasured?.Invoke(this, "");
        }

        IsBusy = false;
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

    public async void AdjustChannel(int id, Channel.Adjustment direction)
    {
        await Task.Delay(50);
        _sniff0.Send(Command.SetChannel(id));
        await Task.Delay(50);
        _sniff0.Send(Command.SetValve(false));
        await Task.Delay(50);
        _sniff0.Send(Command.SetMotorDirection(direction == Channel.Adjustment.Up));
        await Task.Delay(50);
        _sniff0.Send(Command.RunMotorSteps(5));
        await Task.Delay(50);
        _sniff0.Send(Command.SetValve(true));
    }

    // Internal

    enum AwaitingResponse
    {
        None,
        EnableChannel,
        ChannelIsReady,
    }

    record class ChannelState(bool IsActive, double Flow);

    readonly Dictionary<AwaitingResponse, string> AwaitingResponses = new()
    {
        { AwaitingResponse.None, "" },
        { AwaitingResponse.EnableChannel, "enable Channel" },
        { AwaitingResponse.ChannelIsReady, "Value in range" },
    };

    readonly CommPort _sniff0;
    readonly Channel[] _channelControls;

    bool _isAwaitingResponse = false;
    AwaitingResponse _receivedResponse = AwaitingResponse.None;
    Thread? _flowReadingThread;

    private async void OnData(object? sender, string data)
    {
        await Task.Run(() =>
        {
            if (_flowReadingThread != null)
            {
                var p = data.Split(' ');
                if (p.Length == 3 && p[0].StartsWith("Flow"))
                    FlowMeasured?.Invoke(this, p[2]);
            }
            else if (_isAwaitingResponse)
            {
                if (data.StartsWith(AwaitingResponses[AwaitingResponse.EnableChannel]))
                {
                    _receivedResponse = AwaitingResponse.EnableChannel;
                }
                else if (data.StartsWith(AwaitingResponses[AwaitingResponse.ChannelIsReady]))
                {
                    _receivedResponse = AwaitingResponse.ChannelIsReady;
                }
            }
        });
    }

    private void LoadState()
    {
        var flowsJson = Properties.Settings.Default.Controller_Flows;
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
