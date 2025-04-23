using Cynexo.Communicator;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Cynexo.App.Widgets;

public partial class HighLevel : UserControl, INotifyPropertyChanged
{
    public HighLevelController HighLevelController { get; }

    public HighLevel()
    {
        InitializeComponent();

        HighLevelController = new HighLevelController(CommPort.Instance, GetHighLevelChannels());
        HighLevelController.FlowMeasured += (s, e) => Dispatcher.Invoke(() => lblFlow.Content = $"{e:F2}");

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HighLevelController)));

        App.Current.Exit += (s, e) =>
        {
            HighLevelController.Dispose();
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // Internal

    private Channel[] GetHighLevelChannels()
    {
        var result = new List<Channel>();

        void AddFrom(UIElementCollection elements)
        {
            foreach (UIElement el in elements)
            {
                if (el is Channel channel)
                {
                    result.Add(channel);
                }
                else if (el is Panel panel)
                {
                    AddFrom(panel.Children);
                }
            }
        }

        AddFrom(wplChannels.Children);

        return result.ToArray();
    }

    // UI events

    private void HLAutomaticCalibration_Click(object sender, RoutedEventArgs e)
    {
        HighLevelController.Calibrate();
    }

    private void HLManualCalibration_Click(object sender, RoutedEventArgs e)
    {
        HighLevelController.ToggleFlow((int)cmbChannels.SelectedItem);
        HighLevelController.ToggleFlowMeasurements();

        btnManualCalibration.Content = HighLevelController.IsManualCalibrationActive ? "Close" : "Open";
    }

    private void HLIncreaseFlow_Click(object sender, RoutedEventArgs e)
    {
        HighLevelController.AdjustChannel((int)cmbChannels.SelectedItem, ChannelFlowAdjustment.Up);
    }

    private void HLDecreaseFlow_Click(object sender, RoutedEventArgs e)
    {
        HighLevelController.AdjustChannel((int)cmbChannels.SelectedItem, ChannelFlowAdjustment.Down);
    }
}
