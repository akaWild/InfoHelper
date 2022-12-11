using System.Text;
using HashUtils;
using InfoHelper.StatsEntities;
using StatUtility;

namespace InfoHelper.ViewModel.States
{
    public class ViewModelPostlopHandsTableState : ViewModelDeferredBindableHeaderedState
    {
        public PostflopHandsGroup PostflopHandsGroup { get; set; }

        public override void UpdateBindings()
        {
            string hashString = $"{Visible}{Header ?? string.Empty}";

            StringBuilder sb = new StringBuilder();

            if (PostflopHandsGroup != null)
            {
                for (int i = 0; i < PostflopHandsGroup.HandCategoriesCount; i++)
                    sb.Append($"{PostflopHandsGroup.MadeHands[i]}{PostflopHandsGroup.DrawHands[i]}{PostflopHandsGroup.ComboHands[i]}");
            }

            hashString += sb.ToString();

            int hashCode = hashString.GetStableHashCode();

            if (hashCode != HashCode)
                base.UpdateBindings();

            HashCode = hashCode;
        }
    }
}
