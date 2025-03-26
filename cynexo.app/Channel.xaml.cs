using System;
using System.ComponentModel;
using System.Windows.Controls;

namespace Cynexo.App;

public enum ChannelOperationState
{
    Initial,
    Calibrating,
    Calibrated,
    Flowing
}

public partial class Channel : UserControl, INotifyPropertyChanged
{
    public int ID
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ID)));
        }
    }

    public bool IsActive
    {
        get => field;
        set
        {
            field = value;
            ActivityChanged?.Invoke(this, value);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive)));
        }
    }

    public double Flow
    {
        get => field;
        set
        {
            field = value;
            if (value == 0)
            {
                IsActive = false;
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Flow)));
        }
    }

    public bool IsCalibrated { get; private set; }

    public bool IsCalibrating => _state == ChannelOperationState.Calibrating;
    public bool IsCalibratedAndClosed => _state == ChannelOperationState.Calibrated;
    public bool IsOpen => _state == ChannelOperationState.Flowing;


    public event EventHandler<bool>? ActivityChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Channel()
    {
        InitializeComponent();
    }

    public void SetState(ChannelOperationState state)
    {
        _state = state;

        if (state == ChannelOperationState.Calibrated)
        {
            IsCalibrated = true;
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCalibratedAndClosed)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCalibrating)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOpen)));
    }

    public void ToggleFlowState() => SetState(
        !IsOpen ?
            ChannelOperationState.Flowing :
            IsCalibrated ?
                ChannelOperationState.Calibrated :
                ChannelOperationState.Initial);

    // Internal

    ChannelOperationState _state = ChannelOperationState.Initial;
}
