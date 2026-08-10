using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Wpf.Ui.Common;

namespace Bloxstrap.UI.Converters
{
    [ValueConversion(typeof(SymbolRegular), typeof(Visibility))]
    public class SymbolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SymbolRegular symbol && symbol != SymbolRegular.Empty)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
