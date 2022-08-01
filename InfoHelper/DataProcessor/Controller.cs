﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using InfoHelper.StatsEntities;
using InfoHelper.ViewModel.States;

namespace InfoHelper.DataProcessor
{
    public class Controller
    {
        private readonly ViewModelMain _mainWindowState;

        public Controller(ViewModelMain window)
        {
            _mainWindowState = window;

            _mainWindowState.ControlsState.ExitRequested += ControlsState_ExitRequested;

            _mainWindowState.ControlsState.ShowOptionsRequested += ControlsState_ShowOptionsRequested; ;

            StatsManager.LoadCells();
        }

        private void ControlsState_ShowOptionsRequested(object sender, EventArgs e)
        {
            OptionsWindow optionsWindow = new OptionsWindow();

            if ((bool)optionsWindow.ShowDialog())
            {
                _mainWindowState.ControlsState.SetError("Settings not found", ErrorType.Settings);
            }
            else
            {
                _mainWindowState.ControlsState.SetError("", ErrorType.Critical);
            }
        }

        private void ControlsState_ExitRequested(object sender, EventArgs e)
        {
            Application.Current.Shutdown(0);
        }
    }
}
