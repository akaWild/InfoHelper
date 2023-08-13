namespace InfoHelper.ViewModel.States
{
    public class ViewModelHudsParent
    {
        public ViewModelActionsState ActionsState { get; } = new ViewModelActionsState();

        public ViewModelNameState NameState { get; } = new ViewModelNameState();

        public ViewModelStatsHud GeneralHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud AggressionHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud IpRaiser4PreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud IpCaller4PreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud OopRaiser4PreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud OopCaller4PreflopHudState { get; } = new ViewModelStatsHud();

        public PostflopSizingTableState[] PostflopSizingTableStates { get; } = new PostflopSizingTableState[5]
        {
            new PostflopSizingTableState(),
            new PostflopSizingTableState(),
            new PostflopSizingTableState(),
            new PostflopSizingTableState(),
            new PostflopSizingTableState()
        };

        public ViewModelStatsHud SbPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud BbPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud EpPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud MpPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud CoPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud BtnPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud SbvsBbPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud BbvsSbPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud PostflopHuIpRaiserHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud PostflopHuIpCallerHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud PostflopHuOopRaiserHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud PostflopHuOopCallerHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud PostflopGeneralHudState { get; } = new ViewModelStatsHud();

        public ViewModelPreflopMatrixState PreflopMatrixState { get; } = new ViewModelPreflopMatrixState();

        public ViewModelPreflopMatrixState PreflopMatrixAltState { get; } = new ViewModelPreflopMatrixState();

        public ViewModelPostlopHandsTableState PostflopHandsPanelState { get; } = new ViewModelPostlopHandsTableState();

        public ViewModelPostlopHandsTableState PostflopHandsPanelAltState { get; } = new ViewModelPostlopHandsTableState();
    }
}
