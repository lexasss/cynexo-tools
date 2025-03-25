using System;
using System.ComponentModel;
using System.Windows.Controls;

namespace Cynexo.App;

public enum ChannelOperationState
{
    Initial,
    Calibrating,
    Calibrated
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Flow)));
        }
    }

    public ChannelOperationState State
    {
        get => field;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsReadyMarkVisible)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsInProcessAnimationVisible)));
        }
    }

    public bool IsReadyMarkVisible => State == ChannelOperationState.Calibrated;
    public bool IsInProcessAnimationVisible => State == ChannelOperationState.Calibrating;


    public event EventHandler<bool>? ActivityChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Channel()
    {
        InitializeComponent();
    }
}
