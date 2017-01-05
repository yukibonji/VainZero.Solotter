using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace VainZero.Windows.Converters
{
    public sealed class DateTimeToLocalFullDateTimeConverter
        : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var pattern = CultureInfo.CurrentUICulture.DateTimeFormat.FullDateTimePattern;
            var dateTime = (DateTime)value;
            return dateTime.ToLocalTime().ToString(pattern);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public static IValueConverter Instance { get; } =
            new DateTimeToLocalFullDateTimeConverter();
    }
}
