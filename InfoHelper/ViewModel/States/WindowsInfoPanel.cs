using System;
using System.Windows;
using HashUtils;
using InfoHelper.ViewModel.DataEntities;

namespace InfoHelper.ViewModel.States
{
    public class ViewModelWindowsInfoState : ViewModelDeferredBindableState
    {
        public WindowInfo[] WinInfos { get; set; }

        public override void UpdateBindings()
        {
            string hashString = $"{Visible}";

            if (WinInfos != null)
            {
                Array.Sort(WinInfos);

                foreach (WindowInfo window in WinInfos)
                    hashString += $"{window.Rectangle}{window.WindowState}{window.IsHeroActing}";
            }

            int hashCode = hashString.GetStableHashCode();

            if (hashCode != HashCode)
                base.UpdateBindings();

            HashCode = hashCode;
        }
    }
}
