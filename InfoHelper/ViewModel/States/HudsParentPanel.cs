﻿namespace InfoHelper.ViewModel.States
{
    public class ViewModelHudsParent
    {
        public ViewModelActionsState ActionsState { get; } = new ViewModelActionsState();

        public ViewModelNameState NameState { get; } = new ViewModelNameState();

        public ViewModelStatsHud PreflopGeneralHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud SbPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud BbPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud EpPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud MpPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud CoPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud BtnPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud SbvsBbPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud BbvsSbPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud PostflopHuIpHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud PostflopHuOopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud PostflopGeneralHudState { get; } = new ViewModelStatsHud();

        public ViewModelPreflopMatrixState PreflopMatrixState { get; } = new ViewModelPreflopMatrixState();

        public ViewModelPreflopMatrixState PreflopMatrixAltState { get; } = new ViewModelPreflopMatrixState();

        public ViewModelPostlopHandsTableState PostflopHandsPanelState { get; } = new ViewModelPostlopHandsTableState();
    }
}
