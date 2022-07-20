namespace InfoHelper.ViewModel.States
{
    public class ViewModelHudsParent
    {
        public ViewModelActionsState ActionsState { get; } = new ViewModelActionsState();

        public ViewModelNameState NameState { get; } = new ViewModelNameState();

        public ViewModelStatsHud GeneralHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud SbPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud BbPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud EpPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud MpPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud CoPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud BtnPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud SbvsBbPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud BbvsSbPreflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud AggressorIpPostflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud AggressorOopPostflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud CallerIpPostflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud CallerOopPostflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud FlopMultiwayPostflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud TurnMultiwayPostflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelStatsHud RiverMultiwayPostflopHudState { get; } = new ViewModelStatsHud();

        public ViewModelPreflopMatrixState PreflopMatrixState { get; } = new ViewModelPreflopMatrixState();

        public ViewModelPostlopHandsTableState PostflopHandsPanelState { get; } = new ViewModelPostlopHandsTableState();
    }
}
