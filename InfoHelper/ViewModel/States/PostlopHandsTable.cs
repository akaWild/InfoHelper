using System.Text;
using HashUtils;
using InfoHelper.StatsEntities;
using StatUtility;

namespace InfoHelper.ViewModel.States
{
    public class ViewModelPostlopHandsTableState : ViewModelDeferredBindableHeaderedState
    {
        public PostflopHandsGroup PostflopHandsGroup { get; set; }

        public int Round { get; set; }

        public override void UpdateBindings()
        {
            string hashString = $"{Visible}{Round}{Header ?? string.Empty}{PostflopHandsGroup?.MadeHandsAccumulatedEquity}{PostflopHandsGroup?.DrawHandsAccumulatedEquity}";

            int hashCode = hashString.GetStableHashCode();

            if (hashCode != HashCode)
                base.UpdateBindings();

            HashCode = hashCode;
        }
    }
}
