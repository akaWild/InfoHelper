using HashUtils;

namespace InfoHelper.ViewModel.States
{
    public class ViewModelNameState : ViewModelDeferredBindableState
    {
        public string Name { get; set; }

        public bool IsConfirmed { get; set; }

        public override void UpdateBindings()
        {
            string hashString = $"{Visible}{Name}{IsConfirmed}";

            int hashCode = hashString.GetStableHashCode();

            if (hashCode != HashCode)
                base.UpdateBindings();

            HashCode = hashCode;
        }
    }
}
