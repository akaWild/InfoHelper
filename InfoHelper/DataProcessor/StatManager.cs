using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InfoHelper.StatsEntities;
using Timer = System.Timers.Timer;

namespace InfoHelper.DataProcessor
{
    public class StatManager
    {
        private bool _shouldStop = false;

        private readonly Dictionary<string, StatPlayer> _players = new Dictionary<string, StatPlayer>();

        private readonly object _playersLock = new object();

        private readonly Timer _timer;

        private static readonly Dictionary<string, List<DataCell>> CellGroups = new Dictionary<string, List<DataCell>>();

        private static readonly List<StatSet> StatSets = new List<StatSet>();

        private const int ExpireTimeout = 10;

        private const int TimerInterval = 10;

        public StatManager()
        {
            LoadCellSets();

            LoadStatSets();

            _timer = new Timer(TimerInterval * 1000) { AutoReset = false };

            _timer.Elapsed += (sender, args) =>
            {
                lock (_playersLock)
                {
                    List<string> playersToRemove = new List<string>();

                    foreach (var kv in _players)
                    {
                        if(DateTime.Now.Subtract(kv.Value.LastAccessTime) > TimeSpan.FromSeconds(ExpireTimeout))
                            playersToRemove.Add(kv.Key);
                    }

                    foreach (string player in playersToRemove)
                    {
                        StatPlayer statPlayer = _players[player];

                        _players.Remove(player);

                        statPlayer.Dispose();

                        GC.Collect();
                    }
                }

                if (!_shouldStop)
                    _timer.Enabled = true;
            };
        }

        public void Start()
        {
            _shouldStop = false;

            _timer.Start();
        }

        public void Stop()
        {
            _shouldStop = true;

            lock (_playersLock)
            {
                foreach (string player in _players.Keys)
                {
                    StatPlayer statPlayer = _players[player];

                    _players.Remove(player);

                    statPlayer.Dispose();
                }

                GC.Collect();
            }
        }

        public StatSet[] GetPlayer(string player)
        {
            StatPlayer output = null;

            lock (_playersLock)
            {
                if (!_players.ContainsKey(player))
                {
                    StatSet[] statSetsCopy = StatSets.Select(ss => (StatSet)ss.Clone()).ToArray();

                    Parallel.ForEach<StatSet>(statSetsCopy, statSet =>
                    {
                        DataCell[] cells = CellGroups[$"{statSet.SetType}"].ToArray();

                        DataCell[] cellsCopy = cells.Select(c => (DataCell)c.Clone()).ToArray();

                        for (int i = 0; i < cells.Length; i++)
                        {
                            if (cells[i].ConnectedCells == null)
                                continue;

                            DataCell[] connectedCellsCopy = cells[i].ConnectedCells.Select(c => cellsCopy.First(c1 => c1.Name == c.Name)).ToArray();

                            cellsCopy[i].ConnectedCells = connectedCellsCopy;
                        }

                        statSet.Cells = cellsCopy;
                    });

                    StatPlayer newPlayer = new StatPlayer() { StatSets = statSetsCopy };

                    _players[player] = newPlayer;
                }

                output = _players[player];

                output.LastAccessTime = DateTime.Now;
            }

            return output.StatSets;
        }

        private static void LoadCellSets()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\CsvFiles\\cell_sets.csv");

            if(!File.Exists(filePath))
                throw new Exception("File \"cell_sets.csv\" doesn't exist in Resources\\CsvFiles directory");

            using FileStream fs = File.Open(filePath, FileMode.Open);

            StreamReader sr = new StreamReader(fs);

            string title = sr.ReadLine();

            if(title == null)
                throw new Exception("File \"cell_sets.csv\" doesn't contain header");

            string[] headerParts = title.Split(';');

            if (headerParts.Length != 8)
                throw new Exception("Header row in file \"cell_sets.csv\" contains less or more than 8 elements");

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

                DataCell cell = (DataCell)Activator.CreateInstance(cellType, record[4], record[5]);

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

                    if (record[3] != string.Empty)
                    {
                        string[] betStrings = record[3].Split(",", StringSplitOptions.RemoveEmptyEntries);

                        ushort[] bets = new ushort[betStrings.Length];

                        for (int i = 0; i < betStrings.Length; i++)
                        {
                            if(!ushort.TryParse(betStrings[i], out ushort bet))
                                throw new Exception($"{record[3]} bet ranges line has incorrect format");

                            bets[i] = bet;
                        }

                        if(bets.Length > 3)
                            throw new Exception("The total number of bet values must be 3 or less");

                        Array.Sort(bets);

                        BetRange[] betRanges = new BetRange[bets.Length + 1];

                        ushort lowBound = 0;

                        for (int i = 0; i < bets.Length; i++)
                        {
                            if(bets[i] <= 0)
                                throw new Exception("Bet value must greater than 0");

                            betRanges[i] = new BetRange() { LowBound = lowBound, UpperBound = bets[i] };

                            lowBound = bets[i];
                        }

                        betRanges[^1] = new BetRange() { LowBound = bets[^1], UpperBound = ushort.MaxValue };

                        cell.BetRanges = betRanges;
                    }
                }

                if (cellGroup.Count(c => c.Name == cell.Name) != 0)
                    throw new Exception($"Cell {cell.Name} has been already added");

                cellGroup.Add(cell);
            }

            foreach (string[] record in records)
            {
                List<DataCell> cellGroup = CellGroups[record[0]];

                DataCell dataCell = cellGroup.First(c => c.Name == record[4]);

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

        private static void LoadStatSets()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\CsvFiles\\stat_sets.csv");

            if (!File.Exists(filePath))
                throw new Exception("File \"stat_sets.csv\" doesn't exist in Resources\\CsvFiles directory");

            using FileStream fs = File.Open(filePath, FileMode.Open);

            StreamReader sr = new StreamReader(fs);

            string title = sr.ReadLine();

            if (title == null)
                throw new Exception("File \"stat_sets.csv\" doesn't contain header");

            string[] headerParts = title.Split(';');

            if (headerParts.Length != 10)
                throw new Exception("Header row in file \"stat_sets.csv\" contains less or more than 10 elements");

            while (sr.Peek() >= 0)
            {
                string line = sr.ReadLine();

                string[] parts = line.Split(';');

                if(!Enum.TryParse(parts[0], out GameType gameType))
                    throw new Exception($"{parts[0]} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[1], out Round round))
                    throw new Exception($"{parts[1]} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[2], out Position position))
                    throw new Exception($"{parts[2]} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[3], out RelativePosition relativePosition))
                    throw new Exception($"{parts[3]} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[4], out Position oppPosition))
                    throw new Exception($"{parts[4]} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[5], out PlayersOnFlop playersOnFlop))
                    throw new Exception($"{parts[5]} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[6], out PreflopPotType preflopPotType))
                    throw new Exception($"{parts[6]} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[7], out PreflopActions preflopActions))
                    throw new Exception($"{parts[7]} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[8], out OtherPlayersActed otherPlayersActed))
                    throw new Exception($"{parts[8]} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[9], out SetType setType))
                    throw new Exception($"{parts[9]} is not defined or has incorrect format");

                if (!CellGroups.ContainsKey(parts[9]))
                    throw new Exception($"Cells set for {parts[9]} set type wasn't found");

                DataCell[] cells = CellGroups[parts[9]].Select(c => (DataCell)c.Clone()).ToArray();

                StatSet statSet = new StatSet()
                {
                    GameType = gameType,
                    Round = round,
                    Position = position,
                    OppPosition = oppPosition,
                    RelativePosition = relativePosition,
                    PlayersOnFlop = playersOnFlop,
                    PreflopPotType = preflopPotType,
                    PreflopActions = preflopActions,
                    OtherPlayersActed = otherPlayersActed,
                    SetType = setType,
                    Cells = cells
                };

                StatSets.Add(statSet);
            }
        }
    }
}
