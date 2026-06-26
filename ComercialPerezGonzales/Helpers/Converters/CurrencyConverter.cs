using System.Globalization;
using System.Windows.Data;

namespace ComercialPerezGonzales.Helpers.Converters;

public class CurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d) return d.ToString("N2", culture);
        if (value is double dbl) return dbl.ToString("N2", culture);
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (decimal.TryParse(value?.ToString(), NumberStyles.Any, culture, out var result))
            return result;
        return 0m;
    }
}
