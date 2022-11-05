using HashUtils;
using InfoHelper.StatsEntities;

namespace InfoHelper.ViewModel.States
{
    public class ViewModelPostlopHandsTableState : ViewModelDeferredBindableHeaderedState
    {
        public PostflopData PostflopData { get; set; }

        public override void UpdateBindings()
        {
            string hashString = $"{Visible}";

            if (PostflopData != null)
            {
                for (int i = 0; i < PostflopData.MainGroup.MadeHands.Length; i++)
                    hashString += $"{PostflopData.MainGroup.MadeHands[i]}{PostflopData.MainGroup.DrawHands[i]}";
            }

            int hashCode = hashString.GetStableHashCode();

            if (hashCode != HashCode)
                base.UpdateBindings();

            HashCode = hashCode;
        }
    }
}
