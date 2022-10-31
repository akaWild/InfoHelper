using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InfoHelper.StatsEntities;

namespace InfoHelper.DataProcessor
{
    public static class StatsManager
    {
        private static readonly Dictionary<string, List<DataCell>> CellGroups = new Dictionary<string, List<DataCell>>();

        public static void LoadCells()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\StatsCsvFiles\\cell_sets.csv");

            if(!File.Exists(filePath))
                throw new Exception("File \"cell_sets.csv\" doesn't exist in Resources\\StatsCsvFiles directory");

            using FileStream fs = File.Open(filePath, FileMode.Open);

            StreamReader sr = new StreamReader(fs);

            string title = sr.ReadLine();

            if(title == null)
                throw new Exception("File \"cell_sets.csv\" doesn't contain header");

            string[] headerParts = title.Split(';');

            if (headerParts.Length != 7)
                throw new Exception("Header row in file \"cell_sets.csv\" contains less or more than 7 elements");

            List<string[]> records = new List<string[]>();

            while (sr.Peek() >= 0)
            {
                string line = sr.ReadLine();

                string[] parts = line.Split(';');

                string[] record = parts.ToArray();

                records.Add(record);
            }

            foreach (string[] record in records)
            {
                string key = record[0];

                if (!CellGroups.ContainsKey(key))
                    CellGroups[key] = new List<DataCell>();

                List<DataCell> cellGroup = CellGroups[key];

                Type cellType = Type.GetType($"{typeof(DataCell).Namespace}.{record[1]}");

                if(cellType == null)
                    throw new Exception($"{record[1]} cell type was not found");

                DataCell cell = (DataCell)Activator.CreateInstance(cellType, record[3], record[4]);

                if(cell == null)
                    throw new Exception($"Failed to create instance of type {cellType.Name}");

                if (record[2] != string.Empty)
                {
                    Type dataCellType = Type.GetType($"{typeof(DataCell).Namespace}.{record[2]}");

                    if (dataCellType == null)
                        throw new Exception($"{record[2]} cell data type was not found");

                    object cellData = Activator.CreateInstance(dataCellType);

                    if (cellData == null)
                        throw new Exception($"Failed to create instance of type {dataCellType.Name}");

                    cell.CellData = cellData;
                }

                if (cellGroup.Count(c => c.Name == cell.Name) != 0)
                    throw new Exception($"Cell {cell.Name} has been already added");

                cellGroup.Add(cell);
            }

            foreach (string[] record in records)
            {
                List<DataCell> cellGroup = CellGroups[record[0]];

                DataCell dataCell = cellGroup.First(c => c.Name == record[3]);

                string[] connectedCellsNames = record.TakeLast(2).Where(c => c != string.Empty).ToArray();

                if(connectedCellsNames.Length == 0)
                    continue;

                DataCell[] connectedCells = connectedCellsNames.Select(cc => cellGroup.FirstOrDefault(c => c.Name == cc)).ToArray();

                if(connectedCells.Any(cc => cc == null))
                    throw new Exception($"Connected cells of cell {dataCell.Name} contain invalid cell name");

                if (connectedCells.Length == 2)
                {
                    if(connectedCells[0].Name == connectedCells[1].Name)
                        throw new Exception($"Connected cells of cell {dataCell.Name} are identical");
                }

                if (connectedCells.Any(cc => cc.Name == dataCell.Name))
                    throw new Exception($"Connected cells of cell {dataCell.Name} contain parent cell name");

                dataCell.ConnectedCells = connectedCells;
            }
        }

        public static DataCell[] GetPanelCells(string panelName)
        {
            if (!CellGroups.ContainsKey(panelName))
                throw new Exception($"{panelName} panel was not found in dictionary");

            return CellGroups[panelName].ToArray();
        }
    }
}
