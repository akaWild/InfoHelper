using System.Windows;
using InfoHelper.ViewModel.DataEntities;

namespace InfoHelper.ViewModel.States
{
    public class ViewModelWindowsInfoState : ViewModelDeferredBindableState
    {
        public WindowInfo[] WinInfos{ get; set; }
    }
}
