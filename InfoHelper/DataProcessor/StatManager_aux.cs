using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HandUtility;
using HoldemHand;
using InfoHelper.Db;
using InfoHelper.StatsEntities;
using InfoHelper.Utils;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using StatUtility;

namespace InfoHelper.DataProcessor
{
    public partial class StatManager
    {
        private partial void GetPlayerStats(string player, StatSet[] statSets, CancellationToken token)
        {
            SqlConnectionStringBuilder sqlBuilder = new SqlConnectionStringBuilder
            {
                DataSource = Shared.ServerName,
                IntegratedSecurity = true,
                InitialCatalog = Shared.DbName
            };

            using StatDbContext statDbContext = new StatDbContext(sqlBuilder.ConnectionString);

            statDbContext.ChangeTracker.AutoDetectChangesEnabled = false;

            statDbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            var playerData =
                (from ph in statDbContext.PlayerHands
                join h in statDbContext.Hands
                    on ph.HandId equals h.Id
                join p in statDbContext.Players
                    on ph.PlayerId equals p.Id
                where p.PlayerName == player
                orderby h.DateTimePlayed
                select new { h, ph }).Take(Shared.GetLastNHands);

            foreach (var record in playerData.AsQueryable())
            {
                if(token.IsCancellationRequested)
                    break;

                int playersSawFlop = record.h.PlayersSawFlop;

                int sbPosition = record.h.SbPosition;
                int bbPosition = record.h.BbPosition;
                int btnPosition = record.h.BtnPosition;

                float bbAmount = record.h.BbAmount;

                int playerPosition = record.ph.Position;

                float playerSdChips = record.ph.ChipsSd;
                float playerEvChips = record.ph.ChipsEv;

                string[] playerHoleCards = new string[] { record.ph.HoleCard1, record.ph.HoleCard2 };

                byte[] actionsData = record.h.Actions;

                if (actionsData.Length == 0 || actionsData.Length != actionsData[0] * 8 + 1)
                    throw new Exception("Actions data array contains incorrect number of bytes");

                var actions = Enumerable.Repeat(new { ActionType = -1, Position = -1, Amount = float.NaN, Round = -1, CanRaise = false}, actionsData[0]).ToArray();

                bool[] isPlayerIn = new bool[6];

                int actionIndex = 1;

                for (int i = 0; i < actionsData[0]; i++)
                {
                    actions[i] = new
                    {
                        ActionType = (int)actionsData[actionIndex],
                        Position = (int)actionsData[actionIndex + 1],
                        Amount = BitConverter.ToSingle(actionsData, actionIndex + 2),
                        Round = (int)actionsData[actionIndex + 6],
                        CanRaise = BitConverter.ToBoolean(actionsData, actionIndex + 7)
                    };

                    if (!isPlayerIn[actions[i].Position - 1])
                        isPlayerIn[actions[i].Position - 1] = true;

                    actionIndex += 8;
                }

                bool playerActedOnFlop = false;

                foreach (var action in actions)
                {
                    if (action.Round == 1)
                        continue;

                    if (action.Position != playerPosition)
                        continue;

                    if (action.Round == 2)
                    {
                        playerActedOnFlop = true;

                        break;
                    }
                }

                GameType gameType = sbPosition == btnPosition ? GameType.Hu : GameType.SixMax;

                Round round = playerActedOnFlop ? Round.Postflop : Round.Preflop;

                Position position = Common.GetPlayerPosition(new int[] { sbPosition, bbPosition, btnPosition }, playerPosition, isPlayerIn);

                Position oppPosition = Position.Any;

                RelativePosition relativePosition = RelativePosition.Any;

                PlayersOnFlop playersOnFlop = PlayersOnFlop.Any;

                PreflopPotType preflopPotType = PreflopPotType.Any;

                PreflopActions preflActions = PreflopActions.Any;

                OtherPlayersActed otherPlayersActed = OtherPlayersActed.Any;

                int callCount = 0;
                int raiseCount = 0;

                if (playerActedOnFlop)
                {
                    if (playersSawFlop != 2)
                    {
                        playersOnFlop = PlayersOnFlop.Multiway;

                        otherPlayersActed = OtherPlayersActed.Yes;
                    }
                    else
                    {
                        bool[] isPlayerInBeforeFlop = isPlayerIn.ToArray();

                        foreach (var action in actions)
                        {
                            if (action.Round == 2)
                                break;

                            //Fold
                            if (action.ActionType == 2)
                                isPlayerInBeforeFlop[action.Position - 1] = false;
                        }

                        int oppIndexPosition = isPlayerInBeforeFlop.Select((p, k) => new { Value = p, Index = k }).First(item => item.Value && item.Index != playerPosition - 1).Index;

                        otherPlayersActed = OtherPlayersActed.No;

                        foreach (var action in actions)
                        {
                            if(action.Round != 1)
                                break;

                            if (action.Position == playerPosition || action.Position == oppIndexPosition + 1)
                                continue;

                            //Check || call || raise
                            if (action.ActionType is 3 or 4 or 6)
                            {
                                otherPlayersActed = OtherPlayersActed.Yes;

                                break;
                            }
                        }

                        if (otherPlayersActed == OtherPlayersActed.No)
                        {
                            oppPosition = Common.GetPlayerPosition(new int[] { sbPosition, bbPosition, btnPosition }, oppIndexPosition + 1, isPlayerIn);

                            if (gameType == GameType.Hu)
                                relativePosition = sbPosition == playerPosition ? RelativePosition.Ip : RelativePosition.Oop;
                            else
                                relativePosition = position > oppPosition ? RelativePosition.Ip : RelativePosition.Oop;
                        }

                        playersOnFlop = PlayersOnFlop.Hu;
                    }

                    foreach (var action in actions)
                    {
                        if (action.Round != 1)
                            break;

                        //Call
                        if (action.ActionType == 4)
                        {
                            if (raiseCount == 0)
                                preflopPotType = PreflopPotType.LimpPot;

                            callCount++;
                        }
                        //Raise
                        else if (action.ActionType == 6)
                        {
                            if (raiseCount == 0)
                                preflopPotType = callCount == 0 ? PreflopPotType.RaisePot : PreflopPotType.IsolatePot;
                            else if (raiseCount == 1)
                            {
                                if (preflopPotType == PreflopPotType.IsolatePot)
                                    preflopPotType = PreflopPotType.RaiseIsolatePot;
                                else if (preflopPotType == PreflopPotType.RaisePot)
                                    preflopPotType = callCount == 0 ? PreflopPotType.ThreeBetPot : PreflopPotType.SqueezePot;
                            }
                            else if (raiseCount == 2)
                                preflopPotType = PreflopPotType.FourBetPot;
                            else if (raiseCount == 3)
                                preflopPotType = PreflopPotType.FiveBetPot;
                            else
                                preflopPotType = PreflopPotType.Unknown;

                            raiseCount++;
                        }
                    }

                    var playerPreflopActions = actions.Where(a => a.Position == playerPosition && a.ActionType != 0 && a.ActionType != 1 && a.Round == 1).ToArray();

                    if (playerPreflopActions.Length == 1)
                    {
                        //Check
                        if (playerPreflopActions[0].ActionType == 3)
                            preflActions = PreflopActions.Check;
                        //Call
                        else if (playerPreflopActions[0].ActionType == 4)
                            preflActions = PreflopActions.Call;
                        //Raise
                        else if (playerPreflopActions[0].ActionType == 6)
                            preflActions = PreflopActions.Raise;
                    }
                    else if (playerPreflopActions.Length == 2)
                    {
                        //Call
                        if (playerPreflopActions[0].ActionType == 4)
                        {
                            //Call
                            if (playerPreflopActions[1].ActionType == 4)
                                preflActions = PreflopActions.CallCall;
                            //Raise
                            else if (playerPreflopActions[1].ActionType == 6)
                                preflActions = PreflopActions.CallRaise;
                        }
                        //Raise
                        else if (playerPreflopActions[0].ActionType == 6)
                        {
                            //Call
                            if (playerPreflopActions[1].ActionType == 4)
                                preflActions = PreflopActions.RaiseCall;
                            //Raise
                            else if (playerPreflopActions[1].ActionType == 6)
                                preflActions = PreflopActions.RaiseRaise;
                        }
                    }
                    else if (playerPreflopActions.Length > 2)
                    {
                        //Call
                        if (playerPreflopActions[^1].ActionType == 4)
                            preflActions = PreflopActions.AnyCall;
                        //Raise
                        else if (playerPreflopActions[^1].ActionType == 6)
                            preflActions = PreflopActions.AnyRaise;
                    }
                }

                StatSetManager statSetManager = new StatSetManager()
                {
                    StatSets = statSets,
                    GameType = gameType,
                    Round = round,
                    Position = position,
                    RelativePosition = relativePosition,
                    OppPosition = oppPosition,
                    PlayersOnFlop = playersOnFlop,
                    PreflopPotType = preflopPotType,
                    PreflopActions = preflActions,
                    OtherPlayersActed = otherPlayersActed
                };

                StatSet generalSet = statSetManager.GetStatSet(SetType.General);

                if (generalSet == null)
                    throw new Exception("General set wasn't found");

                StatSet preflopSet = null;

                foreach (SetType setTypeValue in Enum.GetValues<SetType>())
                {
                    if(!$"{setTypeValue}".Contains("Preflop"))
                        continue;

                    preflopSet = statSetManager.GetStatSet(setTypeValue);

                    if(preflopSet != null)
                        break;
                }

                if (preflopSet == null)
                    throw new Exception("Preflop set wasn't found");

                StatSet postflopSet = null;

                if (playerActedOnFlop)
                {
                    foreach (SetType setTypeValue in Enum.GetValues<SetType>())
                    {
                        if (!$"{setTypeValue}".Contains("Postflop"))
                            continue;

                        postflopSet = statSetManager.GetStatSet(setTypeValue);

                        if (postflopSet != null)
                            break;
                    }
                }

                #region Filling general set

                Dictionary<string, DataCell> generalCellsDict = generalSet.Cells.ToDictionary(k => k.Name, v => v);

                int playersRemaining = isPlayerIn.Count(p => p);

                generalCellsDict["Hands"].IncrementSample();

                foreach (var action in actions)
                {
                    if (action.Round != 1)
                        break;

                    if (action.ActionType is 0 or 1)
                        continue;

                    if (action.ActionType == 2)
                        playersRemaining--;

                    if(action.Position != playerPosition)
                        continue;

                    generalCellsDict["Vpip"].IncrementSample();

                    if(action.ActionType is 4 or 6)
                        generalCellsDict["Vpip"].IncrementValue();

                    if(action.CanRaise)
                        generalCellsDict["Pfr"].IncrementSample();

                    if (action.ActionType is 6)
                        generalCellsDict["Pfr"].IncrementValue();
                }

                bool playerFolded = false;

                foreach (var action in actions)
                {
                    if (action.Round == 1)
                        continue;

                    if (action.ActionType == 2)
                        playersRemaining--;

                    if (action.Position != playerPosition)
                        continue;

                    if (action.Round == 2)
                    {
                        if (action.CanRaise)
                            generalCellsDict["AggFq_F"].IncrementSample();

                        if (action.ActionType is 5 or 6)
                            generalCellsDict["AggFq_F"].IncrementValue();
                    }
                    else if (action.Round == 3)
                    {
                        if (action.CanRaise)
                            generalCellsDict["AggFq_T"].IncrementSample();

                        if (action.ActionType is 5 or 6)
                            generalCellsDict["AggFq_T"].IncrementValue();
                    }
                    else if (action.Round == 4)
                    {
                        if (action.CanRaise)
                            generalCellsDict["AggFq_R"].IncrementSample();

                        if (action.ActionType is 5 or 6)
                            generalCellsDict["AggFq_R"].IncrementValue();
                    }

                    if (action.ActionType == 2 && action.Position == playerPosition)
                    {
                        playerFolded = true;

                        break;
                    }
                }

                if (playerActedOnFlop)
                    generalCellsDict["WentToSd"].IncrementSample();

                bool wentToSd = playerActedOnFlop && !playerFolded && actions[^1].Round == 4 && playersRemaining > 1;

                if (wentToSd)
                {
                    generalCellsDict["WentToSd"].IncrementValue();

                    generalCellsDict["WonOnSd"].IncrementSample();

                    if(playerSdChips > 0)
                        generalCellsDict["WonOnSd"].IncrementValue();
                }

                generalCellsDict["EvBb"].IncrementSample();
                generalCellsDict["EvBb"].IncrementValue(playerEvChips);

                #endregion

                #region Filling preflop set

                Dictionary<string, DataCell> preflopCellsDict = preflopSet.Cells.ToDictionary(k => k.Name, v => v);

                gameType = preflopSet.GameType;

                preflActions = PreflopActions.NoActions;
                preflopPotType = PreflopPotType.Unopened;

                callCount = 0;
                raiseCount = 0;

                float pot = actions[0].Amount + actions[1].Amount;

                float[] bets = new float[6];

                bets[actions[0].Position - 1] = actions[0].Amount;
                bets[actions[1].Position - 1] = actions[1].Amount;

                float currentBet = actions[1].Amount;

                playersRemaining = isPlayerIn.Count(p => p);

                bool[] isPlayerInPreflop = isPlayerIn.ToArray();

                bool[] playersWithNonFoldActions = new bool[6];

                for (int i = 0; i < actions.Length; i++)
                {
                    var action = actions[i];

                    if (action.Round != 1)
                        break;

                    if(action.ActionType is 0 or 1)
                        continue;

                    if (action.Position == playerPosition)
                    {
                        string cellName = string.Empty;
                        string vsCellName = string.Empty;

                        string[] actionsString = ConvertPreflopActionIntToString(action.ActionType, false);

                        oppPosition = Position.Any;

                        if (playersRemaining == 2)
                        {
                            int oppIndexPosition = isPlayerInPreflop.Select((p, k) => new { Value = p, Index = k }).First(item => item.Value && item.Index != playerPosition - 1).Index;

                            if (!isPlayerInPreflop.Where((p, k) => k != playerPosition - 1 && k != oppIndexPosition && playersWithNonFoldActions[k]).Any())
                                oppPosition = Common.GetPlayerPosition(new int[] { sbPosition, bbPosition, btnPosition }, oppIndexPosition + 1, isPlayerIn);
                        }

                        if (preflopPotType == PreflopPotType.Unopened)
                        {
                            if (preflActions == PreflopActions.NoActions)
                            {
                                cellName = gameType == GameType.Hu ? "SbvsBb_Unopened" : $"{position}_Unopened";

                                Enum.TryParse<PreflopActions>(actionsString[0], out preflActions);
                            }
                        }
                        else if (preflopPotType == PreflopPotType.LimpPot)
                        {
                            if (preflActions == PreflopActions.NoActions)
                            {
                                if (callCount == 1)
                                {
                                    if (gameType == GameType.Hu)
                                        cellName = "BbvsSb_Limp";
                                    else
                                    {
                                        Position limperPosition = Common.GetPlayerPosition(new int[] { sbPosition, bbPosition, btnPosition }, actions.First(a => a.Round == 1 && a.ActionType == 4).Position, isPlayerIn);

                                        cellName = $"{position}_Limp";

                                        vsCellName = $"{position}_Limp_Vs_{limperPosition}";
                                    }
                                }
                                else
                                {
                                    cellName = $"{position}_Limp";

                                    vsCellName = $"{position}_Limp_Vs_Multi";
                                }

                                if (position == Position.Bb)
                                    actionsString = ConvertPreflopActionIntToString(action.ActionType, true);

                                Enum.TryParse<PreflopActions>(actionsString[0], out preflActions);
                            }
                        }
                        else if (preflopPotType == PreflopPotType.RaisePot)
                        {
                            if (preflActions == PreflopActions.NoActions)
                            {
                                if (callCount == 0)
                                {
                                    if (gameType == GameType.Hu)
                                        cellName = "BbvsSb_Raise";
                                    else
                                    {
                                        Position raiserPosition = Common.GetPlayerPosition(new int[] { sbPosition, bbPosition, btnPosition }, actions.First(a => a.Round == 1 && a.ActionType == 6).Position, isPlayerIn);

                                        cellName = $"{position}_Raise";

                                        vsCellName = $"{position}_Raise_Vs_{raiserPosition}";
                                    }
                                }
                                else
                                {
                                    cellName = $"{position}_Raise";

                                    vsCellName = $"{position}_Raise_Vs_Multi";
                                }

                                Enum.TryParse<PreflopActions>(actionsString[0], out preflActions);
                            }
                        }
                        else if (preflopPotType == PreflopPotType.IsolatePot)
                        {
                            if (preflActions == PreflopActions.NoActions)
                            {
                                cellName = $"{position}_Isolate_CC";

                                Enum.TryParse<PreflopActions>(actionsString[0], out preflActions);
                            }
                            else if (preflActions == PreflopActions.Call)
                            {
                                if (gameType == GameType.Hu)
                                    cellName = "SbvsBb_Isolate";
                                else
                                {
                                    cellName = $"{position}_Isolate_Limper";

                                    if (position == Position.Sb && oppPosition == Position.Bb)
                                        vsCellName = $"{position}_Isolate_Limper_Vs_{oppPosition}";
                                }

                                Enum.TryParse<PreflopActions>($"{preflActions}{actionsString[0]}", out preflActions);
                            }
                        }
                        else if (preflopPotType == PreflopPotType.ThreeBetPot)
                        {
                            if (preflActions == PreflopActions.NoActions)
                            {
                                cellName = $"{position}_3bet_CC";

                                Enum.TryParse<PreflopActions>(actionsString[0], out preflActions);
                            }
                            else if (preflActions == PreflopActions.Raise)
                            {
                                if (gameType == GameType.Hu)
                                    cellName = "SbvsBb_3bet";
                                else
                                {
                                    cellName = $"{position}_3bet_Raiser";

                                    vsCellName = oppPosition != Position.Any ? $"{position}_3bet_Raiser_Vs_{oppPosition}" : $"{position}_3bet_Raiser_Vs_Multi";
                                }

                                Enum.TryParse<PreflopActions>($"{preflActions}{actionsString[0]}", out preflActions);
                            }
                        }
                        else if (preflopPotType == PreflopPotType.SqueezePot)
                        {
                            if (preflActions == PreflopActions.NoActions)
                            {
                                cellName = $"{position}_Squeeze_CC";

                                Enum.TryParse<PreflopActions>(actionsString[0], out preflActions);
                            }
                            else if (preflActions == PreflopActions.Call)
                            {
                                cellName = $"{position}_Squeeze_Caller";

                                Enum.TryParse<PreflopActions>($"{preflActions}{actionsString[0]}", out preflActions);
                            }
                            else if (preflActions == PreflopActions.Raise)
                            {
                                cellName = $"{position}_Squeeze_Raiser";

                                Enum.TryParse<PreflopActions>($"{preflActions}{actionsString[0]}", out preflActions);
                            }
                        }
                        else if (preflopPotType == PreflopPotType.RaiseIsolatePot)
                        {
                            if (preflActions == PreflopActions.NoActions)
                            {
                                cellName = $"{position}_RaiseIsolate_CC";

                                Enum.TryParse<PreflopActions>(actionsString[0], out preflActions);
                            }
                            else if (preflActions == PreflopActions.Call)
                            {
                                cellName = $"{position}_RaiseIsolate_Caller";

                                Enum.TryParse<PreflopActions>($"{preflActions}{actionsString[0]}", out preflActions);
                            }
                            else if (preflActions == PreflopActions.Raise)
                            {
                                if (gameType == GameType.Hu)
                                    cellName = "BbvsSb_RaiseIsolate";
                                else
                                {
                                    cellName = $"{position}_RaiseIsolate_Raiser";

                                    if (position == Position.Bb && oppPosition == Position.Sb)
                                        vsCellName = $"{position}_RaiseIsolate_Vs_{oppPosition}";
                                }

                                Enum.TryParse<PreflopActions>($"{preflActions}{actionsString[0]}", out preflActions);
                            }
                            else if (preflActions == PreflopActions.CallCall)
                            {
                                cellName = $"{position}_RaiseIsolate_Caller";

                                Enum.TryParse<PreflopActions>($"Any{actionsString[0]}", out preflActions);
                            }
                        }
                        else if (preflopPotType == PreflopPotType.FourBetPot)
                        {
                            if (preflActions == PreflopActions.CallRaise)
                            {
                                if (gameType == GameType.Hu)
                                    cellName = "SbvsBb_4bet";
                                else
                                {
                                    cellName = $"{position}_4bet";

                                    if (position == Position.Sb && oppPosition == Position.Bb)
                                        vsCellName = $"{position}_4bet_Vs_{oppPosition}";
                                }
                            }
                            else if (preflActions == PreflopActions.RaiseRaise)
                            {
                                if (gameType == GameType.Hu)
                                    cellName = "BbvsSb_4bet";
                                else
                                {
                                    cellName = $"{position}_4bet";

                                    if (position == Position.Bb && oppPosition == Position.Sb)
                                        vsCellName = $"{position}_4bet_Vs_{oppPosition}";
                                }
                            }
                            else
                                cellName = $"{position}_4bet";

                            Enum.TryParse<PreflopActions>($"Any{actionsString[0]}", out preflActions);
                        }
                        else if (preflopPotType == PreflopPotType.FiveBetPot)
                        {
                            if (gameType == GameType.Hu)
                                cellName = $"{position}vs{oppPosition}_5bet";
                        }

                        preflopCellsDict.TryGetValue($"{cellName}_{actionsString[0]}", out DataCell preflopCell);
                        preflopCellsDict.TryGetValue($"{vsCellName}_{actionsString[0]}", out DataCell vsPreflopCell);

                        if (preflopCell != null)
                        {
                            preflopCell.IncrementSample();
                            preflopCellsDict[$"{cellName}_{actionsString[1]}"].IncrementSample();

                            if (actionsString.Length == 3)
                            {
                                if (action.ActionType == 6 || action.CanRaise)
                                    preflopCellsDict[$"{cellName}_{actionsString[2]}"].IncrementSample();
                            }

                            preflopCell.IncrementValue();

                            PreflopData preflopData = (PreflopData)preflopCell.CellData;

                            PreflopHandsGroup preflopHandsMainGroup = (PreflopHandsGroup)preflopData.MainGroup;

                            AddCounterAction(preflopHandsMainGroup, i, action.Round);

                            AddPreflopHand(preflopHandsMainGroup);

                            if (preflopCell.BetRanges != null)
                            {
                                float amount = 0;

                                if (preflopCell.BetType == 'b')
                                    amount = (bets[playerPosition - 1] + action.Amount) / bbAmount;
                                else if (preflopCell.BetType == 'x')
                                    amount = (bets[playerPosition - 1] + action.Amount) / currentBet;
                                else if (preflopCell.BetType == 'p')
                                {
                                    float callAmount = currentBet - bets[playerPosition - 1];

                                    amount = (action.Amount - callAmount) * 100 / (pot + callAmount);
                                }

                                if (amount > 0)
                                {
                                    for (int j = 0; j < preflopCell.BetRanges.Length; j++)
                                    {
                                        if (amount >= preflopCell.BetRanges[j].LowBound && amount < preflopCell.BetRanges[j].UpperBound)
                                        {
                                            PreflopHandsGroup preflopHandsSubGroup = (PreflopHandsGroup)preflopData.SubGroups[j];

                                            AddCounterAction(preflopHandsSubGroup, i, action.Round);

                                            AddPreflopHand(preflopHandsSubGroup);

                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (vsPreflopCell != null)
                        {
                            vsPreflopCell.IncrementSample();
                            preflopCellsDict[$"{vsCellName}_{actionsString[1]}"].IncrementSample();

                            if (actionsString.Length == 3)
                            {
                                if (action.ActionType == 6 || action.CanRaise)
                                    preflopCellsDict[$"{vsCellName}_{actionsString[2]}"].IncrementSample();
                            }

                            vsPreflopCell.IncrementValue();

                            PreflopData preflopData = (PreflopData)vsPreflopCell.CellData;

                            PreflopHandsGroup preflopHandsMainGroup = (PreflopHandsGroup)preflopData.MainGroup;

                            AddCounterAction(preflopHandsMainGroup, i, action.Round);

                            AddPreflopHand(preflopHandsMainGroup);

                            if (vsPreflopCell.BetRanges != null)
                            {
                                float amount = 0;

                                if (vsPreflopCell.BetType == 'b')
                                    amount = (bets[playerPosition - 1] + action.Amount) / bbAmount;
                                else if (vsPreflopCell.BetType == 'x')
                                    amount = (bets[playerPosition - 1] + action.Amount) / currentBet;
                                else if (vsPreflopCell.BetType == 'p')
                                {
                                    float callAmount = currentBet - bets[playerPosition - 1];

                                    amount = (action.Amount - callAmount) * 100 / (pot + callAmount);
                                }

                                if (amount > 0)
                                {
                                    for (int j = 0; j < vsPreflopCell.BetRanges.Length; j++)
                                    {
                                        if (amount >= vsPreflopCell.BetRanges[j].LowBound && amount < vsPreflopCell.BetRanges[j].UpperBound)
                                        {
                                            PreflopHandsGroup preflopHandsSubGroup = (PreflopHandsGroup)preflopData.SubGroups[j];

                                            AddCounterAction(preflopHandsSubGroup, i, action.Round);

                                            AddPreflopHand(preflopHandsSubGroup);

                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        void AddPreflopHand(PreflopHandsGroup handsGroup)
                        {
                            if (playerHoleCards[0] == null || playerHoleCards[1] == null)
                                return;

                            (char firstCardFaceValue, char secondCardFaceValue) = (playerHoleCards[0][0], playerHoleCards[1][0]);

                            if (Array.IndexOf(Common.FaceValues, firstCardFaceValue) < Array.IndexOf(Common.FaceValues, secondCardFaceValue))
                                (firstCardFaceValue, secondCardFaceValue) = (playerHoleCards[1][0], playerHoleCards[0][0]);

                            string hand = $"{firstCardFaceValue}{secondCardFaceValue}";

                            if (firstCardFaceValue != secondCardFaceValue)
                                hand += playerHoleCards[0][1] == playerHoleCards[1][1] ? "s" : "o";

                            handsGroup.PocketHands[Array.IndexOf(Common.HoleCards, hand)]++;
                        }
                    }

                    //Update variables
                    pot += action.Amount;

                    bets[action.Position - 1] += action.Amount;

                    if(bets[action.Position - 1] + action.Amount > currentBet)
                        currentBet = bets[action.Position - 1] + action.Amount;

                    if(action.ActionType == 2)
                    {
                        isPlayerInPreflop[action.Position - 1] = false;

                        playersRemaining--;
                    }
                    else 
                        playersWithNonFoldActions[action.Position - 1] = true;

                    //Update pot type
                    if (action.ActionType == 4)
                    {
                        if (raiseCount == 0)
                            preflopPotType = PreflopPotType.LimpPot;

                        callCount++;
                    }
                    else if (action.ActionType == 6)
                    {
                        if (raiseCount == 0)
                            preflopPotType = callCount == 0 ? PreflopPotType.RaisePot : PreflopPotType.IsolatePot;
                        else if (raiseCount == 1)
                        {
                            if (preflopPotType == PreflopPotType.IsolatePot)
                                preflopPotType = PreflopPotType.RaiseIsolatePot;
                            else if (preflopPotType == PreflopPotType.RaisePot)
                                preflopPotType = callCount == 0 ? PreflopPotType.ThreeBetPot : PreflopPotType.SqueezePot;
                        }
                        else if (raiseCount == 2)
                            preflopPotType = PreflopPotType.FourBetPot;
                        else if (raiseCount == 3)
                            preflopPotType = PreflopPotType.FiveBetPot;
                        else
                            preflopPotType = PreflopPotType.Unknown;

                        raiseCount++;
                    }

                    string[] ConvertPreflopActionIntToString(int actionType, bool isBbInLimpPot)
                    {
                        return (actionType, isBbInLimpPot) switch
                        {
                            (2, _) => new string[] { "Fold", "Call", "Raise" },
                            (3, true) => new string[] { "Check", "Raise" },
                            (4, _) => new string[] { "Call", "Fold", "Raise" },
                            (6, false) => new string[] { "Raise", "Fold", "Call" },
                            (6, true) => new string[] { "Raise", "Check" },
                            _ => null
                        };
                    }
                }

                #endregion

                #region Filling postflop set

                if (postflopSet == null)
                    continue;

                Dictionary<string, DataCell> postflopCellsDict = postflopSet.Cells.ToDictionary(k => k.Name, v => v);

                float[] roundBets = new float[6];

                currentBet = 0;

                int currentRound = 1;

                string[] actionSequences = new string[3] { string.Empty, string.Empty, string.Empty };

                ulong pocketMask = playerHoleCards[0] != null && playerHoleCards[1] != null ? Hand.ParseHand($"{playerHoleCards[0]}{playerHoleCards[1]}") : 0ul;

                double handEquity = double.NaN;

                HandType handType = HandType.None;

                for (int i = 0; i < actions.Length; i++)
                {
                    var action = actions[i];

                    if (action.Round == 1)
                        continue;

                    if (action.Round > currentRound)
                    {
                        roundBets = new float[6];

                        currentBet = 0;

                        currentRound = action.Round;

                        if (pocketMask != 0ul)
                        {
                            string board = null;

                            if(action.Round == 2)
                                board = $"{record.h.FlopCard1}{record.h.FlopCard2}{record.h.FlopCard3}";
                            else if(action.Round == 3)
                                board = $"{record.h.FlopCard1}{record.h.FlopCard2}{record.h.FlopCard3}{record.h.TurnCard}";
                            else if (action.Round == 4)
                                board = $"{record.h.FlopCard1}{record.h.FlopCard2}{record.h.FlopCard3}{record.h.TurnCard}{record.h.RiverCard}";

                            ulong boardMask = Hand.ParseHand(board);

                            handEquity = Math.Round(HandManager.CalculateHandEquity(pocketMask, boardMask));

                            handType = HandManager.GetMadeHandType(pocketMask, boardMask);
                        }
                    }

                    if (action.Position == playerPosition)
                    {
                        string[] cellNames = new string[3];
                        string[] altCellNames = new string[3];

                        string roundAbbr = string.Empty;

                        if (action.Round == 2)
                            roundAbbr = "_F";
                        else if (action.Round == 3)
                            roundAbbr = "_T";
                        else if (action.Round == 4)
                            roundAbbr = "_R";

                        int betsRaisesCount = actionSequences[action.Round - 2].Count(a => a is 'b' or 'r');

                        //Multiway
                        if (postflopSet.SetType == SetType.PostflopGeneral)
                        {
                            //Unopened pot
                            if (betsRaisesCount == 0)
                                cellNames = action.ActionType == 5 ? new string[] { $"Bet{roundAbbr}", $"Check{roundAbbr}", null } : new string[] { $"Check{roundAbbr}", $"Bet{roundAbbr}", null };
                            //Bet pot
                            else if (betsRaisesCount == 1)
                            {
                                if (action.ActionType == 2)
                                    cellNames = new string[] { $"FvBet{roundAbbr}", $"CvBet{roundAbbr}", $"Raise{roundAbbr}" };
                                else if (action.ActionType == 4)
                                    cellNames = new string[] { $"CvBet{roundAbbr}", $"FvBet{roundAbbr}", $"Raise{roundAbbr}" };
                                else if (action.ActionType == 6)
                                    cellNames = new string[] { $"Raise{roundAbbr}", $"FvBet{roundAbbr}", $"CvBet{roundAbbr}" };
                            }
                            //Raise pot
                            else if (betsRaisesCount == 2)
                            {
                                if (action.ActionType == 2)
                                    cellNames = new string[] { $"FvRaise{roundAbbr}", $"CvRaise{roundAbbr}", $"ThreeBet{roundAbbr}" };
                                else if (action.ActionType == 4)
                                    cellNames = new string[] { $"CvRaise{roundAbbr}", $"FvRaise{roundAbbr}", $"ThreeBet{roundAbbr}" };
                                else if (action.ActionType == 6)
                                    cellNames = new string[] { $"ThreeBet{roundAbbr}", $"FvRaise{roundAbbr}", $"CvRaise{roundAbbr}" };
                            }
                            //3 bet pot
                            else if (betsRaisesCount == 3)
                            {
                                if (action.ActionType == 2)
                                    cellNames = new string[] { $"FvThreeBet{roundAbbr}", $"CvThreeBet{roundAbbr}", $"FourBet{roundAbbr}" };
                                else if (action.ActionType == 4)
                                    cellNames = new string[] { $"CvThreeBet{roundAbbr}", $"FvThreeBet{roundAbbr}", $"FourBet{roundAbbr}" };
                                else if (action.ActionType == 6)
                                    cellNames = new string[] { $"FourBet{roundAbbr}", $"FvThreeBet{roundAbbr}", $"CvThreeBet{roundAbbr}" };
                            }
                        }
                        //Hu
                        else
                        {
                            //Unopened pot
                            if (betsRaisesCount == 0)
                                altCellNames = action.ActionType == 5 ? new string[] { $"Bet{roundAbbr}", $"Check{roundAbbr}", null } : new string[] { $"Check{roundAbbr}", $"Bet{roundAbbr}", null };
                            //Bet pot
                            else if (betsRaisesCount == 1)
                            {
                                if (action.ActionType == 2)
                                    altCellNames = new string[] { $"FvBet{roundAbbr}", $"CvBet{roundAbbr}", $"Raise{roundAbbr}" };
                                else if (action.ActionType == 4)
                                    altCellNames = new string[] { $"CvBet{roundAbbr}", $"FvBet{roundAbbr}", $"Raise{roundAbbr}" };
                                else if (action.ActionType == 6)
                                    altCellNames = new string[] { $"Raise{roundAbbr}", $"FvBet{roundAbbr}", $"CvBet{roundAbbr}" };
                            }
                            //Raise pot
                            else if (betsRaisesCount == 2)
                            {
                                if (action.ActionType == 2)
                                    altCellNames = new string[] { $"FvRaise{roundAbbr}", $"CvRaise{roundAbbr}", $"THREEBET{roundAbbr}" };
                                else if (action.ActionType == 4)
                                    altCellNames = new string[] { $"CvRaise{roundAbbr}", $"FvRaise{roundAbbr}", $"THREEBET{roundAbbr}" };
                                else if (action.ActionType == 6)
                                    altCellNames = new string[] { $"THREEBET{roundAbbr}", $"FvRaise{roundAbbr}", $"CvRaise{roundAbbr}" };
                            }
                            //3 bet pot
                            else if (betsRaisesCount == 3)
                            {
                                if (action.ActionType == 2)
                                    altCellNames = new string[] { $"FvTHREEBET{roundAbbr}", $"CvTHREEBET{roundAbbr}", $"FOURBET{roundAbbr}" };
                                else if (action.ActionType == 4)
                                    altCellNames = new string[] { $"CvTHREEBET{roundAbbr}", $"FvTHREEBET{roundAbbr}", $"FOURBET{roundAbbr}" };
                                else if (action.ActionType == 6)
                                    altCellNames = new string[] { $"FOURBET{roundAbbr}", $"FvTHREEBET{roundAbbr}", $"CvTHREEBET{roundAbbr}" };
                            }

                            //Flop
                            if (action.Round == 2)
                            {
                                if (actionSequences[0] is not ("brr" or "xbrr"))
                                {
                                    //IP preflop raiser
                                    if (postflopSet.SetType == SetType.PostflopHuIpRaiser)
                                    {
                                        if (actionSequences[0] == "x")
                                            cellNames = action.ActionType == 5 ? new string[] { "CB_F", "CX_F", null } : new string[] { "CX_F", "CB_F", null };
                                        else if (actionSequences[0] == "xbr")
                                        {
                                            if (action.ActionType == 2)
                                                cellNames = new string[] { "FCBvR_F", "CCBvR_F", null };
                                            else if (action.ActionType == 4)
                                                cellNames = new string[] { "CCBvR_F", "FCBvR_F", null };
                                        }
                                        else if (actionSequences[0] == "b")
                                        {
                                            if (action.ActionType == 2)
                                                cellNames = new string[] { "FvDONK_F", "CvDONK_F", "RvDONK_F" };
                                            else if (action.ActionType == 4)
                                                cellNames = new string[] { "CvDONK_F", "FvDONK_F", "RvDONK_F" };
                                            else if (action.ActionType == 6)
                                                cellNames = new string[] { "RvDONK_F", "FvDONK_F", "CvDONK_F" };
                                        }
                                    }
                                    //IP preflop caller
                                    else if (postflopSet.SetType == SetType.PostflopHuIpCaller)
                                    {
                                        if (actionSequences[0] == "x")
                                            cellNames = action.ActionType == 5 ? new string[] { "FLOAT_F", "FLOATX_F", null } : new string[] { "FLOATX_F", "FLOAT_F", null };
                                        else if (actionSequences[0] == "xbr")
                                        {
                                            if (action.ActionType == 2)
                                                cellNames = new string[] { "FLOAT_F_F", "FLOAT_C_F", null };
                                            else if (action.ActionType == 4)
                                                cellNames = new string[] { "FLOAT_C_F", "FLOAT_F_F", null };
                                        }
                                        else if (actionSequences[0] == "b")
                                        {
                                            if (action.ActionType == 2)
                                                cellNames = new string[] { "FvCB_F", "CvCB_F", "RvCB_F" };
                                            else if (action.ActionType == 4)
                                                cellNames = new string[] { "CvCB_F", "FvCB_F", "RvCB_F" };
                                            else if (action.ActionType == 6)
                                                cellNames = new string[] { "RvCB_F", "FvCB_F", "CvCB_F" };
                                        }
                                    }
                                    //OOP preflop raiser
                                    else if (postflopSet.SetType == SetType.PostflopHuOopRaiser)
                                    {
                                        if (actionSequences[0] == string.Empty)
                                            cellNames = action.ActionType == 5 ? new string[] { "CB_F", "CX_F", null } : new string[] { "CX_F", "CB_F", null };
                                        else if (actionSequences[0] == "xb")
                                        {
                                            if (action.ActionType == 2)
                                                cellNames = new string[] { "FvFLOAT_F", "CvFLOAT_F", "RvFLOAT_F" };
                                            else if (action.ActionType == 4)
                                                cellNames = new string[] { "CvFLOAT_F", "FvFLOAT_F", "RvFLOAT_F" };
                                            else if (action.ActionType == 6)
                                                cellNames = new string[] { "RvFLOAT_F", "FvFLOAT_F", "CvFLOAT_F" };
                                        }
                                        else if (actionSequences[0] == "br")
                                        {
                                            if (action.ActionType == 2)
                                                cellNames = new string[] { "FCBvR_F", "CCBvR_F", null };
                                            else if (action.ActionType == 4)
                                                cellNames = new string[] { "CCBvR_F", "FCBvR_F", null };
                                        }
                                    }
                                    //OOP preflop caller
                                    else if (postflopSet.SetType == SetType.PostflopHuOopCaller)
                                    {
                                        if (actionSequences[0] == string.Empty)
                                            cellNames = action.ActionType == 5 ? new string[] { "DONK_F", "DONKX_F", null } : new string[] { "DONKX_F", "DONK_F", null };
                                        else if (actionSequences[0] == "xb")
                                        {
                                            if (action.ActionType == 2)
                                                cellNames = new string[] { "FvCB_F", "CvCB_F", "RvCB_F" };
                                            else if (action.ActionType == 4)
                                                cellNames = new string[] { "CvCB_F", "FvCB_F", "RvCB_F" };
                                            else if (action.ActionType == 6)
                                                cellNames = new string[] { "RvCB_F", "FvCB_F", "CvCB_F" };
                                        }
                                        else if (actionSequences[0] == "br")
                                        {
                                            if (action.ActionType == 2)
                                                cellNames = new string[] { "DONK_F_F", "DONK_C_F", null };
                                            else if (action.ActionType == 4)
                                                cellNames = new string[] { "DONK_C_F", "DONK_F_F", null };
                                        }
                                    }
                                }
                            }
                            //Turn
                            else if (action.Round == 3)
                            {
                                if (actionSequences[1] is not ("brr" or "xbrr"))
                                {
                                    //IP preflop raiser
                                    if (postflopSet.SetType == SetType.PostflopHuIpRaiser)
                                    {
                                        if (actionSequences[1] == "x")
                                        {
                                            if (actionSequences[0] == "xx")
                                                cellNames = action.ActionType == 5 ? new string[] { "DELAY_T", "DELAYX_T", null } : new string[] { "DELAYX_T", "DELAY_T", null };
                                            else if (actionSequences[0] == "xbc")
                                                cellNames = action.ActionType == 5 ? new string[] { "CB_T", "CX_T", null } : new string[] { "CX_T", "CB_T", null };
                                            else if (actionSequences[0] == "xbrc")
                                                cellNames = action.ActionType == 5 ? new string[] { "FLOATXRCB_T", "FLOATXRCBX_T", null } : new string[] { "FLOATXRCBX_T", "FLOATXRCB_T", null };
                                            else if (actionSequences[0] == "bc")
                                                cellNames = action.ActionType == 5 ? new string[] { "FLOATDONK_T", "FLOATDONKX_T", null } : new string[] { "FLOATDONKX_T", "FLOATDONK_T", null };
                                            else if (actionSequences[0] == "brc")
                                                cellNames = action.ActionType == 5 ? new string[] { "RvDONK_F_B", "RvDONK_F_X", null } : new string[] { "RvDONK_F_X", "RvDONK_F_B", null };
                                        }
                                        else if (actionSequences[1] == "xbr")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "DELAY_F_T", "DELAY_C_T", null };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "DELAY_C_T", "DELAY_F_T", null };
                                            }
                                            else if (actionSequences[0] == "xbc")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "FCBvR_T", "CCBvR_T", null };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "CCBvR_T", "FCBvR_T", null };
                                            }
                                        }
                                        else if (actionSequences[1] == "b")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "FvPROBE_T", "CvPROBE_T", "RvPROBE_T" };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "CvPROBE_T", "FvPROBE_T", "RvPROBE_T" };
                                                else if (action.ActionType == 6)
                                                    cellNames = new string[] { "RvPROBE_T", "FvPROBE_T", "CvPROBE_T" };
                                            }
                                            else if (actionSequences[0] == "xbc")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "FvDONK_T", "CvDONK_T", "RvDONK_T" };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "CvDONK_T", "FvDONK_T", "RvDONK_T" };
                                                else if (action.ActionType == 6)
                                                    cellNames = new string[] { "RvDONK_T", "FvDONK_T", "CvDONK_T" };
                                            }
                                            else if (actionSequences[0] == "xbrc")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "FCBvR_F_B", "CCBvR_F_B", null };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "CCBvR_F_B", "FCBvR_F_B", null };
                                            }
                                            else if (actionSequences[0] == "bc")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "FvDONK_F_B", "CvDONK_F_B", null };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "CvDONK_F_B", "FvDONK_F_B", null };
                                            }
                                        }
                                    }
                                    //IP preflop caller
                                    else if (postflopSet.SetType == SetType.PostflopHuIpCaller)
                                    {
                                        if (actionSequences[1] == "x")
                                        {
                                            if (actionSequences[0] == "xx")
                                                cellNames = action.ActionType == 5 ? new string[] { "DelFLOAT_T", "DelFLOATX_T", null } : new string[] { "DelFLOATX_T", "DelFLOAT_T", null };
                                            else if (actionSequences[0] == "xbc")
                                                cellNames = action.ActionType == 5 ? new string[] { "FLOAT_F_B", "FLOAT_F_X", null } : new string[] { "FLOAT_F_X", "FLOAT_F_B", null };
                                            else if (actionSequences[0] == "bc")
                                                cellNames = action.ActionType == 5 ? new string[] { "FLOAT_T", "FLOATX_T", null } : new string[] { "FLOATX_T", "FLOAT_T", null };
                                            else if (actionSequences[0] == "brc")
                                                cellNames = action.ActionType == 5 ? new string[] { "RvCB_F_B", "RvCB_F_X", null } : new string[] { "RvCB_F_X", "RvCB_F_B", null };
                                        }
                                        else if (actionSequences[1] == "xbr")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "DelFLOAT_F_T", "DelFLOAT_C_T", null };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "DelFLOAT_C_T", "DelFLOAT_F_T", null };
                                            }
                                            else if (actionSequences[0] == "bc")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "FLOAT_F_T", "FLOAT_C_T", null };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "FLOAT_C_T", "FLOAT_F_T", null };
                                            }
                                        }
                                        else if (actionSequences[1] == "b")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "FvDELAY_T", "CvDELAY_T", "RvDELAY_T" };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "CvDELAY_T", "FvDELAY_T", "RvDELAY_T" };
                                                else if (action.ActionType == 6)
                                                    cellNames = new string[] { "RvDELAY_T", "FvDELAY_T", "CvDELAY_T" };
                                            }
                                            else if (actionSequences[0] == "xbrc")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "FLOAT_F_F_B", "FLOAT_C_F_B", null };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "FLOAT_C_F_B", "FLOAT_F_F_B", null };
                                            }
                                            else if (actionSequences[0] == "bc")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "FvCB_T", "CvCB_T", "RvCB_T" };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "CvCB_T", "FvCB_T", "RvCB_T" };
                                                else if (action.ActionType == 6)
                                                    cellNames = new string[] { "RvCB_T", "FvCB_T", "CvCB_T" };
                                            }
                                        }
                                    }
                                    //OOP preflop raiser
                                    else if (postflopSet.SetType == SetType.PostflopHuOopRaiser)
                                    {
                                        if (actionSequences[1] == string.Empty)
                                        {
                                            if (actionSequences[0] == "xx")
                                                cellNames = action.ActionType == 5 ? new string[] { "DELAY_T", "DELAYX_T", null } : new string[] { "DELAYX_T", "DELAY_T", null };
                                            else if (actionSequences[0] == "xbrc")
                                                cellNames = action.ActionType == 5 ? new string[] { "RvFLOAT_F_B", "RvFLOAT_F_X", null } : new string[] { "RvFLOAT_F_X", "RvFLOAT_F_B", null };
                                            else if (actionSequences[0] == "bc")
                                                cellNames = action.ActionType == 5 ? new string[] { "CB_T", "CX_T", null } : new string[] { "CX_T", "CB_T", null };
                                        }
                                        else if (actionSequences[1] == "xb")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "FvDelFLOAT_T", "CvDelFLOAT_T", "RvDelFLOAT_T" };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "CvDelFLOAT_T", "FvDelFLOAT_T", "RvDelFLOAT_T" };
                                                else if (action.ActionType == 6)
                                                    cellNames = new string[] { "RvDelFLOAT_T", "FvDelFLOAT_T", "CvDelFLOAT_T" };
                                            }
                                            else if (actionSequences[0] == "xbc")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "FvFLOAT_F_B", "CvFLOAT_F_B", null };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "CvFLOAT_F_B", "FvFLOAT_F_B", null };
                                            }
                                            else if (actionSequences[0] == "bc")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "FvFLOAT_T", "CvFLOAT_T", "RvFLOAT_T" };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "CvFLOAT_T", "FvFLOAT_T", "RvFLOAT_T" };
                                                else if (action.ActionType == 6)
                                                    cellNames = new string[] { "RvFLOAT_T", "FvFLOAT_T", "CvFLOAT_T" };
                                            }
                                            else if (actionSequences[0] == "brc")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "FCBvR_F_B", "CCBvR_F_B", null };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "CCBvR_F_B", "FCBvR_F_B", null };
                                            }
                                        }
                                        else if (actionSequences[1] == "br")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "DELAY_F_T", "DELAY_C_T", null };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "DELAY_C_T", "DELAY_F_T", null };
                                            }
                                            else if (actionSequences[0] == "bc")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "FCBvR_T", "CCBvR_T", null };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "CCBvR_T", "FCBvR_T", null };
                                            }
                                        }
                                    }
                                    //OOP preflop caller
                                    else if (postflopSet.SetType == SetType.PostflopHuOopCaller)
                                    {
                                        if (actionSequences[1] == string.Empty)
                                        {
                                            if (actionSequences[0] == "xx")
                                                cellNames = action.ActionType == 5 ? new string[] { "PROBE_T", "PROBEX_T", null } : new string[] { "PROBEX_T", "PROBE_T", null };
                                            else if (actionSequences[0] == "xbc")
                                                cellNames = action.ActionType == 5 ? new string[] { "DONK_T", "DONKX_T", null } : new string[] { "DONKX_T", "DONK_T", null };
                                            else if (actionSequences[0] == "xbrc")
                                                cellNames = action.ActionType == 5 ? new string[] { "RvCB_F_B", "RvCB_F_X", null } : new string[] { "RvCB_F_X", "RvCB_F_B", null };
                                            else if (actionSequences[0] == "bc")
                                                cellNames = action.ActionType == 5 ? new string[] { "DONK_F_B", "DONK_F_X", null } : new string[] { "DONK_F_X", "DONK_F_B", null };
                                        }
                                        else if (actionSequences[1] == "xb")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "FvDELAY_T", "CvDELAY_T", "RvDELAY_T" };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "CvDELAY_T", "FvDELAY_T", "RvDELAY_T" };
                                                else if (action.ActionType == 6)
                                                    cellNames = new string[] { "RvDELAY_T", "FvDELAY_T", "CvDELAY_T" };
                                            }
                                            else if (actionSequences[0] == "xbc")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "FvCB_T", "CvCB_T", "RvCB_T" };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "CvCB_T", "FvCB_T", "RvCB_T" };
                                                else if (action.ActionType == 6)
                                                    cellNames = new string[] { "RvCB_T", "FvCB_T", "CvCB_T" };
                                            }
                                            else if (actionSequences[0] == "xbrc")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "FvFLOATXRCB_T", "CvFLOATXRCB_T", null };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "CvFLOATXRCB_T", "FvFLOATXRCB_T", null };
                                            }
                                            else if (actionSequences[0] == "bc")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "FvFLOATDONK_T", "CvFLOATDONK_T", null };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "CvFLOATDONK_T", "FvFLOATDONK_T", null };
                                            }
                                            else if (actionSequences[0] == "brc")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "DONK_F_F_B", "DONK_C_F_B", null };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "DONK_C_F_B", "DONK_F_F_B", null };
                                            }
                                        }
                                        else if (actionSequences[1] == "br")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "PROBE_F_T", "PROBE_C_T", null };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "PROBE_C_T", "PROBE_F_T", null };
                                            }
                                            else if (actionSequences[0] == "xbc")
                                            {
                                                if (action.ActionType == 2)
                                                    cellNames = new string[] { "DONK_F_T", "DONK_C_T", null };
                                                else if (action.ActionType == 4)
                                                    cellNames = new string[] { "DONK_C_T", "DONK_F_T", null };
                                            }
                                        }
                                    }
                                }
                            }
                            //River
                            else if (action.Round == 4)
                            {
                                if (actionSequences[2] is not ("brr" or "xbrr"))
                                {
                                    //IP preflop raiser
                                    if (postflopSet.SetType == SetType.PostflopHuIpRaiser)
                                    {
                                        if (actionSequences[2] == "x")
                                        {
                                            if (actionSequences[1] == "xx")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellNames = action.ActionType == 5 ? new string[] { "SDELAY_R", "SDELAYX_R", null } : new string[] { "SDELAYX_R", "SDELAY_R", null };
                                                else if (actionSequences[0] == "xbc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "DELAY_R", "DELAYX_R", null } : new string[] { "DELAYX_R", "DELAY_R", null };
                                                else if (actionSequences[0] == "xbrc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "DelFLOATXRCB_R", "DelFLOATXRCBX_R", null } : new string[] { "DelFLOATXRCBX_R", "DelFLOATXRCB_R", null };
                                                else if (actionSequences[0] == "bc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "DelFLOATDONK_R", "DelFLOATDONKX_R", null } : new string[] { "DelFLOATDONKX_R", "DelFLOATDONK_R", null };
                                                else if (actionSequences[0] == "brc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "RvDONK_F_XB", "RvDONK_F_XX", null } : new string[] { "RvDONK_F_XX", "RvDONK_F_XB", null };
                                            }
                                            else if (actionSequences[1] == "xbc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellNames = action.ActionType == 5 ? new string[] { "DELAY_T_B", "DELAY_T_X", null } : new string[] { "DELAY_T_X", "DELAY_T_B", null };
                                                else if (actionSequences[0] == "xbc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "CB_R", "CX_R", null } : new string[] { "CX_R", "CB_R", null };
                                                else if (actionSequences[0] == "xbrc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "FLOATXRCB_T_B", "FLOATXRCB_T_X", null } : new string[] { "FLOATXRCB_T_X", "FLOATXRCB_T_B", null };
                                                else if (actionSequences[0] == "bc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "FLOATDONK_T_B", "FLOATDONK_T_X", null } : new string[] { "FLOATDONK_T_X", "FLOATDONK_T_B", null };
                                                else if (actionSequences[0] == "brc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "RvDONK_F_BB", "RvDONK_F_BX", null } : new string[] { "RvDONK_F_BX", "RvDONK_F_BB", null };
                                            }
                                            else if (actionSequences[1] == "xbrc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellNames = action.ActionType == 5 ? new string[] { "FLOATXRDELAY_R", "FLOATXRDELAYX_R", null } : new string[] { "FLOATXRDELAYX_R", "FLOATXRDELAY_R", null };
                                                else if (actionSequences[0] == "xbc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "FLOATXRCB_R", "FLOATXRCBX_R", null } : new string[] { "FLOATXRCBX_R", "FLOATXRCB_R", null };
                                            }
                                            else if (actionSequences[1] == "bc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellNames = action.ActionType == 5 ? new string[] { "FLOATPROBE_R", "FLOATPROBEX_R", null } : new string[] { "FLOATPROBEX_R", "FLOATPROBE_R", null };
                                                else if (actionSequences[0] == "xbc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "FLOATDONK_R", "FLOATDONKX_R", null } : new string[] { "FLOATDONKX_R", "FLOATDONK_R", null };
                                                else if (actionSequences[0] == "xbrc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "DblFLOATXRCB_R", "DblFLOATXRCBX_R", null } : new string[] { "DblFLOATXRCBX_R", "DblFLOATXRCB_R", null };
                                                else if (actionSequences[0] == "bc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "DblFLOATDONK_R", "DblFLOATDONKX_R", null } : new string[] { "DblFLOATDONKX_R", "DblFLOATDONK_R", null };
                                            }
                                            else if (actionSequences[1] == "brc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellNames = action.ActionType == 5 ? new string[] { "RvPROBE_T_B", "RvPROBE_T_X", null } : new string[] { "RvPROBE_T_X", "RvPROBE_T_B", null };
                                                else if (actionSequences[0] == "xbc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "RvDONK_T_B", "RvDONK_T_X", null } : new string[] { "RvDONK_T_X", "RvDONK_T_B", null };
                                            }
                                        }
                                        else if (actionSequences[2] == "xbr")
                                        {
                                            if (actionSequences[1] == "xx")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "SDELAY_F_R", "SDELAY_C_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "SDELAY_C_R", "SDELAY_F_R", null };
                                                }
                                                else if (actionSequences[0] == "xbc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "DELAY_F_R", "DELAY_C_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "DELAY_C_R", "DELAY_F_R", null };
                                                }
                                            }
                                            else if (actionSequences[1] == "xbc")
                                            {
                                                if (actionSequences[0] == "xbc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FCBvR_R", "CCBvR_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CCBvR_R", "FCBvR_R", null };
                                                }
                                            }
                                        }
                                        else if (actionSequences[2] == "b")
                                        {
                                            if (actionSequences[1] == "xx")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvSPROBE_R", "CvSPROBE_R", "RvSPROBE_R" };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvSPROBE_R", "FvSPROBE_R", "RvSPROBE_R" };
                                                    else if (action.ActionType == 6)
                                                        cellNames = new string[] { "RvSPROBE_R", "FvSPROBE_R", "CvSPROBE_R" };
                                                }
                                                else if (actionSequences[0] == "xbc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvPROBE_R", "CvPROBE_R", "RvPROBE_R" };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvPROBE_R", "FvPROBE_R", "RvPROBE_R" };
                                                    else if (action.ActionType == 6)
                                                        cellNames = new string[] { "RvPROBE_R", "FvPROBE_R", "CvPROBE_R" };
                                                }
                                                else if (actionSequences[0] == "xbrc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FCBvR_F_XB", "CCBvR_F_XB", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CCBvR_F_XB", "FCBvR_F_XB", null };
                                                }
                                                else if (actionSequences[0] == "bc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvDONK_F_XB", "CvDONK_F_XB", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvDONK_F_XB", "FvDONK_F_XB", null };
                                                }
                                            }
                                            else if (actionSequences[1] == "xbc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvDONKDELAY_R", "CvDONKDELAY_R", "RvDONKDELAY_R" };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvDONKDELAY_R", "FvDONKDELAY_R", "RvDONKDELAY_R" };
                                                    else if (action.ActionType == 6)
                                                        cellNames = new string[] { "RvDONKDELAY_R", "FvDONKDELAY_R", "CvDONKDELAY_R" };
                                                }
                                                else if (actionSequences[0] == "xbc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvDONK_R", "CvDONK_R", "RvDONK_R" };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvDONK_R", "FvDONK_R", "RvDONK_R" };
                                                    else if (action.ActionType == 6)
                                                        cellNames = new string[] { "RvDONK_R", "FvDONK_R", "CvDONK_R" };
                                                }
                                            }
                                            else if (actionSequences[1] == "xbrc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "DELAY_F_T_B", "DELAY_C_T_B", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "DELAY_C_T_B", "DELAY_F_T_B", null };
                                                }
                                                else if (actionSequences[0] == "xbc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FCBvR_T_B", "CCBvR_T_B", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CCBvR_T_B", "FCBvR_T_B", null };
                                                }
                                            }
                                            else if (actionSequences[1] == "bc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvPROBE_T_B", "CvPROBE_T_B", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvPROBE_T_B", "FvPROBE_T_B", null };
                                                }
                                                else if (actionSequences[0] == "xbc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvDONK_T_B", "CvDONK_T_B", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvDONK_T_B", "FvDONK_T_B", null };
                                                }
                                                else if (actionSequences[0] == "xbrc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FCBvR_F_BB", "CCBvR_F_BB", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CCBvR_F_BB", "FCBvR_F_BB", null };
                                                }
                                                else if (actionSequences[0] == "bc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvDONK_F_BB", "CvDONK_F_BB", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvDONK_F_BB", "FvDONK_F_BB", null };
                                                }
                                            }
                                        }
                                    }
                                    //IP preflop caller
                                    else if (postflopSet.SetType == SetType.PostflopHuIpCaller)
                                    {
                                        if (actionSequences[2] == "x")
                                        {
                                            if (actionSequences[1] == "xx")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellNames = action.ActionType == 5 ? new string[] { "SDelFLOAT_R", "SDelFLOATX_R", null } : new string[] { "SDelFLOATX_R", "SDelFLOAT_R", null };
                                                else if (actionSequences[0] == "xbc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "FLOAT_F_XB", "FLOAT_F_XX", null } : new string[] { "FLOAT_F_XX", "FLOAT_F_XB", null };
                                                else if (actionSequences[0] == "bc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "DelFLOAT_R", "DelFLOATX_R", null } : new string[] { "DelFLOATX_R", "DelFLOAT_R", null };
                                                else if (actionSequences[0] == "brc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "RvCB_F_XB", "RvCB_F_XX", null } : new string[] { "RvCB_F_XX", "RvCB_F_XB", null };
                                            }
                                            else if (actionSequences[1] == "xbc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellNames = action.ActionType == 5 ? new string[] { "DelFLOAT_T_B", "DelFLOAT_T_X", null } : new string[] { "DelFLOAT_T_X", "DelFLOAT_T_B", null };
                                                else if (actionSequences[0] == "xbc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "FLOAT_F_BB", "FLOAT_F_BX", null } : new string[] { "FLOAT_F_BX", "FLOAT_F_BB", null };
                                                else if (actionSequences[0] == "bc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "FLOAT_T_B", "FLOAT_T_X", null } : new string[] { "FLOAT_T_X", "FLOAT_T_B", null };
                                                else if (actionSequences[0] == "brc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "RvCB_F_BB", "RvCB_F_BX", null } : new string[] { "RvCB_F_BX", "RvCB_F_BB", null };
                                            }
                                            else if (actionSequences[1] == "bc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellNames = action.ActionType == 5 ? new string[] { "FLOATDELAY_R", "FLOATDELAYX_R", null } : new string[] { "FLOATDELAYX_R", "FLOATDELAY_R", null };
                                                else if (actionSequences[0] == "bc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "FLOAT_R", "FLOATX_R", null } : new string[] { "FLOATX_R", "FLOAT_R", null };
                                            }
                                            else if (actionSequences[1] == "brc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellNames = action.ActionType == 5 ? new string[] { "RvDELAY_T_B", "RvDELAY_T_X", null } : new string[] { "RvDELAY_T_X", "RvDELAY_T_B", null };
                                                else if (actionSequences[0] == "bc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "RvCB_T_B", "RvCB_T_X", null } : new string[] { "RvCB_T_X", "RvCB_T_B", null };
                                            }
                                        }
                                        else if (actionSequences[2] == "xbr")
                                        {
                                            if (actionSequences[1] == "xx")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "SDelFLOAT_F_R", "SDelFLOAT_C_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "SDelFLOAT_C_R", "SDelFLOAT_F_R", null };
                                                }
                                                else if (actionSequences[0] == "bc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "DelFLOAT_F_R", "DelFLOAT_C_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "DelFLOAT_C_R", "DelFLOAT_F_R", null };
                                                }
                                            }
                                            else if (actionSequences[1] == "bc")
                                            {
                                                if (actionSequences[0] == "bc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FLOAT_F_R", "FLOAT_C_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "FLOAT_C_R", "FLOAT_F_R", null };
                                                }
                                            }
                                        }
                                        else if (actionSequences[2] == "b")
                                        {
                                            if (actionSequences[1] == "xx")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvSDELAY_R", "CvSDELAY_R", "RvSDELAY_R" };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvSDELAY_R", "FvSDELAY_R", "RvSDELAY_R" };
                                                    else if (action.ActionType == 6)
                                                        cellNames = new string[] { "RvSDELAY_R", "FvSDELAY_R", "CvSDELAY_R" };
                                                }
                                                else if (actionSequences[0] == "xbrc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FLOAT_F_F_XB", "FLOAT_C_F_XB", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "FLOAT_C_F_XB", "FLOAT_F_F_XB", null };
                                                }
                                                else if (actionSequences[0] == "bc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvDELAY_R", "CvDELAY_R", "RvDELAY_R" };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvDELAY_R", "FvDELAY_R", "RvDELAY_R" };
                                                    else if (action.ActionType == 6)
                                                        cellNames = new string[] { "RvDELAY_R", "FvDELAY_R", "CvDELAY_R" };
                                                }
                                            }
                                            else if (actionSequences[1] == "xbrc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "DelFLOAT_F_T_B", "DelFLOAT_C_T_B", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "DelFLOAT_C_T_B", "DelFLOAT_F_T_B", null };
                                                }
                                                else if (actionSequences[0] == "bc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FLOAT_F_T_B", "FLOAT_C_T_B", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "FLOAT_C_T_B", "FLOAT_F_T_B", null };
                                                }
                                            }
                                            else if (actionSequences[1] == "bc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvDELAY_T_B", "CvDELAY_T_B", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvDELAY_T_B", "FvDELAY_T_B", null };
                                                }
                                                else if (actionSequences[0] == "xbrc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FLOAT_F_F_BB", "FLOAT_C_F_BB", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "FLOAT_C_F_BB", "FLOAT_F_F_BB", null };
                                                }
                                                else if (actionSequences[0] == "bc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvCB_R", "CvCB_R", "RvCB_R" };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvCB_R", "FvCB_R", "RvCB_R" };
                                                    else if (action.ActionType == 6)
                                                        cellNames = new string[] { "RvCB_R", "FvCB_R", "CvCB_R" };
                                                }
                                            }
                                        }
                                    }
                                    //OOP preflop raiser
                                    else if (postflopSet.SetType == SetType.PostflopHuOopRaiser)
                                    {
                                        if (actionSequences[2] == string.Empty)
                                        {
                                            if (actionSequences[1] == "xx")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellNames = action.ActionType == 5 ? new string[] { "SDELAY_R", "SDELAYX_R", null } : new string[] { "SDELAYX_R", "SDELAY_R", null };
                                                else if (actionSequences[0] == "xbrc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "RvFLOAT_F_XB", "RvFLOAT_F_XX", null } : new string[] { "RvFLOAT_F_XX", "RvFLOAT_F_XB", null };
                                                else if (actionSequences[0] == "bc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "DELAY_R", "DELAYX_R", null } : new string[] { "DELAYX_R", "DELAY_R", null };
                                            }
                                            else if (actionSequences[1] == "xbrc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellNames = action.ActionType == 5 ? new string[] { "RvDelFLOAT_T_B", "RvDelFLOAT_T_X", null } : new string[] { "RvDelFLOAT_T_X", "RvDelFLOAT_T_B", null };
                                                else if (actionSequences[0] == "bc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "RvFLOAT_T_B", "RvFLOAT_T_X", null } : new string[] { "RvFLOAT_T_X", "RvFLOAT_T_B", null };
                                            }
                                            else if (actionSequences[1] == "bc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellNames = action.ActionType == 5 ? new string[] { "DELAY_T_B", "DELAY_T_X", null } : new string[] { "DELAY_T_X", "DELAY_T_B", null };
                                                else if (actionSequences[0] == "xbrc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "RvFLOAT_F_BB", "RvFLOAT_F_BX", null } : new string[] { "RvFLOAT_F_BX", "RvFLOAT_F_BB", null };
                                                else if (actionSequences[0] == "bc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "CB_R", "CX_R", null } : new string[] { "CX_R", "CB_R", null };
                                            }
                                        }
                                        else if (actionSequences[2] == "xb")
                                        {
                                            if (actionSequences[1] == "xx")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvSDelFLOAT_R", "CvSDelFLOAT_R", "RvSDelFLOAT_R" };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvSDelFLOAT_R", "FvSDelFLOAT_R", "RvSDelFLOAT_R" };
                                                    else if (action.ActionType == 6)
                                                        cellNames = new string[] { "RvSDelFLOAT_R", "FvSDelFLOAT_R", "CvSDelFLOAT_R" };
                                                }
                                                else if (actionSequences[0] == "xbc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvFLOAT_F_XB", "CvFLOAT_F_XB", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvFLOAT_F_XB", "FvFLOAT_F_XB", null };
                                                }
                                                else if (actionSequences[0] == "bc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvDelFLOAT_R", "CvDelFLOAT_R", "RvDelFLOAT_R" };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvDelFLOAT_R", "FvDelFLOAT_R", "RvDelFLOAT_R" };
                                                    else if (action.ActionType == 6)
                                                        cellNames = new string[] { "RvDelFLOAT_R", "FvDelFLOAT_R", "CvDelFLOAT_R" };
                                                }
                                                else if (actionSequences[0] == "brc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FCBvR_F_XB", "CCBvR_F_XB", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CCBvR_F_XB", "FCBvR_F_XB", null };
                                                }
                                            }
                                            else if (actionSequences[1] == "xbc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvDelFLOAT_T_B", "CvDelFLOAT_T_B", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvDelFLOAT_T_B", "FvDelFLOAT_T_B", null };
                                                }
                                                else if (actionSequences[0] == "xbc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvFLOAT_F_BB", "CvFLOAT_F_BB", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvFLOAT_F_BB", "FvFLOAT_F_BB", null };
                                                }
                                                else if (actionSequences[0] == "bc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvFLOAT_T_B", "CvFLOAT_T_B", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvFLOAT_T_B", "FvFLOAT_T_B", null };
                                                }
                                                else if (actionSequences[0] == "brc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FCBvR_F_BB", "CCBvR_F_BB", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CCBvR_F_BB", "FCBvR_F_BB", null };
                                                }
                                            }
                                            else if (actionSequences[1] == "bc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvFLOATDELAY_R", "CvFLOATDELAY_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvFLOATDELAY_R", "FvFLOATDELAY_R", null };
                                                }
                                                else if (actionSequences[0] == "bc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvFLOAT_R", "CvFLOAT_R", "RvFLOAT_R" };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvFLOAT_R", "FvFLOAT_R", "RvFLOAT_R" };
                                                    else if (action.ActionType == 6)
                                                        cellNames = new string[] { "RvFLOAT_R", "FvFLOAT_R", "CvFLOAT_R" };
                                                }
                                            }
                                            else if (actionSequences[1] == "brc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "DELAY_F_T_B", "DELAY_C_T_B", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "DELAY_C_T_B", "DELAY_F_T_B", null };
                                                }
                                                else if (actionSequences[0] == "bc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FCBvR_T_B", "CCBvR_T_B", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CCBvR_T_B", "FCBvR_T_B", null };
                                                }
                                            }
                                        }
                                        else if (actionSequences[2] == "br")
                                        {
                                            if (actionSequences[1] == "xx")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "SDELAY_F_R", "SDELAY_C_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "SDELAY_C_R", "SDELAY_F_R", null };
                                                }
                                                else if (actionSequences[0] == "bc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "DELAY_F_R", "DELAY_C_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "DELAY_C_R", "DELAY_F_R", null };
                                                }
                                            }
                                            else if (actionSequences[1] == "bc")
                                            {
                                                if (actionSequences[0] == "bc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FCBvR_R", "CCBvR_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CCBvR_R", "FCBvR_R", null };
                                                }
                                            }
                                        }
                                    }
                                    //OOP preflop caller
                                    else if (postflopSet.SetType == SetType.PostflopHuOopCaller)
                                    {
                                        if (actionSequences[2] == string.Empty)
                                        {
                                            if (actionSequences[1] == "xx")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellNames = action.ActionType == 5 ? new string[] { "SPROBE_R", "SPROBEX_R", null } : new string[] { "SPROBEX_R", "SPROBE_R", null };
                                                else if (actionSequences[0] == "xbc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "PROBE_R", "PROBEX_R", null } : new string[] { "PROBEX_R", "PROBE_R", null };
                                                else if (actionSequences[0] == "xbrc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "RvCB_F_XB", "RvCB_F_XX", null } : new string[] { "RvCB_F_XX", "RvCB_F_XB", null };
                                                else if (actionSequences[0] == "bc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "DONK_F_XB", "DONK_F_XX", null } : new string[] { "DONK_F_XX", "DONK_F_XB", null };
                                            }
                                            else if (actionSequences[1] == "xbc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellNames = action.ActionType == 5 ? new string[] { "DONKDELAY_R", "DONKDELAYX_R", null } : new string[] { "DONKDELAYX_R", "DONKDELAY_R", null };
                                                else if (actionSequences[0] == "xbc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "DONK_R", "DONKX_R", null } : new string[] { "DONKX_R", "DONK_R", null };
                                            }
                                            else if (actionSequences[1] == "xbrc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellNames = action.ActionType == 5 ? new string[] { "RvDELAY_T_B", "RvDELAY_T_X", null } : new string[] { "RvDELAY_T_X", "RvDELAY_T_B", null };
                                                else if (actionSequences[0] == "xbc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "RvCB_T_B", "RvCB_T_X", null } : new string[] { "RvCB_T_X", "RvCB_T_B", null };
                                            }
                                            else if (actionSequences[1] == "bc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellNames = action.ActionType == 5 ? new string[] { "PROBE_T_B", "PROBE_T_X", null } : new string[] { "PROBE_T_X", "PROBE_T_B", null };
                                                else if (actionSequences[0] == "xbc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "DONK_T_B", "DONK_T_X", null } : new string[] { "DONK_T_X", "DONK_T_B", null };
                                                else if (actionSequences[0] == "xbrc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "RvCB_F_BB", "RvCB_F_BX", null } : new string[] { "RvCB_F_BX", "RvCB_F_BB", null };
                                                else if (actionSequences[0] == "bc")
                                                    cellNames = action.ActionType == 5 ? new string[] { "DONK_F_BB", "DONK_F_BX", null } : new string[] { "DONK_F_BX", "DONK_F_BB", null };
                                            }
                                        }
                                        else if (actionSequences[2] == "xb")
                                        {
                                            if (actionSequences[1] == "xx")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvSDELAY_R", "CvSDELAY_R", "RvSDELAY_R" };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvSDELAY_R", "FvSDELAY_R", "RvSDELAY_R" };
                                                    else if (action.ActionType == 6)
                                                        cellNames = new string[] { "RvSDELAY_R", "FvSDELAY_R", "CvSDELAY_R" };
                                                }
                                                else if (actionSequences[0] == "xbc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvDELAY_R", "CvDELAY_R", "RvDELAY_R" };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvDELAY_R", "FvDELAY_R", "RvDELAY_R" };
                                                    else if (action.ActionType == 6)
                                                        cellNames = new string[] { "RvDELAY_R", "FvDELAY_R", "CvDELAY_R" };
                                                }
                                                else if (actionSequences[0] == "xbrc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvDelFLOATXRCB_R", "CvDelFLOATXRCB_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvDelFLOATXRCB_R", "FvDelFLOATXRCB_R", null };
                                                }
                                                else if (actionSequences[0] == "bc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvDelFLOATDONK_R", "CvDelFLOATDONK_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvDelFLOATDONK_R", "FvDelFLOATDONK_R", null };
                                                }
                                                else if (actionSequences[0] == "brc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "DONK_F_F_XB", "DONK_C_F_XB", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "DONK_C_F_XB", "DONK_F_F_XB", null };
                                                }
                                            }
                                            else if (actionSequences[1] == "xbc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvDELAY_T_B", "CvDELAY_T_B", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvDELAY_T_B", "FvDELAY_T_B", null };
                                                }
                                                else if (actionSequences[0] == "xbc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvCB_R", "CvCB_R", "RvCB_R" };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvCB_R", "FvCB_R", "RvCB_R" };
                                                    else if (action.ActionType == 6)
                                                        cellNames = new string[] { "RvCB_R", "FvCB_R", "CvCB_R" };
                                                }
                                                else if (actionSequences[0] == "xbrc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvFLOATXRCB_T_B", "CvFLOATXRCB_T_B", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvFLOATXRCB_T_B", "FvFLOATXRCB_T_B", null };
                                                }
                                                else if (actionSequences[0] == "bc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvFLOATDONK_T_B", "CvFLOATDONK_T_B", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvFLOATDONK_T_B", "FvFLOATDONK_T_B", null };
                                                }
                                                else if (actionSequences[0] == "brc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "DONK_F_F_BB", "DONK_C_F_BB", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "DONK_C_F_BB", "DONK_F_F_BB", null };
                                                }
                                            }
                                            else if (actionSequences[1] == "xbrc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvFLOATXRDELAY_R", "CvFLOATXRDELAY_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvFLOATXRDELAY_R", "FvFLOATXRDELAY_R", null };
                                                }
                                                else if (actionSequences[0] == "xbc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvFLOATXRCB_R", "CvFLOATXRCB_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvFLOATXRCB_R", "FvFLOATXRCB_R", null };
                                                }
                                            }
                                            else if (actionSequences[1] == "bc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvFLOATPROBE_R", "CvFLOATPROBE_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvFLOATPROBE_R", "FvFLOATPROBE_R", null };
                                                }
                                                else if (actionSequences[0] == "xbc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvFLOATDONK_R", "CvFLOATDONK_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvFLOATDONK_R", "FvFLOATDONK_R", null };
                                                }
                                                else if (actionSequences[0] == "xbrc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvDblFLOATXRCB_R", "CvDblFLOATXRCB_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvDblFLOATXRCB_R", "FvDblFLOATXRCB_R", null };
                                                }
                                                else if (actionSequences[0] == "bc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "FvDblFLOATDONK_R", "CvDblFLOATDONK_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "CvDblFLOATDONK_R", "FvDblFLOATDONK_R", null };
                                                }
                                            }
                                            else if (actionSequences[1] == "brc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "PROBE_F_T_B", "PROBE_C_T_B", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "PROBE_C_T_B", "PROBE_F_T_B", null };
                                                }
                                                else if (actionSequences[0] == "xbc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "DONK_F_T_B", "DONK_C_T_B", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "DONK_C_T_B", "DONK_F_T_B", null };
                                                }
                                            }
                                        }
                                        else if (actionSequences[2] == "br")
                                        {
                                            if (actionSequences[1] == "xx")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "SPROBE_F_R", "SPROBE_C_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "SPROBE_C_R", "SPROBE_F_R", null };
                                                }
                                                else if (actionSequences[0] == "xbc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "PROBE_F_R", "PROBE_C_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "PROBE_C_R", "PROBE_F_R", null };
                                                }
                                            }
                                            else if (actionSequences[1] == "xbc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "DONKDELAY_F_R", "DONKDELAY_C_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "DONKDELAY_C_R", "DONKDELAY_F_R", null };
                                                }
                                                else if (actionSequences[0] == "xbc")
                                                {
                                                    if (action.ActionType == 2)
                                                        cellNames = new string[] { "DONK_F_R", "DONK_C_R", null };
                                                    else if (action.ActionType == 4)
                                                        cellNames = new string[] { "DONK_C_R", "DONK_F_R", null };
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        postflopCellsDict.TryGetValue($"{cellNames[0]}", out DataCell postflopCell);
                        postflopCellsDict.TryGetValue($"{altCellNames[0]}", out DataCell altPostflopCell);

                        if (postflopCell != null)
                        {
                            postflopCell.IncrementSample();
                            postflopCellsDict[$"{cellNames[1]}"].IncrementSample();

                            if (cellNames[2] != null)
                            {
                                if (action.ActionType == 6 || action.CanRaise)
                                    postflopCellsDict[$"{cellNames[2]}"].IncrementSample();
                            }

                            postflopCell.IncrementValue();

                            PostflopData postflopData = (PostflopData)postflopCell.CellData;

                            PostflopHandsGroup postflopHandsMainGroup = (PostflopHandsGroup)postflopData.MainGroup;

                            AddCounterAction(postflopHandsMainGroup, i, action.Round);

                            AddPostflopHand(postflopHandsMainGroup);

                            if (postflopCell.BetRanges != null)
                            {
                                float amount = 0;

                                if (postflopCell.BetType == 'b')
                                    amount = (roundBets[playerPosition - 1] + action.Amount) / bbAmount;
                                else if (postflopCell.BetType == 'x')
                                    amount = (roundBets[playerPosition - 1] + action.Amount) / currentBet;
                                else if (postflopCell.BetType == 'p')
                                {
                                    float callAmount = currentBet - roundBets[playerPosition - 1];

                                    amount = (action.Amount - callAmount) * 100 / (pot + callAmount);
                                }

                                if (amount > 0)
                                {
                                    for (int j = 0; j < postflopCell.BetRanges.Length; j++)
                                    {
                                        if (amount >= postflopCell.BetRanges[j].LowBound && amount < postflopCell.BetRanges[j].UpperBound)
                                        {
                                            PostflopHandsGroup postflopHandsSubGroup = (PostflopHandsGroup)postflopData.SubGroups[j];

                                            AddCounterAction(postflopHandsSubGroup, i, action.Round);

                                            AddPostflopHand(postflopHandsSubGroup);

                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (altPostflopCell != null)
                        {
                            altPostflopCell.IncrementSample();
                            postflopCellsDict[$"{altCellNames[1]}"].IncrementSample();

                            if (altCellNames[2] != null)
                            {
                                if (action.ActionType == 6 || action.CanRaise)
                                    postflopCellsDict[$"{altCellNames[2]}"].IncrementSample();
                            }

                            altPostflopCell.IncrementValue();

                            PostflopData postflopData = (PostflopData)altPostflopCell.CellData;

                            PostflopHandsGroup postflopHandsMainGroup = (PostflopHandsGroup)postflopData.MainGroup;

                            AddCounterAction(postflopHandsMainGroup, i, action.Round);

                            AddPostflopHand(postflopHandsMainGroup);

                            if (altPostflopCell.BetRanges != null)
                            {
                                float amount = 0;

                                if (altPostflopCell.BetType == 'b')
                                    amount = (roundBets[playerPosition - 1] + action.Amount) / bbAmount;
                                else if (altPostflopCell.BetType == 'x')
                                    amount = (roundBets[playerPosition - 1] + action.Amount) / currentBet;
                                else if (altPostflopCell.BetType == 'p')
                                {
                                    float callAmount = currentBet - roundBets[playerPosition - 1];

                                    amount = (action.Amount - callAmount) * 100 / (pot + callAmount);
                                }

                                if (amount > 0)
                                {
                                    for (int j = 0; j < altPostflopCell.BetRanges.Length; j++)
                                    {
                                        if (amount >= altPostflopCell.BetRanges[j].LowBound && amount < altPostflopCell.BetRanges[j].UpperBound)
                                        {
                                            PostflopHandsGroup postflopHandsSubGroup = (PostflopHandsGroup)postflopData.SubGroups[j];

                                            AddCounterAction(postflopHandsSubGroup, i, action.Round);

                                            AddPostflopHand(postflopHandsSubGroup);

                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        void AddPostflopHand(PostflopHandsGroup handsGroup)
                        {
                            if (double.IsNaN(handEquity) || handType == HandType.None)
                                return;

                            int handIndex = 0;

                            if (handEquity > 0)
                                handIndex = (int)handEquity - 1;

                            if (handType == HandType.MadeHand)
                                handsGroup.MadeHands[handIndex]++;
                            else if (handType == HandType.DrawHand)
                                handsGroup.DrawHands[handIndex]++;
                            else if (handType == HandType.ComboHand)
                                handsGroup.ComboHands[handIndex]++;
                        }
                    }

                    //Update variables
                    pot += action.Amount;

                    roundBets[action.Position - 1] += action.Amount;

                    if (roundBets[action.Position - 1] + action.Amount > currentBet)
                        currentBet = roundBets[action.Position - 1] + action.Amount;

                    //Update pot type
                    string actionAbbr = string.Empty;

                    if (action.ActionType == 2)
                        actionAbbr = "f";
                    else if (action.ActionType == 3)
                        actionAbbr = "x";
                    else if (action.ActionType == 4)
                        actionAbbr = "c";
                    else if (action.ActionType == 5)
                        actionAbbr = "b";
                    else if (action.ActionType == 6)
                        actionAbbr = "r";

                    actionSequences[action.Round - 2] += actionAbbr;
                }

                #endregion

                void AddCounterAction(HandsGroupBase handsGroup, int index, int round)
                {
                    for (int j = index + 1; j < actions.Length; j++)
                    {
                        if (actions[j].Round > round)
                            break;

                        if (actions[j].Position == playerPosition)
                        {
                            if (actions[j].ActionType == 2)
                                handsGroup.CounterActions[0]++;
                            else if (actions[j].ActionType == 4)
                                handsGroup.CounterActions[1]++;
                            else if (actions[j].ActionType == 6)
                                handsGroup.CounterActions[2]++;

                            break;
                        }
                    }
                }
            }
        }
    }
}
