using System.Text;
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

            StringBuilder sb = new StringBuilder();

            if (HandsGroup != null)
            {
                for (int i = 0; i < HandsGroup.HandCategoriesCount; i++)
                    sb.Append($"{HandsGroup.MadeHands[i]}{HandsGroup.DrawHands[i]}{HandsGroup.ComboHands[i]}");
            }

            hashString += sb.ToString();

            int hashCode = hashString.GetStableHashCode();

            if (hashCode != HashCode)
                base.UpdateBindings();

            HashCode = hashCode;
        }
    }
}
