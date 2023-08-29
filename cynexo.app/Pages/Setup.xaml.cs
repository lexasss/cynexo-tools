using Cynexo.Communicator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Cynexo.App.Pages;

public partial class Setup : Page, IPage<Navigation>
{
    public event EventHandler<Navigation>? Next;

    public Setup()
    {
        InitializeComponent();

        DataContext = this;
    }

    // Internal

    record class ChannelCalibration(int Id, bool Use, float Flow);

    readonly Storage _storage = Storage.Instance;
    readonly CommPort _sniff0 = CommPort.Instance;

    // Event handlers

    private async void OnData(object? sender, string data)
    {
        await Task.Run(() => Dispatcher.Invoke(() =>
        {
            txbResponses.Text += $"RECEIVED: {data}\n";
            txbResponses.ScrollToEnd();
        }));
    }

    private async void OnError(object? sender, Result result)
    {
        await Task.Run(() => Dispatcher.Invoke(() =>
        {
            txbResponses.Text += $"ERROR: {result}\n";
            txbResponses.ScrollToEnd();
        }));
    }

    private void HandleResponse(Result result, [CallerMemberName] string propertyName = default!)
    {
        txbResponses.Text += $"{propertyName}: {result}\n";
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

    private void Close_Click(object sender, RoutedEventArgs e) =>
        Next?.Invoke(this, Navigation.Exit);

    private void SetVerbose_Toggled(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.SetVerbose(tbnSetVerbose.IsChecked ?? false)));

    private void SetVerboseLCD_Toggled(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            HandleResponse(_sniff0.Send(Command.SetVerboseLCD(tbnSetVerboseLCD.IsChecked ?? false)));
        }
    }

    private void SetCAChannel_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.SetCAChannel((int)sldSetCAChannel.Value)));

    private void OpenCloseValves_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.SetAllSolenoidValves(cmbOpenCloseValves.SelectedIndex == 0)));

    private void SetFlow_Click(object sender, RoutedEventArgs e)
    {
        var props = new List<ChannelCalibration>();
        for (int i = 0; i < 13; i++)
        {
            props.Add(new ChannelCalibration(0, false, 0));
        }

        void CheckChildren(UIElementCollection controls)
        {
            foreach (UIElement ctrl in controls)
            {
                if (ctrl is CheckBox chk && chk.Name.StartsWith("chkCalibChannel"))
                {
                    int id = int.Parse((string)chk.Tag);
                    props[id - 1] = props[id - 1] with { Id = id, Use = chk.IsChecked ?? false };
                }
                else if (ctrl is TextBox txb && txb.Name.StartsWith("txbCalibChannel"))
                {
                    if (float.TryParse((ctrl as TextBox)?.Text, out float flow))
                    {
                        int id = int.Parse((string)txb.Tag);
                        props[id - 1] = props[id - 1] with { Id = id, Flow = flow };
                    }
                }
                else if (ctrl is Panel panel)
                {
                    CheckChildren(panel.Children);
                }
            }
        }

        CheckChildren(wplCalibration.Children);

        var kvs = props.Where(p => p.Use).Select(p => new KeyValuePair<int, float>(p.Id, p.Flow));
        HandleResponse(_sniff0.Send(Command.SetFlow(kvs.ToArray())));
    }

    private void CalibrateManually_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.SetCAChannel((int)sldCalibrateManually.Value)));

    private void StopCalibration_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.StopCalibration));

    private void TestDelay_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.TestDelay((int)sldTestDelay.Value)));

    private void SetChannel_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.SetChannel((int)sldSetChannel.Value)));

    private void ReadFlow_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.ReadFlow));

    private void SetValve_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.SetValve(cmbSetValve.SelectedIndex == 0, chkSetValveTrigger.IsChecked ?? false)));

    private void SetMotorDirection_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.SetMotorDirection(cmbSetMotorDirection.SelectedIndex == 0)));

    private void RunMotorSteps_Click(object sender, RoutedEventArgs e) =>
        HandleResponse(_sniff0.Send(Command.RunMotorSteps((int)sldRunMotorSteps.Value)));

    private void OpenValve_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(txbOpenValve.Text, out int ms))
        {
            if (rdbOpenValveSimple.IsChecked == true)
            {
                HandleResponse(_sniff0.Send(Command.OpenValve(ms, chkOpenValveTrigger.IsChecked ?? false)));
            }
            else if (rdbOpenValveNoCA.IsChecked == true)
            {
                HandleResponse(_sniff0.Send(Command.OpenValveWithoutCleanAir(ms, chkOpenValveTrigger.IsChecked ?? false)));
            }
            else if (rdbOpenValveNoCF.IsChecked == true)
            {
                HandleResponse(_sniff0.Send(Command.OpenValveWithoutConstantFlow(ms, chkOpenValveTrigger.IsChecked ?? false)));
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
            int channel = (int)sldOpenValveOn.Value;
            if (cmbOpenValveOn.SelectedIndex == 0)
            {
                HandleResponse(_sniff0.Send(Command.OpenValveOnInhale(channel, duration, delay, chkOpenValveOnSecondTrigger.IsChecked ?? false)));
            }
            else
            {
                HandleResponse(_sniff0.Send(Command.OpenValveOnExhale(channel, duration, delay, chkOpenValveOnSecondTrigger.IsChecked ?? false)));
            }
        }
    }

    private void OpenValveSound_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(txbOpenValveSoundDuration.Text, out int duration) && 
            int.TryParse(txbOpenValveSoundDelay.Text, out int delay))
        {
            int channel = (int)sldOpenValveSound.Value;
            if (cmbOpenValveSound.SelectedIndex == 0)
            {
                HandleResponse(_sniff0.Send(Command.OpenValveAfterSound(channel, duration, delay, chkOpenValveSoundSecondTrigger.IsChecked ?? false)));
            }
            else
            {
                HandleResponse(_sniff0.Send(Command.OpenValveThenSound(channel, duration, delay, chkOpenValveSoundSecondTrigger.IsChecked ?? false)));
            }
        }
    }
}
