namespace InfoHelper.ViewModel.States
{
    public class ViewModelNameState : ViewModelDeferredBindableState
    {
        public string Name { get; set; }

        public bool IsConfirmed { get; set; }
    }
}
