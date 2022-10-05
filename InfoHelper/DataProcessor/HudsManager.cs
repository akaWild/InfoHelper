using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameInformationUtility;
using InfoHelper.ViewModel.DataEntities;

namespace InfoHelper.ViewModel.States
{
    public class HudsManager
    {
        private readonly ViewModelMain _vmMain;

        public HudsManager(ViewModelMain vmMain)
        {
            _vmMain = vmMain;
        }

        public void UpdateHuds(GameContext gc)
        {
            _vmMain.AnalyzerInfoState.Info = gc.Error;

            for (int i = 0; i < gc.Players.Length; i++)
            {
                if (gc.Players[i] == null)
                    _vmMain.HudsParentStates[i].NameState.Visible = false;
                else
                {
                    _vmMain.HudsParentStates[i].NameState.Visible = true;

                    _vmMain.HudsParentStates[i].NameState.Name = gc.Players[i];

                    _vmMain.HudsParentStates[i].NameState.IsConfirmed = gc.IsPlayerConfirmed[i];
                }

                _vmMain.HudsParentStates[i].NameState.UpdateBindings();
            }

            _vmMain.GtoParentState.Error = gc.Error != string.Empty ? null : gc.GtoError;

            _vmMain.GtoParentState.IsSolverRunning = gc.Error == string.Empty && gc.IsSolving;

            _vmMain.GtoParentState.GtoState.Visible = gc.Error == string.Empty && gc.GtoError == null && !gc.IsSolving;

            _vmMain.GtoParentState.GtoState.GtoInfo = _vmMain.GtoParentState.GtoState.Visible ? (GtoInfo)gc.GtoData : null;

            _vmMain.GtoParentState.GtoState.UpdateBindings();
        }

        public void UpdateWindows(WindowInfo[] winInfos)
        {
            _vmMain.WindowsInfoState.WinInfos = winInfos;

            _vmMain.WindowsInfoState.UpdateBindings();
        }

        public void ResetControls()
        {
            _vmMain.AnalyzerInfoState.Info = null;

            foreach (ViewModelHudsParent vmHudsParent in _vmMain.HudsParentStates)
            {
                vmHudsParent.NameState.Visible = false;

                vmHudsParent.NameState.UpdateBindings();
            }

            _vmMain.GtoParentState.Error = string.Empty;

            _vmMain.GtoParentState.IsSolverRunning = false;

            _vmMain.GtoParentState.GtoState.Visible = false;

            _vmMain.GtoParentState.GtoState.UpdateBindings();
        }

        public void ResetWindowsPanel()
        {
            _vmMain.WindowsInfoState.WinInfos = null;

            _vmMain.WindowsInfoState.UpdateBindings();
        }
    }
}
