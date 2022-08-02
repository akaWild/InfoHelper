using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using InfoHelper.StatsEntities;
using InfoHelper.ViewModel.States;
using InfoHelper.Windows;

namespace InfoHelper.DataProcessor
{
    public class Controller
    {
        private readonly ViewModelMain _mainWindowState;

        private readonly SettingsManager _settingsManager;

        public Controller(ViewModelMain window)
        {
            _mainWindowState = window;

            _mainWindowState.ControlsState.ExitRequested += ControlsState_ExitRequested;

            _mainWindowState.ControlsState.ShowOptionsRequested += ControlsState_ShowOptionsRequested; ;

            _settingsManager = new SettingsManager();

            StatsManager.LoadCells();

            if (!_settingsManager.RetrieveSettings())
                _mainWindowState.ControlsState.SetError(_settingsManager.Error, ErrorType.Settings);
        }

        private void ControlsState_ShowOptionsRequested(object sender, EventArgs e)
        {
            OptionsWindow optionsWindow = new OptionsWindow();

            if ((bool)optionsWindow.ShowDialog())
            {
                bool retrieveSettingsResult = _settingsManager.RetrieveSettings();

                if(retrieveSettingsResult)
                    _mainWindowState.ControlsState.ResetError();
                else 
                    _mainWindowState.ControlsState.SetError(_settingsManager.Error, ErrorType.Settings);
            }
        }

        private void ControlsState_ExitRequested(object sender, EventArgs e)
        {
            Application.Current.Shutdown(0);
        }
    }
}
