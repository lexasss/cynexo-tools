using Cynexo.Communicator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Cynexo.App.Pages;

public partial class Connect : Page, IPage<Navigation>
{
    public event EventHandler<Navigation>? Next;

    public Connect()
    {
        InitializeComponent();

        DataContext = this;

        UpdatePortList(cmbSniff0CommPort);

        Application.Current.Exit += (s, e) => Close();

        LoadSettings();

        _usb.Inserted += (s, e) => Dispatcher.Invoke(() =>
        {
            UpdatePortList(cmbSniff0CommPort);
        });
        _usb.Removed += (s, e) => Dispatcher.Invoke(() =>
        {
            UpdatePortList(cmbSniff0CommPort);
        });
    }


    // Internal

    readonly COMUtils _usb = new();
    readonly Storage _storage = Storage.Instance;

    readonly CommPort _sniff0 = CommPort.Instance;

    private void UpdatePortList(ComboBox cmb)
    {
        var current = cmb.SelectedValue;

        cmb.Items.Clear();

        var availablePorts = _usb.Ports.Select(port => port.Name);
        var ports = new HashSet<string>(availablePorts);
        foreach (var port in ports)
        {
            cmb.Items.Add(port);
        }

        if (current != null)
        {
            foreach (var item in ports)
            {
                if (item == current.ToString())
                {
                    cmb.SelectedItem = item;
                    break;
                }
            }
        }
    }

    private void Close()
    {
        if (_sniff0.IsOpen)
        {
            _sniff0.Close();
        }
    }

    private void LoadSettings()
    {
        var settings = Properties.Settings.Default;

        foreach (string item in cmbSniff0CommPort.Items)
        {
            if (item == settings.Comm_Port)
            {
                cmbSniff0CommPort.SelectedItem = item;
                break;
            }
        }
    }

    private void SaveSettings()
    {
        var settings = Properties.Settings.Default;
        try
        {
            settings.Comm_Port = cmbSniff0CommPort.SelectedItem?.ToString() ?? "";
        }
        catch { }
        settings.Save();
    }

    // implement later
    /*
    private async void Comm_DebugAsync(object? sender, string e)
	{
		await Task.Run(() => { });
	}*/

    // UI events

    private void Page_Loaded(object? sender, RoutedEventArgs e)
    {
        _storage
            .BindScaleToZoomLevel(sctScale)
            .BindContentToZoomLevel(lblZoom)
            .BindVisibilityToDebug(lblDebug);

        if (Focusable)
        {
            Focus();
        }
    }

    private void Page_Unloaded(object? sender, RoutedEventArgs e)
    {
        _storage
            .UnbindScaleToZoomLevel(sctScale)
            .UnbindContentToZoomLevel(lblZoom)
            .UnbindVisibilityToDebug(lblDebug);
    }

    private void Page_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F2)
        {
            cmbSniff0CommPort.Items.Clear();
            cmbSniff0CommPort.Items.Add("simulator");
            cmbSniff0CommPort.SelectedIndex = 0;

            _storage.IsDebugging = true;
            lblDebug.Visibility = Visibility.Visible;
        }
        else if (e.Key == Key.Enter)
        {
            if (btnConnect.IsEnabled)
            {
                Connect_Click(sender, e);
            }
        }
    }

    private void Port_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        btnConnect.IsEnabled = cmbSniff0CommPort.SelectedIndex >= 0;
    }

    private void Connect_Click(object? sender, RoutedEventArgs e)
    {
        var address = _storage.IsDebugging ? null : (string)cmbSniff0CommPort.SelectedItem;
        Result result = _sniff0.Open(address);

        if (result.Error != Error.Success)
        {
            Utils.MsgBox.Error(Title, "Cannot open the port");
        }
        else
        {
            SaveSettings();
            Next?.Invoke(this, Navigation.Setup);
        }
    }
}
