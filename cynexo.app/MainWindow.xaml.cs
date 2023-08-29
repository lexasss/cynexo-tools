using Cynexo.App.Pages;
using Cynexo.App.Utils;
using System.Windows;
using System.Windows.Input;

namespace Cynexo.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var settings = Properties.Settings.Default;

        _connectPage.Next += Page_Next;
        _setupPage.Next += Page_Next;

        if (settings.MainWindow_IsMaximized)
        {
            WindowState = WindowState.Maximized;
        }
        if (settings.MainWindow_Width > 0)
        {
            Left = settings.MainWindow_X;
            Top = settings.MainWindow_Y;
            Width = settings.MainWindow_Width;
            Height = settings.MainWindow_Height;
        }

        string version = Utils.Resources.GetVersion();
        Title += $"   v{version}";
    }

    // Internal

    readonly Connect _connectPage = new();
    readonly Setup _setupPage = new();

    readonly Storage _storage = Storage.Instance;

    bool IsInFullScreen => WindowStyle == WindowStyle.None;

    private void ToggleFullScreen()
    {
        if (!IsInFullScreen)
        {
            var settings = Properties.Settings.Default;
            settings.MainWindow_IsMaximized = WindowState == WindowState.Maximized;
            settings.Save();

            Visibility = Visibility.Collapsed;

            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;

            Visibility = Visibility.Visible;
        }
        else
        {
            WindowState = Properties.Settings.Default.MainWindow_IsMaximized ? WindowState.Maximized : WindowState.Normal;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
        }
    }

    // Events handlers

    private void Page_Next(object? sender, Navigation next)
    {
        if (IsInFullScreen)
        {
            ToggleFullScreen();
        }

        if (next == Navigation.Exit)
        {
            Close();
        }
        else if (next == Navigation.Setup)
        {
            Content = _setupPage;
        }
    }

    // UI events

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Content = _connectPage;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F1)
        {
            MsgBox.Notify(Title, "Developer and tester shortcuts:\n\n" +
                "CONNECTION page\n" +
                "F2 - starts simulator\n\n" +
                "Any page\n" +
                "Ctrl + Scroll - zooms UI in/out\n" +
                "F11 - toggles full screen\n");
        }
        else if (e.Key == Key.OemMinus)
        {
            _storage.ZoomOut();
        }
        else if (e.Key == Key.OemPlus)
        {
            _storage.ZoomIn();
        }
        else if (e.Key == Key.F11)
        {
            ToggleFullScreen();
        }
    }

    private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        {
            if (e.Delta > 0)
            {
                _storage.ZoomIn();
            }
            else
            {
                _storage.ZoomOut();
            }
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        var settings = Properties.Settings.Default;
        settings.MainWindow_X = Left;
        settings.MainWindow_Y = Top;
        settings.MainWindow_Width = Width;
        settings.MainWindow_Height = Height;
        settings.MainWindow_IsMaximized = WindowState == WindowState.Maximized;
        settings.Save();
    }
}
