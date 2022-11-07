using HashUtils;
using InfoHelper.StatsEntities;

namespace InfoHelper.ViewModel.States
{
    public class ViewModelPostlopHandsTableState : ViewModelDeferredBindableHeaderedState
    {
        public HandsGroup HandsGroup { get; set; }

        public override void UpdateBindings()
        {
            string hashString = $"{Visible}{Header ?? string.Empty}";

            if (HandsGroup != null)
            {
                for (int i = 0; i < HandsGroup.MadeHands.Length; i++)
                    hashString += $"{HandsGroup.MadeHands[i]}{HandsGroup.DrawHands[i]}";
            }

            int hashCode = hashString.GetStableHashCode();

            if (hashCode != HashCode)
                base.UpdateBindings();

            HashCode = hashCode;
        }
    }
}
