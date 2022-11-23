using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InfoHelper.StatsEntities;
using Timer = System.Timers.Timer;

namespace InfoHelper.DataProcessor
{
    public partial class StatManager
    {
        private bool _shouldStop = false;

        private readonly Dictionary<string, StatPlayer> _players = new Dictionary<string, StatPlayer>();

        private readonly object _playersLock = new object();

        private readonly Dictionary<string, CancellationTokenSource> _tasksCancellationTokens = new Dictionary<string, CancellationTokenSource>();

        private readonly Timer _timer;

        private static readonly Dictionary<string, List<DataCell>> CellGroups = new Dictionary<string, List<DataCell>>();

        private static readonly List<StatSet> StatSets = new List<StatSet>();

        private const int ExpireTimeout = 300;

        private const int TimerInterval = 10;

        public StatManager()
        {
            LoadCellSets();

            LoadStatSets();

            LoadDefaultValues();

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
                        _tasksCancellationTokens[player].Cancel();

                        _tasksCancellationTokens.Remove(player);

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
                    _tasksCancellationTokens[player].Cancel();

                    _tasksCancellationTokens.Remove(player);

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

                    Parallel.For(0, statSetsCopy.Length, i =>
                    {
                        DataCell[] cells = StatSets[i].Cells.ToArray();

                        DataCell[] cellsCopy = cells.Select(c => (DataCell)c.Clone()).ToArray();

                        for (int j = 0; j < cells.Length; j++)
                        {
                            if (cells[j].ConnectedCells == null)
                                continue;

                            DataCell[] connectedCellsCopy = cells[j].ConnectedCells.Select(c => cellsCopy.First(c1 => c1.Name == c.Name)).ToArray();

                            cellsCopy[j].ConnectedCells = connectedCellsCopy;
                        }

                        statSetsCopy[i].Cells = cellsCopy;
                    });

                    StatPlayer newPlayer = new StatPlayer() { StatSets = statSetsCopy };

                    _players[player] = newPlayer;

                    CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

                    CancellationToken token = cancelTokenSource.Token;

                    _tasksCancellationTokens.Add(player, cancelTokenSource);

                    Task.Factory.StartNew(() => GetPlayerStats(player, statSetsCopy, token), token);
                }

                output = _players[player];

                output.LastAccessTime = DateTime.Now;
            }

            return output.StatSets;
        }

        private partial void GetPlayerStats(string player, StatSet[] statSets, CancellationToken token);

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

            int linesCounter = 1;

            while (sr.Peek() >= 0)
            {
                string line = sr.ReadLine();

                string[] parts = line.Split(';');

                if(!Enum.TryParse(parts[0], out GameType gameType))
                    throw new Exception($"Game type at line {linesCounter} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[1], out Round round))
                    throw new Exception($"Round at line {linesCounter} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[2], out Position position))
                    throw new Exception($"Position at line {linesCounter} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[3], out RelativePosition relativePosition))
                    throw new Exception($"Relative position at line {linesCounter} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[4], out Position oppPosition))
                    throw new Exception($"Opponent position at line {linesCounter} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[5], out PlayersOnFlop playersOnFlop))
                    throw new Exception($"Number of players on flop type at line {linesCounter} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[6], out PreflopPotType preflopPotType))
                    throw new Exception($"Preflop pot type at line {linesCounter} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[7], out PreflopActions preflopActions))
                    throw new Exception($"Preflop actions type at line {linesCounter} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[8], out OtherPlayersActed otherPlayersActed))
                    throw new Exception($"Other players acted type at line {linesCounter} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[9], out SetType setType))
                    throw new Exception($"Set type at line {linesCounter} is not defined or has incorrect format");

                if (!CellGroups.ContainsKey(parts[9]))
                    throw new Exception($"Cells set at line {linesCounter} wasn't found");

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

                linesCounter++;
            }
        }

        private static void LoadDefaultValues()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\CsvFiles\\default_values.csv");

            if (!File.Exists(filePath))
                throw new Exception("File \"default_values.csv\" doesn't exist in Resources\\CsvFiles directory");

            using FileStream fs = File.Open(filePath, FileMode.Open);

            StreamReader sr = new StreamReader(fs);

            string title = sr.ReadLine();

            if (title == null)
                throw new Exception("File \"default_values.csv\" doesn't contain header");

            string[] headerParts = title.Split(';');

            if (headerParts.Length != 17)
                throw new Exception("Header row in file \"default_values.csv\" contains less or more than 17 elements");

            int linesCounter = 1;

            while (sr.Peek() >= 0)
            {
                string line = sr.ReadLine();

                string[] parts = line.Split(';');

                if (!Enum.TryParse(parts[0], out GameType gameType))
                    throw new Exception($"Game type at line {linesCounter} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[1], out Round round))
                    throw new Exception($"Round at line {linesCounter} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[2], out Position position))
                    throw new Exception($"Position at line {linesCounter} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[3], out RelativePosition relativePosition))
                    throw new Exception($"Relative position at line {linesCounter} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[4], out Position oppPosition))
                    throw new Exception($"Opponent position at line {linesCounter} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[5], out PlayersOnFlop playersOnFlop))
                    throw new Exception($"Number of players on flop type at line {linesCounter} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[6], out PreflopPotType preflopPotType))
                    throw new Exception($"Preflop pot type at line {linesCounter} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[7], out PreflopActions preflopActions))
                    throw new Exception($"Preflop actions type at line {linesCounter} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[8], out OtherPlayersActed otherPlayersActed))
                    throw new Exception($"Other players acted type at line {linesCounter} is not defined or has incorrect format");

                if (!Enum.TryParse(parts[9], out SetType setType))
                    throw new Exception($"Set type at line {linesCounter} is not defined or has incorrect format");

                string cellName = parts[10];

                if(cellName == string.Empty)
                    throw new Exception($"Cell name at line {linesCounter} is not defined");

                if (parts[11] == string.Empty)
                    throw new Exception($"Cell default value at line {linesCounter} is not defined");

                if (!float.TryParse(parts[11], NumberStyles.Any, CultureInfo.InvariantCulture, out float cellDefaultValue))
                    throw new Exception($"Cell default value at line {linesCounter} has incorrect format");

                float[] mainGroupDfltValues = new float[] { float.NaN, float.NaN, float.NaN };

                if (parts[12] != string.Empty)
                {
                    string[] mainGroupParts = parts[12].Split(",");

                    if (mainGroupParts.Length != 3)
                        throw new Exception($"Cell main group default values at line {linesCounter} are defined, but have incorrect format");

                    for (int i = 0; i < mainGroupParts.Length; i++)
                    {
                        if (mainGroupParts[i] == string.Empty)
                            continue;

                        if (!float.TryParse(mainGroupParts[i], NumberStyles.Any, CultureInfo.InvariantCulture, out mainGroupDfltValues[i]))
                            throw new Exception($"Cell main group default values at line {linesCounter} are defined, but have incorrect format");
                    }
                }

                float[][] subGroupDefaultValues = new float[][]
                {
                    new float[]{float.NaN, float.NaN, float.NaN},
                    new float[]{float.NaN, float.NaN, float.NaN},
                    new float[]{float.NaN, float.NaN, float.NaN},
                    new float[]{float.NaN, float.NaN, float.NaN},
                };

                for (int i = 13; i < parts.Length; i++)
                {
                    if (parts[i] != string.Empty)
                    {
                        string[] subGroupParts = parts[i].Split(",");

                        if (subGroupParts.Length != 3)
                            throw new Exception($"Cell subgroup {i - 13} default values at line {linesCounter} are defined, but have incorrect format");

                        for (int j = 0; j < subGroupParts.Length; j++)
                        {
                            if (subGroupParts[j] == string.Empty)
                                continue;

                            if (!float.TryParse(subGroupParts[j], NumberStyles.Any, CultureInfo.InvariantCulture, out subGroupDefaultValues[i - 13][j]))
                                throw new Exception($"Cell subgroup {i - 13} default values at line {linesCounter} are defined, but have incorrect format");
                        }
                    }
                }

                StatSet matchedSet = StatSets.FirstOrDefault(s => s.GameType == gameType && s.Round == round && s.Position == position && s.RelativePosition == relativePosition && s.OppPosition == oppPosition &&
                                                                  s.PlayersOnFlop == playersOnFlop && s.PreflopPotType == preflopPotType && s.PreflopActions == preflopActions && s.OtherPlayersActed == otherPlayersActed &&
                                                                  s.SetType == setType);

                if(matchedSet == null)
                    throw new Exception("No matched set found");

                DataCell matchedCell = matchedSet.Cells.FirstOrDefault(c => c.Name == cellName);

                if (matchedCell == null)
                    throw new Exception("No matched data cell found");

                matchedCell.DefaultValue = cellDefaultValue;

                if (matchedCell.CellData is PostflopData pd)
                {
                    pd.MainGroup.MadeHandsDefaultValue = mainGroupDfltValues[0];
                    pd.MainGroup.DrawHandsDefaultValue = mainGroupDfltValues[1];
                    pd.MainGroup.ComboHandsDefaultValue = mainGroupDfltValues[2];

                    for (int i = 0; i < subGroupDefaultValues.Length; i++)
                    {
                        pd.SubGroups[i].MadeHandsDefaultValue = subGroupDefaultValues[i][0];
                        pd.SubGroups[i].DrawHandsDefaultValue = subGroupDefaultValues[i][1];
                        pd.SubGroups[i].ComboHandsDefaultValue = subGroupDefaultValues[i][2];
                    }
                }

                linesCounter++;
            }
        }
    }
}
