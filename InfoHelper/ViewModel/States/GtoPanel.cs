using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HashUtils;
using InfoHelper.ViewModel.DataEntities;

namespace InfoHelper.ViewModel.States
{
    public class ViewModelGtoState : ViewModelDeferredBindableState
    {
        public GtoInfo GtoInfo { get; set; }

        public override void UpdateBindings()
        {
            string hashString = $"{Visible}{GtoInfo?.Round}{GtoInfo?.Title}{GtoInfo?.PocketRender}";

            int hashCode = hashString.GetStableHashCode();

            if (hashCode != HashCode)
                base.UpdateBindings();

            HashCode = hashCode;
        }
    }
}
