using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfoHelper.ViewModel.DataEntities;

namespace InfoHelper.ViewModel.States
{
    public class ViewModelGtoState : ViewModelDeferredBindableState
    {
        public GtoInfo GtoInfo { get; set; }
    }
}
