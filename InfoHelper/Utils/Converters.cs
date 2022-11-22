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
using InfoHelper.ViewModel.DataEntities;

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

                if (dataCell.Sample > 0)
                {
                    string postfix = string.Empty;

                    double value = dataCell.CalculatedValue;

                    if (dataCell.CalculatedValue >= 1000000)
                    {
                        value = dataCell.CalculatedValue / 1000000;

                        postfix = "M";
                    }
                    else if (dataCell.CalculatedValue >= 1000)
                    {
                        value = dataCell.CalculatedValue / 1000;

                        postfix = "K";
                    }

                    precision = dataCell.CalculatedValue >= 10000 ? 1 : (dataCell.CalculatedValue >= 1000 ? 2 : precision);

                    defaultValue = $"{Math.Round(value, precision).ToString(CultureInfo.InvariantCulture)}{postfix}";
                }
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

    public class DataCellBackgroundConverter : IValueConverter
    {
        private object _notSelectedBackgroundBrush;
        private object _selectedBackgroundBrush;
        private object _missedBackgroundBrush;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return _notSelectedBackgroundBrush;

            _notSelectedBackgroundBrush ??= Application.Current.TryFindResource("NotSelectedDataCellBackground");
            _selectedBackgroundBrush ??= Application.Current.TryFindResource("SelectedDataCellBackground");
            _missedBackgroundBrush ??= Application.Current.TryFindResource("MissedDataCellBackground");

            DataCell dc = (DataCell)value;

            return dc.CellSelectedState == CellSelectedState.Selected ? _selectedBackgroundBrush : (dc.CellSelectedState == CellSelectedState.Missed ? _missedBackgroundBrush : _notSelectedBackgroundBrush);
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

    public class HandTypeForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            HandType handType = (HandType)value;

            return handType switch
            {
                HandType.MadeHand => Brushes.ForestGreen,
                HandType.DrawHand => Brushes.Red,
                HandType.ComboHand => Brushes.Blue,
                _ => null,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ErrorMessageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string error = value?.ToString();

            return string.IsNullOrEmpty(error) || error.Length < 160 ? error : $"{error.Substring(0, 160)}..."; 
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
