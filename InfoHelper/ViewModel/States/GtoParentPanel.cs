namespace InfoHelper.ViewModel.States
{
    public class ViewModelGtoParentState
    {
        public string Error { get; set; }

        public bool IsSolverRunning { get; set; }

        public ViewModelPreflopGtoState PreflopGtoState { get; } = new ViewModelPreflopGtoState();

        public ViewModelPostflopGtoState PostflopGtoState { get; } = new ViewModelPostflopGtoState();
    }
}
