using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using HandUtility;
using InfoHelper.DataProcessor;
using InfoHelper.StatsEntities;
using InfoHelper.ViewModel.DataEntities;
using StatUtility;

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

    public class DataCellForegroundConverter : IValueConverter
    {
        private object _gtoForegroundBrush;
        private object _defaultForegroundBrush;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            _gtoForegroundBrush ??= Application.Current.TryFindResource("GtoDataCellForeground");
            _defaultForegroundBrush ??= Application.Current.TryFindResource("DefaultDataCellForeground");

            DataCell dataCell = (DataCell)value;

            if (dataCell == null || dataCell.Sample < Shared.MinDeviationSample)
                return _defaultForegroundBrush;

            if (dataCell is ValueCell or EvCell)
                return _defaultForegroundBrush;

            return !float.IsNaN(dataCell.GtoValue) ? _gtoForegroundBrush : _defaultForegroundBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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

            _notSelectedBackgroundBrush ??= Application.Current.TryFindResource("DefaultDataCellBackground");
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

            return isSolving ? "Solver is running" : error;
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

    public class DeviationsForegroundConverter : IValueConverter
    {
        private SolidColorBrush[] _deviationBrushesList;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            _deviationBrushesList ??= (SolidColorBrush[])Application.Current.TryFindResource("DeviationForegroundBrushes");

            DataCell dataCell = (DataCell)value;

            if (dataCell == null || float.IsNaN(dataCell.DefaultValue) || dataCell.Sample < Shared.MinDeviationSample)
                return null;

            if (dataCell is ValueCell or EvCell)
                return null;

            float returnValue = float.NaN;

            if (!float.IsNaN(dataCell.GtoValue))
                returnValue = dataCell.GtoValue;
            else if (!float.IsNaN(dataCell.DefaultValue))
                returnValue = dataCell.DefaultValue;

            if (float.IsNaN(returnValue))
                return null;

            if (_deviationBrushesList == null)
                throw new Exception("Deviation brushes were not found");

            if (_deviationBrushesList.Length % 2 != 0)
                throw new Exception("Deviation brushes list contains odd number of elements");

            float calculatedValue = dataCell.CalculatedValue;

            float deviation = calculatedValue - returnValue;

            bool straightOrder = deviation >= 0 ? dataCell.RevertColors : !dataCell.RevertColors;

            int index = (int)Math.Abs(deviation) / 2;

            if (index >= _deviationBrushesList.Length / 2)
                index = _deviationBrushesList.Length / 2 - 1;

            return straightOrder ? _deviationBrushesList[index] : _deviationBrushesList[new Index(index + 1, true)];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DeviationsRectBrushConverter : IValueConverter
    {
        private SolidColorBrush[] _deviationBrushesList;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            _deviationBrushesList ??= (SolidColorBrush[])Application.Current.TryFindResource("DeviationBackgroundBrushes");

            DataCell dataCell = (DataCell)value;

            if (dataCell == null || float.IsNaN(dataCell.DefaultValue) || dataCell.Sample < Shared.MinDeviationSample)
                return null;

            if (dataCell is ValueCell or EvCell)
                return null;

            float returnValue = float.NaN;

            if (!float.IsNaN(dataCell.GtoValue))
                returnValue = dataCell.GtoValue;
            else if (!float.IsNaN(dataCell.DefaultValue))
                returnValue = dataCell.DefaultValue;

            if (float.IsNaN(returnValue))
                return null;

            if (_deviationBrushesList == null)
                throw new Exception("Deviation brushes were not found");

            if (_deviationBrushesList.Length % 2 != 0)
                throw new Exception("Deviation brushes list contains odd number of elements");

            float calculatedValue = dataCell.CalculatedValue;

            float deviation = calculatedValue - returnValue;

            bool straightOrder = deviation >= 0 ? dataCell.RevertColors : !dataCell.RevertColors;

            int index = (int)Math.Abs(deviation) / 2;

            if (index >= _deviationBrushesList.Length / 2)
                index = _deviationBrushesList.Length / 2 - 1;

            return straightOrder ? _deviationBrushesList[index] : _deviationBrushesList[new Index(index + 1, true)];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DeviationsTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DataCell dataCell = (DataCell)value;

            if (dataCell == null || dataCell.Sample < Shared.MinDeviationSample)
                return null;

            if (dataCell is ValueCell or EvCell)
                return null;

            float returnValue = float.NaN;

            if (!float.IsNaN(dataCell.GtoValue))
                returnValue = dataCell.GtoValue;
            else if (!float.IsNaN(dataCell.DefaultValue))
                returnValue = dataCell.DefaultValue;

            return float.IsNaN(returnValue) ? null : Math.Round(returnValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EvCellForegroundConverter : IValueConverter
    {
        private SolidColorBrush[] _evBrushesList;
        private SolidColorBrush _evDefaultBrush;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            _evBrushesList ??= (SolidColorBrush[])Application.Current.TryFindResource("DeviationForegroundBrushes");
            _evDefaultBrush ??= (SolidColorBrush)Application.Current.TryFindResource("DefaultGeneralForegroundBrush");

            DataCell dataCell = (DataCell)value;

            if (dataCell is not EvCell)
                return null;

            if (_evBrushesList == null)
                throw new Exception("Ev brushes were not found");

            if (_evBrushesList.Length % 2 != 0)
                throw new Exception("Ev brushes list contains odd number of elements");

            if (dataCell.Sample == 0)
                return _evDefaultBrush;

            bool straightOrder = dataCell.CalculatedValue > 0;

            int index = (int)(Math.Abs(dataCell.CalculatedValue) / 0.3);

            if (index >= _evBrushesList.Length / 2)
                index = _evBrushesList.Length / 2 - 1;

            return straightOrder ? _evBrushesList[index] : _evBrushesList[new Index(index + 1, true)];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EvCellBackgroundConverter : IValueConverter
    {
        private SolidColorBrush[] _evBrushesList;
        private SolidColorBrush _evDefaultBrush;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            _evBrushesList ??= (SolidColorBrush[])Application.Current.TryFindResource("DeviationBackgroundBrushes");
            _evDefaultBrush ??= (SolidColorBrush)Application.Current.TryFindResource("DefaultGeneralBackgroundBrush");

            DataCell dataCell = (DataCell)value;

            if (dataCell is not EvCell)
                return null;

            if (_evBrushesList == null)
                throw new Exception("Ev brushes were not found");

            if (_evBrushesList.Length % 2 != 0)
                throw new Exception("Ev brushes list contains odd number of elements");

            if (dataCell.Sample == 0)
                return _evDefaultBrush;

            bool straightOrder = dataCell.CalculatedValue > 0;

            int index = (int)(Math.Abs(dataCell.CalculatedValue) / 0.3);

            if (index >= _evBrushesList.Length / 2)
                index = _evBrushesList.Length / 2 - 1;

            return straightOrder ? _evBrushesList[index] : _evBrushesList[new Index(index + 1, true)];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FourBetRangeCellConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string defaultValue = "---";

            if (values[0] is DataCell dataCell1 && values[1] is DataCell dataCell2)
            {
                if (dataCell1.Sample > 0 && dataCell2.Sample > 0)
                    defaultValue = $"{Math.Round(dataCell1.CalculatedValue * dataCell2.CalculatedValue / 100, 1).ToString(CultureInfo.InvariantCulture)}% range";
            }

            return defaultValue;
        }

        public object[] ConvertBack(object values, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
