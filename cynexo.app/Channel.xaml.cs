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

    public string FlowString
    {
        get => field;
        set
        {
            field = value;
            if (double.TryParse(value, out double flow) && flow == 0)
            {
                IsActive = false;
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FlowString)));
        }
    } = "0";

    public ChannelOperationState State
    {
        get => field;
        set
        {
            field = value;

            if (value == ChannelOperationState.Calibrated)
            {
                IsCalibrated = true;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCalibratedAndClosed)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCalibrating)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFlowing)));
        }
    }

    public double Flow
    {
        get => double.Parse(FlowString);
        set => FlowString = value.ToString();
    }

    public bool IsCalibrated { get; private set; }

    public bool IsCalibrating => State == ChannelOperationState.Calibrating;
    public bool IsCalibratedAndClosed => State == ChannelOperationState.Calibrated;
    public bool IsFlowing => State == ChannelOperationState.Flowing;


    public event EventHandler<bool>? ActivityChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Channel()
    {
        InitializeComponent();
    }
}
