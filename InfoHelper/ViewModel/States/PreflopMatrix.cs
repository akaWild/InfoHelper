using HashUtils;
using InfoHelper.StatsEntities;
using StatUtility;

namespace InfoHelper.ViewModel.States
{
    public class ViewModelPreflopMatrixState : ViewModelDeferredBindableHeaderedState
    {
        public PreflopHandsGroup PreflopHandsGroup { get; set; }

        public override void UpdateBindings()
        {
            string hashString = $"{Visible}{Header ?? string.Empty}";

            if (PreflopHandsGroup != null)
            {
                for (int i = 0; i < 169; i++)
                    hashString += $"{PreflopHandsGroup.PocketHands[i]}";
            }

            int hashCode = hashString.GetStableHashCode();

            if (hashCode != HashCode)
                base.UpdateBindings();

            HashCode = hashCode;
        }
    }
}
