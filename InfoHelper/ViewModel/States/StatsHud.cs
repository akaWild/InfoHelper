using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HashUtils;
using InfoHelper.StatsEntities;

namespace InfoHelper.ViewModel.States
{
    public class ViewModelStatsHud : ViewModelDeferredBindableState
    {
        private readonly Dictionary<string, DataCell> _cells = new Dictionary<string, DataCell>();

        private readonly Dictionary<string, bool> _rowsVisible = new Dictionary<string, bool>();

        public string PlayerName { get; set; }

        public string SetName { get; set; }

        public SetType SetType { get; set; }

        public static string SelectedCell { get; set; }

        public DataCell this[string name] => !_cells.ContainsKey(name) ? null : _cells[name];

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

                object cellCopy = dc?.ShallowCopy;

                result = cellCopy;

                return valueFound;
            }
        }

        public void SetData(DataCell[] cells)
        {
            if (cells == null)
                return;

            _cells.Clear();

            foreach (DataCell cell in cells)
            {
                _cells[cell.Name] = cell;

                _cells[cell.Name].CellSelectedState = CellSelectedState.NotSelected;
            }
        }

        public void SetRows(string[] rows)
        {
            if (rows == null)
                return;

            _rowsVisible.Clear();

            foreach (string row in rows)
                _rowsVisible[row] = true;
        }

        public override void UpdateBindings()
        {
            string selectedCell = null;

            if (SelectedCell != null)
                selectedCell = !_cells.ContainsKey(SelectedCell) ? null : _cells[SelectedCell].Name;

            string hashString = $"{Visible}{PlayerName}{SetName}{selectedCell}";

            foreach (var kv in _rowsVisible)
                hashString += $"{kv.Key}";

            int hashCode = hashString.GetStableHashCode();

            if (hashCode != HashCode)
                base.UpdateBindings();

            HashCode = hashCode;
        }
    }
}
