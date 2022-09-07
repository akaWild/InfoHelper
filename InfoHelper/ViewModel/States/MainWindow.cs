using System.Windows.Threading;
using InfoHelper.DataProcessor;

namespace InfoHelper.ViewModel.States
{
    public class ViewModelMain
    {
        public ViewModelControlsState ControlsState { get; }

        public ViewModelWindowsInfoState WindowsInfoState { get; } = new ViewModelWindowsInfoState();

        public ViewModelAnalyzerInfo AnalyzerInfoState { get; } = new ViewModelAnalyzerInfo();

        public ViewModelGtoParentState GtoParentState { get; } = new ViewModelGtoParentState();

        public ViewModelHudsParent[] HudsParentStates { get; } = new ViewModelHudsParent[6]
        {
            new ViewModelHudsParent(),
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
