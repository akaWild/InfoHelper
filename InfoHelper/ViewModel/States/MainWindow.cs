using System.Windows.Threading;

namespace InfoHelper.ViewModel.States
{
    public class ViewModelMain
    {
        public ViewModelControlsState ControlsState { get; }

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

        public ViewModelMain(Dispatcher dispatcher)
        {
            ControlsState = new ViewModelControlsState(dispatcher);
        }
    }
}
