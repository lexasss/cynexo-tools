using Cynexo.Communicator;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Cynexo.App.Pages;

public partial class Setup : Page, IPage<Navigation>, INotifyPropertyChanged
{
    public HighLevelController HighLevelController { get; }

    public event EventHandler<Navigation>? Next;
    public event PropertyChangedEventHandler? PropertyChanged;

    public Setup()
    {
        InitializeComponent();

        HighLevelController = new HighLevelController(_sniff0, GetHighLevelChannels());
        HighLevelController.FlowMeasured += (s, e) => Dispatcher.Invoke(() => lblFlow.Content = $"{e:F2}");

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HighLevelController)));

        App.Current.Exit += (s, e) =>
        {
            HighLevelController.Dispose();
        };
    }

    // Internal

    record class ChannelCalibration(int Id, bool Use, float Flow);

    readonly Storage _storage = Storage.Instance;
    readonly CommPort _sniff0 = CommPort.Instance;

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

    private ChannelCalibration[] GetCalibChannels()
    {
        var props = new List<ChannelCalibration>();
        for (int id = Command.MinChannelID; id <= Command.MaxChannelID; id++)
        {
            props.Add(new ChannelCalibration(id, false, 0));
        }

        void CheckChildren(UIElementCollection controls)
        {
            foreach (UIElement ctrl in controls)
            {
                if (ctrl is CheckBox chk && chk.Name.StartsWith("chkCalibChannel"))
                {
                    int id = int.Parse((string)chk.Tag) - Command.MinChannelID;
                    props[id] = props[id] with { Use = chk.IsChecked == true };
                }
                else if (ctrl is TextBox txb && txb.Name.StartsWith("txbCalibChannel"))
                {
                    if (float.TryParse((ctrl as TextBox)?.Text, out float flow))
                    {
                        int id = int.Parse((string)txb.Tag) - Command.MinChannelID;
                        props[id] = props[id] with { Flow = flow };
                    }
                }
                else if (ctrl is Panel panel)
                {
                    CheckChildren(panel.Children);
                }
            }
        }

        CheckChildren(wplCalibration.Children);
        return props.ToArray();
    }

    // Event handlers

    private async void OnData(object? sender, string data)
    {
        if (!data.StartsWith("Flow: "))
        {
            await Task.Run(() => Dispatcher.Invoke(() =>
            {
                txbResponses.Text += $"RECEIVED: {data}\n";
                txbResponses.ScrollToEnd();
            }));
        }
    }

    private async void OnError(object? sender, Result result)
    {
        await Task.Run(() => Dispatcher.Invoke(() =>
        {
            txbResponses.Text += $"ERROR: {result}\n";
            txbResponses.ScrollToEnd();
        }));
    }

    private void HandleResponse(Result result)
    {
        txbResponses.Text += $"{result}\n";
        txbResponses.ScrollToEnd();
    }

    // UI events

    private void Page_Loaded(object? sender, RoutedEventArgs e)
    {
        _storage
            .BindScaleToZoomLevel(sctScale)
            .BindContentToZoomLevel(lblZoom)
            .BindVisibilityToDebug(lblDebug);

        _sniff0.Data += OnData;
        _sniff0.COMError += OnError;

        var settings = Properties.Settings.Default;
    }

    private void Page_Unloaded(object? sender, RoutedEventArgs e)
    {
        _sniff0.COMError -= OnError;
        _sniff0.Data -= OnData;

        _storage
            .UnbindScaleToZoomLevel(sctScale)
            .UnbindContentToZoomLevel(lblZoom)
            .UnbindVisibilityToDebug(lblDebug);
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        txbResponses.Text = "";
    }

    //private void Close_Click(object sender, RoutedEventArgs e) =>
    //    Next?.Invoke(this, Navigation.Exit);

    private void SetVerbose_Toggled(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.SetVerbose(tbnSetVerbose.IsChecked == true)));

    private void SetVerboseLCD_Toggled(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            HandleResponse(_sniff0.Send(Command.SetVerboseLCD(tbnSetVerboseLCD.IsChecked == true)));
        }
    }

    private void SetCAChannel_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.SetCAChannel(nudSetCAChannel.Value)));

    private void OpenCloseValves_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.SetAllSolenoidValves(rdbOpenValves.IsChecked == true)));

    private void SetFlow_Click(object sender, RoutedEventArgs e)
    {
        var kvs = GetCalibChannels()
            .Where(p => p.Use && p.Flow > 0)
            .Select(p => new KeyValuePair<int, float>(p.Id, p.Flow));

        HandleResponse(_sniff0.Send(Command.SetFlow(kvs.ToArray())));
    }

    private void CalibrateManually_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.ManualFlow(nudCalibrateManually.Value)));

    private void StopCalibration_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.StopCalibration));

    private void TestDelay_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.TestDelay(nudTestDelay.Value)));

    private void SetChannel_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.SetChannel(nudSetChannel.Value)));

    private void ReadFlow_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.ReadFlow));

    private void SetValve_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.SetValve(rdbSetValveOpened.IsChecked == true, chkSetValveTrigger.IsChecked == true)));

    private void SetMotorDirection_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.SetMotorDirection(rdbSetMotorDirectionToOpen.IsChecked == true)));

    private void RunMotorSteps_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.RunMotorSteps((int)sldRunMotorSteps.Value)));

    private void OpenValve_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(txbOpenValve.Text, out int ms))
        {
            if (rdbOpenValveSimple.IsChecked == true)
            {
                HandleResponse(_sniff0.Send(Command.OpenValve(ms, chkOpenValveTrigger.IsChecked == true)));
            }
            else if (rdbOpenValveNoCA.IsChecked == true)
            {
                HandleResponse(_sniff0.Send(Command.OpenValveWithoutCleanAir(ms, chkOpenValveTrigger.IsChecked == true)));
            }
            else if (rdbOpenValveNoCF.IsChecked == true)
            {
                HandleResponse(_sniff0.Send(Command.OpenValveWithoutConstantFlow(ms, chkOpenValveTrigger.IsChecked == true)));
            }
        }
    }

    private void SetTriggerOutDelay_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(txbSetTriggerOutDelay.Text, out int ms))
        {
            HandleResponse(_sniff0.Send(Command.SetTriggerOutDelay(ms)));
        }
    }

    private void SetTriggerOutDuration_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(txbSetTriggerOutDuration.Text, out int ms))
        {
            HandleResponse(_sniff0.Send(Command.SetTriggerOutDuration(ms)));
        }
    }

    private void TestTriggerOut_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.OutTrigger));

    private void TestTriggerIn_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.InTrigger));

    private void LoopTrigger_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.LoopTrigger));

    private void OpenValveOn_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(txbOpenValveOnDuration.Text, out int duration) && 
            int.TryParse(txbOpenValveOnDelay.Text, out int delay))
        {
            int channel = nudOpenValveOn.Value;
            if (cmbOpenValveOn.SelectedIndex == 0)
            {
                HandleResponse(_sniff0.Send(Command.OpenValveOnInhale(channel, duration, delay, chkOpenValveOnSecondTrigger.IsChecked == true)));
            }
            else
            {
                HandleResponse(_sniff0.Send(Command.OpenValveOnExhale(channel, duration, delay, chkOpenValveOnSecondTrigger.IsChecked == true)));
            }
        }
    }

    private void OpenValveSound_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(txbOpenValveSoundDuration.Text, out int duration) && 
            int.TryParse(txbOpenValveSoundDelay.Text, out int delay))
        {
            int channel = nudOpenValveSound.Value;
            if (cmbOpenValveSound.SelectedIndex == 0)
            {
                HandleResponse(_sniff0.Send(Command.OpenValveAfterSound(channel, duration, delay, chkOpenValveSoundSecondTrigger.IsChecked == true)));
            }
            else
            {
                HandleResponse(_sniff0.Send(Command.OpenValveThenSound(channel, duration, delay, chkOpenValveSoundSecondTrigger.IsChecked == true)));
            }
        }
    }

    private void CalibChannel_Toggled(object sender, RoutedEventArgs e) =>
        btnStartCalibration.IsEnabled = GetCalibChannels().Any(p => p.Use);

    private void HLCalibrate_Click(object sender, RoutedEventArgs e)
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
        HighLevelController.AdjustChannel((int)cmbChannels.SelectedItem, Channel.Adjustment.Up);
    }

    private void HLDecreaseFlow_Click(object sender, RoutedEventArgs e)
    {
        HighLevelController.AdjustChannel((int)cmbChannels.SelectedItem, Channel.Adjustment.Down);
    }
}
