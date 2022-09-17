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

            DataCell dataCell = (DataCell)data;

            if (dataCell != null)
            {
                int.TryParse(values[1].ToString(), out int precision);

                defaultValue = dataCell.Sample == 0 ? defaultValue : $"{Math.Round(dataCell.CalculatedValue, precision)}";
            }

            return defaultValue;
        }

        public object[] ConvertBack(object values, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AttemptsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            object data = values[0];

            string defaultValue = string.Empty;

            DataCell dataCell = (DataCell)data;

            if(dataCell != null && (bool)values[1])
                defaultValue = dataCell.Sample < 100 ? dataCell.Sample.ToString() : "++";

            return defaultValue;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToGridRowHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((bool)value) ? new GridLength(1, GridUnitType.Auto) : new GridLength(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToHiddenVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((bool)value) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NameControlForegroundConverter : IValueConverter
    {
        private object _confirmedForegroundBrush;
        private object _unconfirmedForegroundBrush;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            _confirmedForegroundBrush ??= Application.Current.TryFindResource("NamePanelConfirmedForegroundBrush");
            _unconfirmedForegroundBrush ??= Application.Current.TryFindResource("NamePanelUnconfirmedForegroundBrush");

            return ((bool)value) ? _confirmedForegroundBrush : _unconfirmedForegroundBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CardRadioButtonCheckedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == parameter?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? parameter : null;
        }
    }

    public class AnalyzerInfoForegroundConverter : IValueConverter
    {
        private object _analyzerDefault;
        private object _analyzerInfo;
        private object _analyzerError;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            _analyzerDefault ??= Application.Current.TryFindResource("AnalyzerDefault");
            _analyzerInfo ??= Application.Current.TryFindResource("AnalyzerInfo");
            _analyzerError ??= Application.Current.TryFindResource("AnalyzerError");

            if (value == null)
                return _analyzerDefault;

            return value.ToString() == "It's not hero's turn to act" ? _analyzerInfo : _analyzerError;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SolverInfoTextConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null)
                return null;

            string error = (string)values[0];

            bool isSolving = (bool)values[1];

            if (isSolving)
                return "Solver is running";

            return error;
        }

        public object[] ConvertBack(object values, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SolverInfoForegroundConverter : IValueConverter
    {
        private object _solverDefault;
        private object _solverRunning;
        private object _solverError;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            _solverDefault ??= Application.Current.TryFindResource("SolverDefault");
            _solverRunning ??= Application.Current.TryFindResource("SolverRunning");
            _solverError ??= Application.Current.TryFindResource("SolverError");

            if (value == null)
                return _solverDefault;

            return (bool)value ? _solverRunning : _solverError;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}
