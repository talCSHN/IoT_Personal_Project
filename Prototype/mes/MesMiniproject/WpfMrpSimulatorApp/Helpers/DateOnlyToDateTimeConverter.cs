using System.Globalization;
using System.Windows.Data;

namespace WpfMrpSimulatorApp.Helpers
{
    // 형변환시 WPF에 컨버터 클래스 작성은 필요작업!
    // DateTime, TimeOnly, DateOnly 형을 <--> String 형으로 형변환
    public class DateOnlyToDateTimeConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateOnly dateOnly)
                return new DateTime(dateOnly.Year, dateOnly.Month, dateOnly.Day);
            return null;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
                return DateOnly.FromDateTime(dateTime);
            return null;
        }
    }
}
