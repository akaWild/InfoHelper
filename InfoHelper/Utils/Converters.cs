using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using InfoHelper.StatsEntities;

namespace InfoHelper.Utils
{
    public class ValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            object data = values[0];

            string defaultValue = "---";

            if (data != null)
            {
                int.TryParse(values[1].ToString(), out int precision);

                int mades = 0, attempts = 0;

                if (data is StatsCell sc)
                {
                    mades = sc.Mades;
                    attempts = sc.Attempts;

                    return attempts == 0 ? defaultValue : $"{Math.Round((double)mades * 100 / attempts, precision)}";
                }

                if (data is ValueCell vc)
                {
                    attempts = vc.Attempts;

                    return attempts == 0 ? defaultValue : $"{Math.Round((double)attempts, precision)}";
                }
            }

            return defaultValue;
        }

        public object[] ConvertBack(object values, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AttemptsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return 0;

            ValueCell data = (ValueCell)value;

            return data.Attempts < 100 ? data.Attempts : "++";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToGridRowHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((bool)value) ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
