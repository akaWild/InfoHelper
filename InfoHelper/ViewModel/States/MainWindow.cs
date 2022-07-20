namespace InfoHelper.ViewModel.States
{
    public class ViewModelMain
    {
        public ViewModelControlsState ControlsState { get; } = new ViewModelControlsState();

        public ViewModelWindowsInfoState WindowsInfoState { get; } = new ViewModelWindowsInfoState();

        public ViewModelGtoParentState GtoParentState { get; } = new ViewModelGtoParentState();

        public ViewModelHudsParent[] HudsParentStates { get; } = new ViewModelHudsParent[5]
        {
            new ViewModelHudsParent(),
            new ViewModelHudsParent(),
            new ViewModelHudsParent(),
            new ViewModelHudsParent(),
            new ViewModelHudsParent()
        };
    }
}
