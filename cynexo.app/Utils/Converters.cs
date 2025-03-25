using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Cynexo.App.Utils;

public class NumberToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value.GetType() == typeof(int))
        {
            return (int)value != 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        else if (value.GetType() == typeof(double))
        {
            return (double)value != 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        else return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var visibility = (Visibility)value;
        return visibility == Visibility.Visible ? 1.0 : 0.0;
    }
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isInverted = parameter != null;
        return (bool)value ?
            (isInverted ? Visibility.Hidden : Visibility.Visible) : 
            (isInverted ? Visibility.Visible : Visibility.Hidden);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isInverted = parameter != null;
        var visibility = (Visibility)value;
        return isInverted ?
            visibility != Visibility.Visible :
            visibility == Visibility.Visible;
    }
}

public class ObjectToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolInverseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !(bool)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !(bool)value;
    }
}

public class ZoomToPercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value.GetType() == typeof(float) || value.GetType() == typeof(double))
        {
            double number = (double)value * 100;
            return $"{number:F0}%";
        }
        else
        {
            return "NaN";
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
