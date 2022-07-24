using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InfoHelper.StatsEntities
{
    public static class CellsManager
    {
        public static void GetStatsSets()
        {
            using FileStream fs = File.Open("cell_sets.csv", FileMode.Open);

            StreamReader sr = new StreamReader(fs);

            string title = sr.ReadLine();

            string[] headerParts = title.Split(';');

            Dictionary<string, List<DataCell>> cellGroups = new Dictionary<string, List<DataCell>>();

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

                if (!cellGroups.ContainsKey(key))
                    cellGroups[key] = new List<DataCell>();

                List<DataCell> cellGroup = cellGroups[key];

                Type type = Type.GetType($"{typeof(DataCell).Namespace}.{record[1]}");

                DataCell dataCell = (DataCell)Activator.CreateInstance(type, record[2], record[3]);

                cellGroup.Add(dataCell);
            }

            foreach (string[] record in records)
            {
                List<DataCell> cellGroup = cellGroups[record[0]];

                DataCell dataCell = cellGroup.First(c => c.Name == record[2]);

                string[] connectedCellsNames = record.TakeLast(2).Where(c => c != string.Empty).ToArray();

                if(connectedCellsNames.Length == 0)
                    continue;

                DataCell[] connectedCells = connectedCellsNames.Select(cc => cellGroup.First(c => c.Name == cc)).ToArray();

                dataCell.ConnectedCells = connectedCells;
            }
        }

        public static StatsCell[] GetPreflopCells(Gametype gameType, Position position)
        {
            List<StatsCell> cells = new List<StatsCell>();

            if (gameType == Gametype.SixMax)
            {
                if (position == Position.Sb)
                {
                    return null;
                }
                else if (position == Position.Bb)
                {
                    return null;
                }
                else if (position == Position.Ep)
                {
                    return null;
                }
                else if (position == Position.Mp)
                {
                    return null;
                }
                else if (position == Position.Co)
                {
                    return null;
                }
                else if (position == Position.Btn)
                {
                    //cells.Add(new StatsCell("Btn_Unopened_Fold"));
                    //cells.Add(new StatsCell("Btn_Unopened_Call"));
                    //cells.Add(new StatsCell("Btn_Unopened_Raise"));

                    //cells.Add(new StatsCell("Btn_VsLimp_Fold"));
                    //cells.Add(new StatsCell("Btn_VsLimp_Call"));
                    //cells.Add(new StatsCell("Btn_VsLimp_Raise"));

                    //cells.Add(new StatsCell("Btn_VsLimp_VsEp_Fold"));
                    //cells.Add(new StatsCell("Btn_VsLimp_VsEp_Call"));
                    //cells.Add(new StatsCell("Btn_VsLimp_VsEp_Raise"));

                    //cells.Add(new StatsCell("Btn_VsLimp_VsMp_Fold"));
                    //cells.Add(new StatsCell("Btn_VsLimp_VsMp_Call"));
                    //cells.Add(new StatsCell("Btn_VsLimp_VsMp_Raise"));

                    //cells.Add(new StatsCell("Btn_VsLimp_VsCo_Fold"));
                    //cells.Add(new StatsCell("Btn_VsLimp_VsCo_Call"));
                    //cells.Add(new StatsCell("Btn_VsLimp_VsCo_Raise"));

                    //cells.Add(new StatsCell("Btn_VsLimp_Multi_Fold"));
                    //cells.Add(new StatsCell("Btn_VsLimp_Multi_Call"));
                    //cells.Add(new StatsCell("Btn_VsLimp_Multi_Raise"));

                    //cells.Add(new StatsCell("Btn_VsRaise_Fold"));
                    //cells.Add(new StatsCell("Btn_VsRaise_Call"));
                    //cells.Add(new StatsCell("Btn_VsRaise_Raise"));

                    //cells.Add(new StatsCell("Btn_VsRaise_VsEp_Fold"));
                    //cells.Add(new StatsCell("Btn_VsRaise_VsEp_Call"));
                    //cells.Add(new StatsCell("Btn_VsRaise_VsEp_Raise"));

                    //cells.Add(new StatsCell("Btn_VsRaise_VsMp_Fold"));
                    //cells.Add(new StatsCell("Btn_VsRaise_VsMp_Call"));
                    //cells.Add(new StatsCell("Btn_VsRaise_VsMp_Raise"));

                    //cells.Add(new StatsCell("Btn_VsRaise_VsCo_Fold"));
                    //cells.Add(new StatsCell("Btn_VsRaise_VsCo_Call"));
                    //cells.Add(new StatsCell("Btn_VsRaise_VsCo_Raise"));

                    //cells.Add(new StatsCell("Btn_VsRaise_Multi_Fold"));
                    //cells.Add(new StatsCell("Btn_VsRaise_Multi_Call"));
                    //cells.Add(new StatsCell("Btn_VsRaise_Multi_Raise"));

                    //cells.Add(new StatsCell("Btn_VsIsolateCc_Fold"));
                    //cells.Add(new StatsCell("Btn_VsIsolateCc_Call"));
                    //cells.Add(new StatsCell("Btn_VsIsolateCc_Raise"));

                    //cells.Add(new StatsCell("Btn_VsIsolateAsLimper_Fold"));
                    //cells.Add(new StatsCell("Btn_VsIsolateAsLimper_Call"));
                    //cells.Add(new StatsCell("Btn_VsIsolateAsLimper_Raise"));

                    //cells.Add(new StatsCell("Btn_VsIsolateAsLimper_Hu_Fold"));
                    //cells.Add(new StatsCell("Btn_VsIsolateAsLimper_Hu_Call"));
                    //cells.Add(new StatsCell("Btn_VsIsolateAsLimper_Hu_Raise"));

                    //cells.Add(new StatsCell("Btn_VsIsolateAsLimper_Multi_Fold"));
                    //cells.Add(new StatsCell("Btn_VsIsolateAsLimper_Multi_Call"));
                    //cells.Add(new StatsCell("Btn_VsIsolateAsLimper_Multi_Raise"));
                }
            }
            else if (gameType == Gametype.Hu)
            {
                if ((position & Position.Sb) == Position.Sb)
                {
                    return null;
                }
                else if (position == Position.Bb)
                {
                    return null;
                }
            }

            return cells.ToArray();
        }
    }
}
