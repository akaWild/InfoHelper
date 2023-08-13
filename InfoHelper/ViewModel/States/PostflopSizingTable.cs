using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HashUtils;

namespace InfoHelper.ViewModel.States
{
    public class PostflopSizingTableState : ViewModelDeferredBindableState
    {
        public float LowBound { get; set; }

        public float UpperBound { get; set; }

        public int Sample { get; set; }

        public float Ev { get; set; }

        public float EvDelta { get; set; }

        public override void UpdateBindings()
        {
            string hashString = $"{Visible}{LowBound}{UpperBound}{Sample}{Ev}{EvDelta}";

            int hashCode = hashString.GetStableHashCode();

            if (hashCode != HashCode)
                base.UpdateBindings();

            HashCode = hashCode;
        }
    }
}
