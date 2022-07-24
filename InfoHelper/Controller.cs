using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using InfoHelper.ViewModel.States;

namespace InfoHelper
{
    public class Controller
    {
        private readonly ViewModelMain _mainWindowState;

        public Controller(ViewModelMain window)
        {
            _mainWindowState = window;

            _mainWindowState.ControlsState.ExitRequested += ControlsState_ExitRequested;
        }

        private void ControlsState_ExitRequested(object sender, EventArgs e)
        {
            Application.Current.Shutdown(0);
        }
    }
}
