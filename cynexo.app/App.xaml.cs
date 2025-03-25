﻿using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Cynexo.App;

public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        var settings = Cynexo.App.Properties.Settings.Default;
        if (settings.CallUpgrade)
        {
            settings.Upgrade();
            settings.CallUpgrade = false;
            settings.Save();
        }

        // Set the US-culture across the application to avoid decimal point parsing/logging issues
        var culture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        System.Threading.Thread.CurrentThread.CurrentCulture = culture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

        // Force all TextBox's to select its content upon focused
        EventManager.RegisterClassHandler(typeof(TextBox),
            UIElement.GotFocusEvent,
            new RoutedEventHandler((s, e) => (s as TextBox)?.SelectAll()));
    }
}
