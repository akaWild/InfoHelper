using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfoHelper.StatsEntities;

namespace InfoHelper.ViewModel.States
{
    public class ViewModelStatsHud : ViewModelDeferredBindableState
    {
        private readonly Dictionary<string, DataCell> _cells = new Dictionary<string, DataCell>();

        private readonly Dictionary<string, bool> _rowsVisible = new Dictionary<string, bool>();

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (binder.Name.EndsWith("Row"))
            {
                result = _rowsVisible.ContainsKey(binder.Name);

                return true;
            }
            else
            {
                bool valueFound = _cells.TryGetValue(binder.Name, out DataCell dc);
                result = dc;

                return valueFound;
            }
        }

        public void SetData(DataCell[] cells)
        {
            if (cells == null)
                return;

            _cells.Clear();

            foreach (DataCell cell in cells)
                _cells[cell.Name] = cell;
        }

        public void SetRows(string[] rows)
        {
            if (rows == null)
                return;

            _rowsVisible.Clear();

            foreach (string row in rows)
                _rowsVisible[row] = true;
        }
    }
}
