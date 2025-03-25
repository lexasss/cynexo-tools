using Cynexo.Communicator;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
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

    public bool CanProceed => _channelControls.Any(c => c.IsActive);

    public event PropertyChangedEventHandler? PropertyChanged;

    public HighLevelController(CommPort port, Channel[] channelControls)
    {
        _sniff0 = port;
        _channelControls = channelControls;

        LoadState();

        foreach (var channelControl in channelControls)
        {
            channelControl.ActivityChanged += (s, e) =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanProceed)));
            };
        }

        _sniff0.Data += OnData;
    }

    public void Dispose()
    {
        _sniff0.Data -= OnData;

        SaveState();

        GC.SuppressFinalize(this);
    }

    public async void Calibrate()
    {
        IsBusy = true;

        foreach (var channel in _channelControls)
        {
            channel.State = ChannelOperationState.Initial;
        }

        var channels = _channelControls.Where(ch => ch.IsActive && ch.Flow > 0).ToArray();
        var kvs = channels.Select(ch => new KeyValuePair<int, float>(ch.ID, (float)ch.Flow));

        if (kvs.Any())
        {
            int channelIndex = 0;

            _hasResponse = false;
            _awaitedAction = AwaitingActions.SelectChannel;

            _sniff0.Send(Command.SetFlow(kvs.ToArray()));

            while (_awaitedAction != AwaitingActions.None)
            {
                await Task.Delay(10);
                if (_hasResponse)
                {
                    if (_awaitedAction == AwaitingActions.SelectChannel)
                    {
                        channels[channelIndex].State = ChannelOperationState.Calibrating;
                        _awaitedAction = AwaitingActions.ChannelReady;
                    }
                    else if (_awaitedAction == AwaitingActions.ChannelReady)
                    {
                        channels[channelIndex].State = ChannelOperationState.Calibrated;

                        channelIndex += 1;
                        _awaitedAction = channelIndex < channels.Length ? AwaitingActions.SelectChannel : AwaitingActions.None;
                    }

                    _hasResponse = false;
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
            channel.State = !channel.IsFlowing ? ChannelOperationState.Flowing :
                (channel.IsCalibrated ? ChannelOperationState.Calibrated : ChannelOperationState.Initial);

            await Task.Delay(100);
            _sniff0.Send(Command.SetChannel(channel.ID));

            await Task.Delay(100);
            _sniff0.Send(Command.SetValve(channel.IsFlowing));
        }

        IsBusy = false;
    }

    // Internal

    enum AwaitingActions
    {
        None,
        SelectChannel,
        ChannelReady,
    }

    record class ChannelState(bool IsActive, double Flow);

    readonly Dictionary<AwaitingActions, string> Actions = new()
    {
        { AwaitingActions.None, "" },
        { AwaitingActions.SelectChannel, "enable Channel" },
        { AwaitingActions.ChannelReady, "Value in range" },
    };

    readonly CommPort _sniff0;
    readonly Channel[] _channelControls;

    AwaitingActions _awaitedAction = AwaitingActions.None;
    bool _hasResponse = false;

    private async void OnData(object? sender, string data)
    {
        await Task.Run(() =>
        {
            if (data.StartsWith(Actions[_awaitedAction]))
            {
                _hasResponse = true;
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
}
