using HashUtils;

namespace InfoHelper.ViewModel.States
{
    public class ViewModelActionsState : ViewModelDeferredBindableState
    {
        public string Actions { get; set; }

        public override void UpdateBindings()
        {
            string hashString = $"{Visible}{Actions}";

            int hashCode = hashString.GetStableHashCode();

            if (hashCode != HashCode)
                base.UpdateBindings();

            HashCode = hashCode;
        }
    }
}
