namespace InfoHelper.ViewModel.States
{
    public class ViewModelProgressBarState : ViewModelStateBase
    {
        private double _minValue;
        public double MinValue
        {
            get => _minValue;
            set
            {
                _minValue = value;

                OnPropertyChanged();
            }
        }

        private double _maxValue;
        public double MaxValue
        {
            get => _maxValue;
            set
            {
                _maxValue = value;

                OnPropertyChanged();
            }
        }

        private double _value;
        public double Value
        {
            get => _value;
            set
            {
                _value = value;

                OnPropertyChanged();
            }
        }
    }
}
