using HashUtils;
using InfoHelper.StatsEntities;

namespace InfoHelper.ViewModel.States
{
    public class ViewModelPreflopMatrixState : ViewModelDeferredBindableHeaderedState
    {
        public PreflopData PreflopData { get; set; }

        public override void UpdateBindings()
        {
            string hashString = $"{Visible}{Header ?? string.Empty}";

            if (PreflopData != null)
            {
                for (int i = 0; i < 169; i++)
                    hashString += $"{PreflopData[i]}";
            }

            int hashCode = hashString.GetStableHashCode();

            if (hashCode != HashCode)
                base.UpdateBindings();

            HashCode = hashCode;
        }
    }
}
