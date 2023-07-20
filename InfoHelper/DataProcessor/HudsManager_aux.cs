using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GameInformationUtility;
using InfoHelper.StatsEntities;
using InfoHelper.Utils;
using InfoHelper.ViewModel.States;
using StatUtility;

namespace InfoHelper.DataProcessor
{
    public partial class HudsManager
    {
        private partial DataCell ProcessSelectedHuds(GameContext gc, ViewModelStatsHud preflopHud, ViewModelStatsHud postflopHud)
        {
            string cellName = string.Empty;

            Position playerPosition = Position.Any;

            switch (preflopHud?.SetType)
            {
                case SetType.PreflopBtn:
                    playerPosition = Position.Btn;
                    break;

                case SetType.PreflopSb:
                case SetType.PreflopSbvsBb:
                    playerPosition = Position.Sb;
                    break;

                case SetType.PreflopBb:
                case SetType.PreflopBbvsSb:
                    playerPosition = Position.Bb;
                    break;

                case SetType.PreflopEp:
                    playerPosition = Position.Ep;
                    break;

                case SetType.PreflopMp:
                    playerPosition = Position.Mp;
                    break;

                case SetType.PreflopCo:
                    playerPosition = Position.Co;
                    break;
            }

            bool[] initialPlayers = gc.InitialStacks.Select(s => s != null).ToArray();

            Position heroPosition = Common.GetPlayerPosition(new int[] { gc.SmallBlindPosition, gc.BigBlindPosition, gc.ButtonPosition }, gc.HeroPosition, initialPlayers);

            BettingAction[] preflopActions = gc.Actions.Where(a => a.Round == 1 && a.ActionType != BettingActionType.PostSb && a.ActionType != BettingActionType.PostBb).ToArray();

            int player = 0;

            foreach (BettingAction action in preflopActions)
            {
                if (action.Player == gc.HeroPosition)
                    continue;

                if (Common.GetPlayerPosition(new int[] { gc.SmallBlindPosition, gc.BigBlindPosition, gc.ButtonPosition }, action.Player, initialPlayers) == playerPosition)
                {
                    player = action.Player;

                    break;
                }
            }

            #region Preflop

            if (preflopHud == null)
                goto Postflop;

            HashSet<string> preflopRows = new HashSet<string>();

            GameType gameType = gc.SmallBlindPosition == gc.ButtonPosition ? GameType.Hu : GameType.SixMax;

            PreflopActions preflActions = PreflopActions.NoActions;
            PreflopPotType preflopPotType = PreflopPotType.Unopened;

            OtherPlayersActed otherPlayersActed = OtherPlayersActed.No;

            int callCount = 0;
            int raiseCount = 0;

            foreach (BettingAction action in preflopActions)
            {
                if (action.Player == player)
                {
                    if (preflopPotType == PreflopPotType.Unopened)
                    {
                        if (preflActions == PreflopActions.NoActions)
                        {
                            cellName = gameType == GameType.Hu ? $"SbvsBb_Unopened_{action.ActionType}" : $"{playerPosition}_Unopened_{action.ActionType}";

                            preflActions = Enum.Parse<PreflopActions>($"{action.ActionType}");
                        }
                    }
                    else if (preflopPotType == PreflopPotType.LimpPot)
                    {
                        if (preflActions == PreflopActions.NoActions)
                        {
                            if (callCount == 1)
                            {
                                if (gameType == GameType.Hu)
                                {
                                    preflopRows.Add("BbvsSb_Limp_Row");

                                    cellName = $"BbvsSb_Limp_{action.ActionType}";
                                }
                                else
                                {
                                    Position limperPosition = Common.GetPlayerPosition(new int[] { gc.SmallBlindPosition, gc.BigBlindPosition, gc.ButtonPosition }, preflopActions.First(a => a.ActionType == BettingActionType.Call).Player, initialPlayers);

                                    preflopRows.Add($"{playerPosition}_Limp_Vs_{limperPosition}_Row");

                                    cellName = $"{playerPosition}_Limp_Vs_{limperPosition}_{action.ActionType}";
                                }
                            }
                            else
                            {
                                preflopRows.Add($"{playerPosition}_Limp_Vs_Multi_Row");

                                cellName = $"{playerPosition}_Limp_Vs_Multi_{action.ActionType}";
                            }

                            preflActions = Enum.Parse<PreflopActions>($"{action.ActionType}");
                        }
                    }
                    else if (preflopPotType == PreflopPotType.RaisePot)
                    {
                        if (preflActions == PreflopActions.NoActions)
                        {
                            if (callCount == 0)
                            {
                                if (gameType == GameType.Hu)
                                {
                                    preflopRows.Add("BbvsSb_Raise_Row");

                                    cellName = $"BbvsSb_Raise_{action.ActionType}";
                                }
                                else
                                {
                                    Position raiserPosition = Common.GetPlayerPosition(new int[] { gc.SmallBlindPosition, gc.BigBlindPosition, gc.ButtonPosition }, preflopActions.First(a => a.ActionType == BettingActionType.Raise).Player, initialPlayers);

                                    preflopRows.Add($"{playerPosition}_Raise_Vs_{raiserPosition}_Row");

                                    cellName = $"{playerPosition}_Raise_Vs_{raiserPosition}_{action.ActionType}";
                                }
                            }
                            else
                            {
                                preflopRows.Add($"{playerPosition}_Raise_Vs_Multi_Row");

                                cellName = $"{playerPosition}_Raise_Vs_Multi_{action.ActionType}";
                            }

                            preflActions = Enum.Parse<PreflopActions>($"{action.ActionType}");
                        }
                    }
                    else if (preflopPotType == PreflopPotType.IsolatePot)
                    {
                        if (preflActions == PreflopActions.NoActions)
                        {
                            preflopRows.Add($"{playerPosition}_Isolate_CC_Row");

                            cellName = $"{playerPosition}_Isolate_CC_{action.ActionType}";

                            preflActions = Enum.Parse<PreflopActions>($"{action.ActionType}");
                        }
                        else if (preflActions == PreflopActions.Call)
                        {
                            if (gameType == GameType.Hu)
                            {
                                preflopRows.Add("SbvsBb_Isolate_Row");

                                cellName = $"SbvsBb_Isolate_{action.ActionType}";
                            }
                            else
                            {
                                preflopRows.Add($"{playerPosition}_Isolate_Limper_Row");

                                cellName = $"{playerPosition}_Isolate_Limper_{action.ActionType}";

                                if (otherPlayersActed == OtherPlayersActed.No)
                                {
                                    if (playerPosition == Position.Sb && heroPosition == Position.Bb)
                                    {
                                        preflopRows.Add($"{playerPosition}_Isolate_Limper_Vs_{heroPosition}_Row");

                                        cellName = $"{playerPosition}_Isolate_Limper_Vs_{heroPosition}_{action.ActionType}";
                                    }
                                }
                            }

                            preflActions = Enum.Parse<PreflopActions>($"{preflActions}{action.ActionType}");
                        }
                    }
                    else if (preflopPotType == PreflopPotType.ThreeBetPot)
                    {
                        if (preflActions == PreflopActions.NoActions)
                        {
                            preflopRows.Add($"{playerPosition}_3bet_CC_Row");

                            cellName = $"{playerPosition}_3bet_CC_{action.ActionType}";

                            preflActions = Enum.Parse<PreflopActions>($"{action.ActionType}");
                        }
                        else if (preflActions == PreflopActions.Raise)
                        {
                            if (gameType == GameType.Hu)
                            {
                                preflopRows.Add("SbvsBb_3bet_Row");

                                if (action.ActionType == BettingActionType.Raise)
                                    preflopRows.Add("SbvsBb_4bet_Range_Row");

                                cellName = $"SbvsBb_3bet_{action.ActionType}";
                            }
                            else
                            {
                                preflopRows.Add($"{playerPosition}_3bet_Raiser_Row");

                                if (action.ActionType == BettingActionType.Raise)
                                    preflopRows.Add($"{playerPosition}_4bet_Range_Row");

                                if (otherPlayersActed == OtherPlayersActed.No)
                                {
                                    preflopRows.Add($"{playerPosition}_3bet_Raiser_Vs_{heroPosition}_Row");

                                    if (action.ActionType == BettingActionType.Raise)
                                        preflopRows.Add($"{playerPosition}_4bet_Range_Vs_{heroPosition}_Row");

                                    cellName = $"{playerPosition}_3bet_Raiser_Vs_{heroPosition}_{action.ActionType}";
                                }
                                else
                                {
                                    preflopRows.Add($"{playerPosition}_3bet_Raiser_Vs_Multi_Row");

                                    if (action.ActionType == BettingActionType.Raise)
                                        preflopRows.Add($"{playerPosition}_4bet_Range_Vs_Multi_Row");

                                    cellName = $"{playerPosition}_3bet_Raiser_Vs_Multi_{action.ActionType}";
                                }
                            }

                            preflActions = Enum.Parse<PreflopActions>($"{preflActions}{action.ActionType}");
                        }
                    }
                    else if (preflopPotType == PreflopPotType.SqueezePot)
                    {
                        if (preflActions == PreflopActions.NoActions)
                        {
                            preflopRows.Add($"{playerPosition}_Squeeze_CC_Row");

                            cellName = $"{playerPosition}_Squeeze_CC_{action.ActionType}";

                            preflActions = Enum.Parse<PreflopActions>($"{action.ActionType}");
                        }
                        else if (preflActions == PreflopActions.Call)
                        {
                            preflopRows.Add($"{playerPosition}_Squeeze_Caller_Row");

                            cellName = $"{playerPosition}_Squeeze_Caller_{action.ActionType}";

                            preflActions = Enum.Parse<PreflopActions>($"{preflActions}{action.ActionType}");
                        }
                        else if (preflActions == PreflopActions.Raise)
                        {
                            preflopRows.Add($"{playerPosition}_Squeeze_Raiser_Row");

                            cellName = $"{playerPosition}_Squeeze_Raiser_{action.ActionType}";

                            if (action.ActionType == BettingActionType.Raise)
                                preflopRows.Add($"{playerPosition}_4bet_Range_SqzPot_Row");

                            preflActions = Enum.Parse<PreflopActions>($"{preflActions}{action.ActionType}");
                        }
                    }
                    else if (preflopPotType == PreflopPotType.RaiseIsolatePot)
                    {
                        if (preflActions == PreflopActions.NoActions)
                        {
                            preflopRows.Add($"{playerPosition}_RaiseIsolate_CC_Row");

                            cellName = $"{playerPosition}_RaiseIsolate_CC_{action.ActionType}";

                            preflActions = Enum.Parse<PreflopActions>($"{action.ActionType}");
                        }
                        else if (preflActions == PreflopActions.Call)
                        {
                            preflopRows.Add($"{playerPosition}_RaiseIsolate_Caller_Row");

                            cellName = $"{playerPosition}_RaiseIsolate_Caller_{action.ActionType}";

                            preflActions = Enum.Parse<PreflopActions>($"{preflActions}{action.ActionType}");
                        }
                        else if (preflActions == PreflopActions.Raise)
                        {
                            if (gameType == GameType.Hu)
                            {
                                preflopRows.Add("BbvsSb_RaiseIsolate_Row");

                                if (action.ActionType == BettingActionType.Raise)
                                    preflopRows.Add("BbvsSb_4bet_Range_Row");

                                cellName = $"BbvsSb_RaiseIsolate_{action.ActionType}";
                            }
                            else
                            {
                                preflopRows.Add($"{playerPosition}_RaiseIsolate_Raiser_Row");

                                cellName = $"{playerPosition}_RaiseIsolate_Raiser_{action.ActionType}";

                                if (otherPlayersActed == OtherPlayersActed.No)
                                {
                                    if (playerPosition == Position.Bb && heroPosition == Position.Sb)
                                    {
                                        preflopRows.Add($"{playerPosition}_RaiseIsolate_Vs_{heroPosition}_Row");

                                        if (action.ActionType == BettingActionType.Raise)
                                            preflopRows.Add($"{playerPosition}_4bet_Range_Vs_{heroPosition}_Row");

                                        cellName = $"{playerPosition}_RaiseIsolate_Vs_{heroPosition}_{action.ActionType}";
                                    }
                                }
                            }

                            preflActions = Enum.Parse<PreflopActions>($"{preflActions}{action.ActionType}");
                        }
                        else if (preflActions == PreflopActions.CallCall)
                        {
                            preflopRows.Add($"{playerPosition}_RaiseIsolate_Caller_Row");

                            cellName = $"{playerPosition}_RaiseIsolate_Caller_{action.ActionType}";

                            preflActions = Enum.Parse<PreflopActions>($"Any{action.ActionType}");
                        }
                    }
                    else if (preflopPotType == PreflopPotType.FourBetPot)
                    {
                        if (preflActions == PreflopActions.CallRaise)
                        {
                            if (gameType == GameType.Hu)
                            {
                                preflopRows.Add("SbvsBb_4bet_Row");

                                cellName = $"SbvsBb_4bet_{action.ActionType}";
                            }
                            else
                            {
                                preflopRows.Add($"{playerPosition}_4bet_Row");

                                cellName = $"{playerPosition}_4bet_{action.ActionType}";

                                if (otherPlayersActed == OtherPlayersActed.No)
                                {
                                    if (playerPosition == Position.Sb && heroPosition == Position.Bb)
                                    {
                                        preflopRows.Add($"{playerPosition}_4bet_Vs_{heroPosition}_Row");

                                        cellName = $"{playerPosition}_4bet_Vs_{heroPosition}_{action.ActionType}";
                                    }
                                }
                            }
                        }
                        else if (preflActions == PreflopActions.RaiseRaise)
                        {
                            if (gameType == GameType.Hu)
                            {
                                preflopRows.Add("BbvsSb_4bet_Row");

                                cellName = $"BbvsSb_4bet_{action.ActionType}";
                            }
                            else
                            {
                                preflopRows.Add($"{playerPosition}_4bet_Row");

                                cellName = $"{playerPosition}_4bet_{action.ActionType}";

                                if (otherPlayersActed == OtherPlayersActed.No)
                                {
                                    if (playerPosition == Position.Bb && heroPosition == Position.Sb)
                                    {
                                        preflopRows.Add($"{playerPosition}_4bet_Vs_{heroPosition}_Row");

                                        cellName = $"{playerPosition}_4bet_Vs_{heroPosition}_{action.ActionType}";
                                    }
                                }
                            }
                        }
                        else
                        {
                            preflopRows.Add($"{playerPosition}_4bet_Row");

                            cellName = $"{playerPosition}_4bet_{action.ActionType}";
                        }

                        preflActions = Enum.Parse<PreflopActions>($"Any{action.ActionType}");
                    }
                    else if (preflopPotType == PreflopPotType.FiveBetPot)
                    {
                        if (gameType == GameType.Hu)
                        {
                            preflopRows.Add($"{playerPosition}vs{heroPosition}_5bet_Row");

                            cellName = $"{playerPosition}vs{heroPosition}_5bet_{action.ActionType}";
                        }
                        else
                            cellName = string.Empty;
                    }
                    else if (preflopPotType == PreflopPotType.Other)
                        cellName = string.Empty;
                }

                //Update pot type
                if (action.ActionType == BettingActionType.Call)
                {
                    if (raiseCount == 0)
                        preflopPotType = PreflopPotType.LimpPot;

                    callCount++;
                }
                else if (action.ActionType == BettingActionType.Raise)
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
                        preflopPotType = PreflopPotType.Other;

                    raiseCount++;
                }

                if (action.Player != gc.HeroPosition && action.Player != player && action.ActionType != BettingActionType.Fold)
                    otherPlayersActed = OtherPlayersActed.Yes;
            }

            //Update at hero turn
            if (gc.Round == 1)
            {
                if (preflopPotType == PreflopPotType.Unopened)
                {
                    if (preflActions == PreflopActions.NoActions)
                    {
                        if (gameType == GameType.Hu)
                            preflopRows.UnionWith(new string[] { "BbvsSb_Limp_Row", "BbvsSb_Raise_Row" });
                        else
                            preflopRows.UnionWith(new string[] { $"{playerPosition}_Limp_Vs_{heroPosition}_Row", $"{playerPosition}_Raise_Vs_{heroPosition}_Row" });
                    }
                }
                else if (preflopPotType == PreflopPotType.LimpPot)
                {
                    if (preflActions == PreflopActions.NoActions)
                        preflopRows.UnionWith(new string[] { $"{playerPosition}_Limp_Vs_Multi_Row", $"{playerPosition}_Isolate_CC_Row" });
                    else if (preflActions == PreflopActions.Call)
                    {
                        if (gameType == GameType.Hu)
                            preflopRows.Add("SbvsBb_Isolate_Row");
                        else
                        {
                            preflopRows.Add($"{playerPosition}_Isolate_Limper_Row");

                            if (otherPlayersActed == OtherPlayersActed.No)
                            {
                                if (playerPosition == Position.Sb && heroPosition == Position.Bb)
                                    preflopRows.Add($"{playerPosition}_Isolate_Limper_Vs_{heroPosition}_Row");
                            }
                        }
                    }
                }
                else if (preflopPotType == PreflopPotType.RaisePot)
                {
                    if (preflActions == PreflopActions.NoActions)
                    {
                        preflopRows.Add($"{playerPosition}_Raise_Vs_Multi_Row");
                        preflopRows.Add(callCount == 0 ? $"{playerPosition}_3bet_CC_Row" : $"{playerPosition}_Squeeze_CC_Row");
                    }
                    else if (preflActions == PreflopActions.Call)
                        preflopRows.Add($"{playerPosition}_Squeeze_Caller_Row");
                    else if (preflActions == PreflopActions.Raise)
                    {
                        if (gameType == GameType.Hu)
                            preflopRows.Add("SbvsBb_3bet_Row");
                        else
                        {
                            if (callCount == 0)
                            {
                                preflopRows.Add($"{playerPosition}_3bet_Raiser_Row");
                                preflopRows.Add($"{playerPosition}_3bet_Raiser_Vs_{heroPosition}_Row");
                            }
                            else
                                preflopRows.Add($"{playerPosition}_Squeeze_Raiser_Row");
                        }
                    }
                }
                else if (preflopPotType == PreflopPotType.IsolatePot)
                {
                    if (preflActions == PreflopActions.NoActions)
                        preflopRows.UnionWith(new string[] { $"{playerPosition}_Isolate_CC_Row", $"{playerPosition}_RaiseIsolate_CC_Row" });
                    else if (preflActions == PreflopActions.Call)
                    {
                        preflopRows.Add($"{playerPosition}_RaiseIsolate_Caller_Row");

                        if (!preflopRows.Contains($"{playerPosition}_Isolate_CC_Row"))
                            preflopRows.Add($"{playerPosition}_Isolate_Limper_Row");
                    }
                    else if(preflActions == PreflopActions.Raise)
                        preflopRows.Add($"{playerPosition}_RaiseIsolate_Raiser_Row");
                }
                else if (preflopPotType == PreflopPotType.ThreeBetPot)
                {
                    if (preflActions == PreflopActions.NoActions)
                        preflopRows.UnionWith(new string[] { $"{playerPosition}_3bet_CC_Row", $"{playerPosition}_4bet_Row" });
                    else if (preflActions == PreflopActions.Call)
                        preflopRows.Add($"{playerPosition}_4bet_Row");
                    else if (preflActions == PreflopActions.Raise)
                    {
                        if (gameType == GameType.Hu)
                            preflopRows.Add($"BbvsSb_4bet_Row");
                        else
                        {
                            preflopRows.Add($"{playerPosition}_4bet_Row");

                            if (otherPlayersActed == OtherPlayersActed.No)
                            {
                                if (playerPosition == Position.Bb && heroPosition == Position.Sb)
                                    preflopRows.Add($"{playerPosition}_4bet_Vs_{heroPosition}_Row");
                            }
                            else
                            {
                                if (!preflopRows.Any(pr => pr.Contains($"{playerPosition}_Raise_Vs")))
                                {
                                    preflopRows.Add($"{playerPosition}_3bet_Raiser_Row");
                                    preflopRows.Add($"{playerPosition}_3bet_Raiser_Vs_Multi_Row");
                                }
                            }
                        }
                    }
                }
                else if (preflopPotType == PreflopPotType.SqueezePot)
                {
                    if (preflActions == PreflopActions.NoActions)
                        preflopRows.UnionWith(new string[] { $"{playerPosition}_Squeeze_CC_Row", $"{playerPosition}_4bet_Row" });
                    else if (preflActions == PreflopActions.Call)
                    {
                        preflopRows.Add($"{playerPosition}_4bet_Row");

                        if (!preflopRows.Contains($"{playerPosition}_Squeeze_CC_Row"))
                            preflopRows.Add($"{playerPosition}_Squeeze_Caller_Row");
                    }
                    else if (preflActions == PreflopActions.Raise)
                    {
                        preflopRows.Add($"{playerPosition}_4bet_Row");

                        if (!preflopRows.Any(pr => pr.Contains($"{playerPosition}_Raise_Vs_Multi")))
                            preflopRows.Add($"{playerPosition}_Squeeze_Raiser_Row");
                    }
                    else
                        preflopRows.Add($"{playerPosition}_4bet_Row");
                }
                else if (preflopPotType == PreflopPotType.RaiseIsolatePot)
                {
                    if (preflActions == PreflopActions.NoActions)
                        preflopRows.UnionWith(new string[] { $"{playerPosition}_RaiseIsolate_CC_Row", $"{playerPosition}_4bet_Row" });
                    else if (preflActions == PreflopActions.Call)
                    {
                        preflopRows.Add($"{playerPosition}_4bet_Row");

                        if (!preflopRows.Contains($"{playerPosition}_RaiseIsolate_CC_Row"))
                            preflopRows.Add($"{playerPosition}_RaiseIsolate_Caller_Row");
                    }
                    else if (preflActions == PreflopActions.Raise)
                    {
                        preflopRows.Add($"{playerPosition}_4bet_Row");

                        if (!preflopRows.Contains($"{playerPosition}_Isolate_CC_Row"))
                            preflopRows.Add($"{playerPosition}_RaiseIsolate_Raiser_Row");
                    }
                    else if (preflActions == PreflopActions.CallRaise)
                    {
                        if (gameType == GameType.Hu)
                            preflopRows.Add($"SbvsBb_4bet_Row");
                        else
                        {
                            preflopRows.Add($"{playerPosition}_4bet_Row");

                            if (otherPlayersActed == OtherPlayersActed.No)
                            {
                                if(playerPosition == Position.Sb && heroPosition == Position.Bb)
                                    preflopRows.Add($"{playerPosition}_4bet_Vs_{heroPosition}_Row");
                            }
                        }
                    }
                    else
                        preflopRows.Add($"{playerPosition}_4bet_Row");
                }
                else if (preflopPotType == PreflopPotType.FourBetPot)
                {
                    if(gameType == GameType.Hu)
                        preflopRows.Add($"{playerPosition}vs{heroPosition}_5bet_Row");
                }
            }

            preflopHud.SetRows(preflopRows.ToArray());

            #endregion

        Postflop:

            #region Postflop

            if(postflopHud == null)
                goto End;

            BettingAction[] postflopActions = gc.Actions.Where(a => a.Round > 1).ToArray();

            HashSet<string> postflopRows = new HashSet<string>();

            string[] actionSequences = new string[3] {string.Empty, string.Empty, string.Empty};

            foreach (BettingAction action in postflopActions)
            {
                if (action.Player == player)
                {
                    //Multiway
                    if (postflopHud.SetType == SetType.PostflopGeneral)
                    {
                        string roundAbbr = string.Empty;

                        if (action.Round == 2)
                            roundAbbr = "_F";
                        else if (action.Round == 3)
                            roundAbbr = "_T";
                        else if (action.Round == 4)
                            roundAbbr = "_R";

                        int betsRaisesCount = actionSequences[action.Round - 2].Count(a => a is 'b' or 'r');

                        //Unopened pot
                        if (betsRaisesCount == 0)
                            cellName = $"{(action.ActionType == BettingActionType.Check ? "Check" : "Bet")}{roundAbbr}";
                        //Bet pot
                        else if (betsRaisesCount == 1)
                            cellName = $"{(action.ActionType == BettingActionType.Call ? "CvBet" : "Raise")}{roundAbbr}";
                        //Raise pot
                        else if (betsRaisesCount == 2)
                        {
                            if(action.ActionType == BettingActionType.Call)
                                cellName = $"CvRaise{roundAbbr}";
                            else
                            {
                                postflopRows.Add("ThreeBet_Row");

                                cellName = $"ThreeBet{roundAbbr}";
                            }
                        }
                        //3 bet pot
                        else if (betsRaisesCount == 3)
                        {
                            postflopRows.Add("FvThreeBet_Row");

                            if (action.ActionType == BettingActionType.Call)
                                cellName = $"CvThreeBet{roundAbbr}";
                            else
                            {
                                postflopRows.Add("FourBet_Row");

                                cellName = $"FourBet{roundAbbr}";
                            }
                        }
                        //4 bet+ pot
                        else
                            cellName = string.Empty;
                    }
                    //Hu
                    else
                    {
                        //Flop
                        if (action.Round == 2)
                        {
                            if (actionSequences[0] is "brr" or "xbrr")
                            {
                                postflopRows.UnionWith(new string[] { "FvTHREEBET_Row" });

                                if (action.ActionType == BettingActionType.Raise)
                                {
                                    postflopRows.UnionWith(new string[] { "FOURBET_Row" });

                                    cellName = "FOURBET_F";
                                }
                                else
                                    cellName = "CvTHREEBET_F";
                            }
                            else
                            {
                                //IP preflop raiser
                                if (postflopHud.SetType == SetType.PostflopHuIpRaiser)
                                {
                                    if (actionSequences[0] == "x")
                                        cellName = action.ActionType == BettingActionType.Bet ? "CB_F" : "CX_F";
                                    else if (actionSequences[0] == "xbr")
                                    {
                                        if (action.ActionType == BettingActionType.Raise)
                                        {
                                            postflopRows.UnionWith(new string[] { "THREEBET_Row" });

                                            cellName = "THREEBET_F";
                                        }
                                        else
                                            cellName = "CCBvR_F";
                                    }
                                    else if (actionSequences[0] == "b")
                                        cellName = action.ActionType == BettingActionType.Raise ? "RvDONK_F" : "CvDONK_F";
                                    else
                                        cellName = string.Empty;
                                }
                                //IP preflop caller
                                else if (postflopHud.SetType == SetType.PostflopHuIpCaller)
                                {
                                    if (actionSequences[0] == "x")
                                        cellName = action.ActionType == BettingActionType.Bet ? "FLOAT_F" : "FLOATX_F";
                                    else if (actionSequences[0] == "xbr")
                                    {
                                        postflopRows.UnionWith(new string[] { "FLOAT_F_Row" });

                                        if (action.ActionType == BettingActionType.Raise)
                                        {
                                            postflopRows.UnionWith(new string[] { "THREEBET_Row" });

                                            cellName = "THREEBET_F";
                                        }
                                        else
                                            cellName = "FLOAT_C_F";
                                    }
                                    else if (actionSequences[0] == "b")
                                        cellName = action.ActionType == BettingActionType.Raise ? "RvCB_F" : "CvCB_F";
                                    else
                                        cellName = string.Empty;
                                }
                                //OOP preflop raiser
                                else if (postflopHud.SetType == SetType.PostflopHuOopRaiser)
                                {
                                    if (actionSequences[0] == string.Empty)
                                        cellName = action.ActionType == BettingActionType.Bet ? "CB_F" : "CX_F";
                                    else if (actionSequences[0] == "xb")
                                    {
                                        if (action.ActionType == BettingActionType.Raise)
                                        {
                                            postflopRows.UnionWith(new string[] { "RvFLOAT_Row" });

                                            cellName = "RvFLOAT_F";
                                        }
                                        else
                                            cellName = "CvFLOAT_F";
                                    }
                                    else if (actionSequences[0] == "br")
                                    {
                                        if (action.ActionType == BettingActionType.Raise)
                                        {
                                            postflopRows.UnionWith(new string[] { "THREEBET_Row" });

                                            cellName = "THREEBET_F";
                                        }
                                        else
                                            cellName = "CCBvR_F";
                                    }
                                    else
                                        cellName = string.Empty;
                                }
                                //OOP preflop caller
                                else if (postflopHud.SetType == SetType.PostflopHuOopCaller)
                                {
                                    if (actionSequences[0] == string.Empty)
                                        cellName = action.ActionType == BettingActionType.Bet ? "DONK_F" : "DONKX_F";
                                    else if (actionSequences[0] == "xb")
                                        cellName = action.ActionType == BettingActionType.Raise ? "RvCB_F" : "CvCB_F";
                                    else if (actionSequences[0] == "br")
                                    {
                                        if (action.ActionType == BettingActionType.Raise)
                                        {
                                            postflopRows.UnionWith(new string[] { "THREEBET_Row" });

                                            cellName = "THREEBET_F";
                                        }
                                        else
                                            cellName = "DONK_C_F";
                                    }
                                    else
                                        cellName = string.Empty;
                                }
                            }
                        }
                        //Turn
                        else if (action.Round == 3)
                        {
                            if (actionSequences[1] is "brr" or "xbrr")
                            {
                                postflopRows.UnionWith(new string[] { "FvTHREEBET_Row" });

                                if (action.ActionType == BettingActionType.Raise)
                                {
                                    postflopRows.UnionWith(new string[] { "FOURBET_Row" });

                                    cellName = "FOURBET_T";
                                }
                                else
                                    cellName = "CvTHREEBET_T";
                            }
                            else
                            {
                                //IP preflop raiser
                                if (postflopHud.SetType == SetType.PostflopHuIpRaiser)
                                {
                                    if (actionSequences[1] == "x")
                                    {
                                        if (actionSequences[0] == "xx")
                                            cellName = action.ActionType == BettingActionType.Bet ? "DELAY_T" : "DELAYX_T";
                                        else if (actionSequences[0] == "xbc")
                                            cellName = action.ActionType == BettingActionType.Bet ? "CB_T" : "CX_T";
                                        else if (actionSequences[0] == "xbrc")
                                        {
                                            postflopRows.UnionWith(new string[] { "FLOATXRCB_Row" });

                                            cellName = action.ActionType == BettingActionType.Bet ? "FLOATXRCB_T" : "FLOATXRCBX_T";
                                        }
                                        else if (actionSequences[0] == "bc")
                                        {
                                            postflopRows.UnionWith(new string[] { "FLOATDONK_Row" });

                                            cellName = action.ActionType == BettingActionType.Bet ? "FLOATDONK_T" : "FLOATDONKX_T";
                                        }
                                        else if (actionSequences[0] == "brc")
                                        {
                                            postflopRows.UnionWith(new string[] { "RvDONK_BB_Row" });

                                            cellName = action.ActionType == BettingActionType.Bet ? "RvDONK_F_B" : "RvDONK_F_X";
                                        }
                                        else
                                            cellName = string.Empty;
                                    }
                                    else if (actionSequences[1] == "xbr")
                                    {
                                        if (action.ActionType == BettingActionType.Raise)
                                        {
                                            postflopRows.UnionWith(new string[] { "THREEBET_Row" });

                                            cellName = "THREEBET_T";

                                            if (actionSequences[0] == "xx")
                                                postflopRows.UnionWith(new string[] { "DELAY_F_Row" });
                                        }
                                        else
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "DELAY_F_Row" });

                                                cellName = "DELAY_C_T";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                                cellName = "CCBvR_T";
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                    }
                                    else if (actionSequences[1] == "b")
                                    {
                                        if (actionSequences[0] == "xx")
                                        {
                                            if (action.ActionType == BettingActionType.Raise)
                                            {
                                                postflopRows.UnionWith(new string[] { "RvPROBE_Row" });

                                                cellName = "RvPROBE_T";
                                            }
                                            else 
                                                cellName = "CvPROBE_T";
                                        }
                                        else if (actionSequences[0] == "xbc")
                                            cellName = action.ActionType == BettingActionType.Raise ? "RvDONK_T" : "CvDONK_T";
                                        else if (actionSequences[0] == "xbrc")
                                        {
                                            postflopRows.UnionWith(new string[] { "FCBvR_BB_Row" });

                                            cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CCBvR_F_B";
                                        }
                                        else if (actionSequences[0] == "bc")
                                        {
                                            postflopRows.UnionWith(new string[] { "FvDONK_BB_Row" });

                                            cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvDONK_F_B";
                                        }
                                        else if (actionSequences[0] == "brc")
                                            cellName = string.Empty;
                                        else
                                            cellName = string.Empty;
                                    }
                                    else
                                        cellName = string.Empty;
                                }
                                //IP preflop caller
                                else if (postflopHud.SetType == SetType.PostflopHuIpCaller)
                                {
                                    if (actionSequences[1] == "x")
                                    {
                                        if (actionSequences[0] == "xx")
                                        {
                                            postflopRows.UnionWith(new string[] { "DelFLOAT_Row" });

                                            cellName = action.ActionType == BettingActionType.Bet ? "DelFLOAT_T" : "DelFLOATX_T";
                                        }
                                        else if (actionSequences[0] == "xbc")
                                        {
                                            postflopRows.UnionWith(new string[] { "FLOAT_BB_Row" });

                                            cellName = action.ActionType == BettingActionType.Bet ? "FLOAT_F_B" : "FLOAT_F_X";
                                        }
                                        else if (actionSequences[0] == "xbrc")
                                            cellName = string.Empty;
                                        else if (actionSequences[0] == "bc")
                                            cellName = action.ActionType == BettingActionType.Bet ? "FLOAT_T" : "FLOATX_T";
                                        else if (actionSequences[0] == "brc")
                                        {
                                            postflopRows.UnionWith(new string[] { "RvCB_BB_Row" });

                                            cellName = action.ActionType == BettingActionType.Bet ? "RvCB_F_B" : "RvCB_F_X";
                                        }
                                        else
                                            cellName = string.Empty;
                                    }
                                    else if (actionSequences[1] == "xbr")
                                    {
                                        if (action.ActionType == BettingActionType.Raise)
                                        {
                                            postflopRows.UnionWith(new string[] { "THREEBET_Row" });

                                            cellName = "THREEBET_T";

                                            if (actionSequences[0] == "xx")
                                                postflopRows.UnionWith(new string[] { "DelFLOAT_F_Row" });
                                            else if (actionSequences[0] == "bc")
                                                postflopRows.UnionWith(new string[] { "FLOAT_F_Row" });
                                        }
                                        else
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "DelFLOAT_F_Row" });

                                                cellName = "DelFLOAT_C_T";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FLOAT_F_Row" });

                                                cellName = "FLOAT_C_T";
                                            }
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                    }
                                    else if (actionSequences[1] == "b")
                                    {
                                        if (actionSequences[0] == "xx")
                                        {
                                            if (action.ActionType == BettingActionType.Raise)
                                            {
                                                postflopRows.UnionWith(new string[] { "RvDELAY_Row" });

                                                cellName = "RvDELAY_T";
                                            }
                                            else 
                                                cellName = "CvDELAY_T";
                                        }
                                        else if (actionSequences[0] == "xbc")
                                            cellName = string.Empty;
                                        else if (actionSequences[0] == "xbrc")
                                        {
                                            postflopRows.UnionWith(new string[] { "FLOAT_F_BB_Row" });

                                            cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "FLOAT_C_F_B";
                                        }
                                        else if (actionSequences[0] == "bc")
                                            cellName = action.ActionType == BettingActionType.Raise ? "RvCB_T" : "CvCB_T";
                                        else if (actionSequences[0] == "brc")
                                            cellName = string.Empty;
                                        else
                                            cellName = string.Empty;
                                    }
                                    else
                                        cellName = string.Empty;
                                }
                                //OOP preflop raiser
                                else if (postflopHud.SetType == SetType.PostflopHuOopRaiser)
                                {
                                    if (actionSequences[1] == string.Empty)
                                    {
                                        if (actionSequences[0] == "xx")
                                            cellName = action.ActionType == BettingActionType.Bet ? "DELAY_T" : "DELAYX_T";
                                        else if (actionSequences[0] == "xbc")
                                            cellName = string.Empty;
                                        else if (actionSequences[0] == "xbrc")
                                        {
                                            postflopRows.UnionWith(new string[] { "RvFLOAT_BB_Row" });

                                            cellName = action.ActionType == BettingActionType.Bet ? "RvFLOAT_F_B" : "RvFLOAT_F_X";
                                        }
                                        else if (actionSequences[0] == "bc")
                                            cellName = action.ActionType == BettingActionType.Bet ? "CB_T" : "CX_T";
                                        else if (actionSequences[0] == "brc")
                                            cellName = string.Empty;
                                        else
                                            cellName = string.Empty;
                                    }
                                    else if (actionSequences[1] == "xb")
                                    {
                                        if (actionSequences[0] == "xx")
                                        {
                                            postflopRows.UnionWith(new string[] { "FvDelFLOAT_Row" });

                                            if (action.ActionType == BettingActionType.Raise)
                                            {
                                                postflopRows.UnionWith(new string[] { "RvDelFLOAT_Row" });

                                                cellName = "RvDelFLOAT_T";
                                            }
                                            else
                                                cellName = "CvDelFLOAT_T";
                                        }
                                        else if (actionSequences[0] == "xbc")
                                        {
                                            postflopRows.UnionWith(new string[] { "FvFLOAT_BB_Row" });

                                            cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvFLOAT_F_B";
                                        }
                                        else if (actionSequences[0] == "xbrc")
                                            cellName = string.Empty;
                                        else if (actionSequences[0] == "bc")
                                        {
                                            if (action.ActionType == BettingActionType.Raise)
                                            {
                                                postflopRows.UnionWith(new string[] { "RvFLOAT_Row" });

                                                cellName = "RvFLOAT_T";
                                            }
                                            else
                                                cellName = "CvFLOAT_T";
                                        }
                                        else if (actionSequences[0] == "brc")
                                        {
                                            postflopRows.UnionWith(new string[] { "FCBvR_BB_Row" });

                                            cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CCBvR_F_B";
                                        }
                                        else
                                            cellName = string.Empty;
                                    }
                                    else if (actionSequences[1] == "br")
                                    {
                                        if (action.ActionType == BettingActionType.Raise)
                                        {
                                            postflopRows.UnionWith(new string[] { "THREEBET_Row" });

                                            cellName = "THREEBET_T";

                                            if (actionSequences[0] == "xx")
                                                postflopRows.UnionWith(new string[] { "DELAY_F_Row" });
                                        }
                                        else
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "DELAY_F_Row" });

                                                cellName = "DELAY_C_T";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                                cellName = "CCBvR_T";
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                    }
                                    else
                                        cellName = string.Empty;
                                }
                                //OOP preflop caller
                                else if (postflopHud.SetType == SetType.PostflopHuOopCaller)
                                {
                                    if (actionSequences[1] == string.Empty)
                                    {
                                        if (actionSequences[0] == "xx")
                                            cellName = action.ActionType == BettingActionType.Bet ? "PROBE_T" : "PROBEX_T";
                                        else if (actionSequences[0] == "xbc")
                                            cellName = action.ActionType == BettingActionType.Bet ? "DONK_T" : "DONKX_T";
                                        else if (actionSequences[0] == "xbrc")
                                        {
                                            postflopRows.UnionWith(new string[] { "RvCB_BB_Row" });

                                            cellName = action.ActionType == BettingActionType.Bet ? "RvCB_F_B" : "RvCB_F_X";
                                        }
                                        else if (actionSequences[0] == "bc")
                                        {
                                            postflopRows.UnionWith(new string[] { "DONK_BB_Row" });

                                            cellName = action.ActionType == BettingActionType.Bet ? "DONK_F_B" : "DONK_F_X";
                                        }
                                        else if (actionSequences[0] == "brc")
                                            cellName = string.Empty;
                                        else
                                            cellName = string.Empty;
                                    }
                                    else if (actionSequences[1] == "xb")
                                    {
                                        if (actionSequences[0] == "xx")
                                        {
                                            if (action.ActionType == BettingActionType.Raise)
                                            {
                                                postflopRows.UnionWith(new string[] { "RvDELAY_Row" });

                                                cellName = "RvDELAY_T";
                                            }
                                            else
                                                cellName = "CvDELAY_T";
                                        }
                                        else if (actionSequences[0] == "xbc")
                                            cellName = action.ActionType == BettingActionType.Raise ? "RvCB_T" : "CvCB_T";
                                        else if (actionSequences[0] == "xbrc")
                                        {
                                            postflopRows.UnionWith(new string[] { "FvFLOATXRCB_Row" });

                                            cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvFLOATXRCB_T";
                                        }
                                        else if (actionSequences[0] == "bc")
                                        {
                                            postflopRows.UnionWith(new string[] { "FvFLOATDONK_Row" });

                                            cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvFLOATDONK_T";
                                        }
                                        else if (actionSequences[0] == "brc")
                                        {
                                            postflopRows.UnionWith(new string[] { "DONK_F_BB_Row" });

                                            cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "DONK_C_F_B";
                                        }
                                        else
                                            cellName = string.Empty;
                                    }
                                    else if (actionSequences[1] == "br")
                                    {
                                        if (action.ActionType == BettingActionType.Raise)
                                        {
                                            postflopRows.UnionWith(new string[] { "THREEBET_Row" });

                                            cellName = "THREEBET_T";

                                            if (actionSequences[0] == "xx")
                                                postflopRows.UnionWith(new string[] { "PROBE_F_Row" });
                                        }
                                        else
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "PROBE_F_Row" });

                                                cellName = "PROBE_C_T";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                                cellName = "DONK_C_T";
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                    }
                                    else
                                        cellName = string.Empty;
                                }
                            }
                        }
                        //River
                        else if (action.Round == 4)
                        {
                            if (actionSequences[2] is "brr" or "xbrr")
                            {
                                postflopRows.UnionWith(new string[] { "FvTHREEBET_Row" });

                                if (action.ActionType == BettingActionType.Raise)
                                {
                                    postflopRows.UnionWith(new string[] { "FOURBET_Row" });

                                    cellName = "FOURBET_R";
                                }
                                else
                                    cellName = "CvTHREEBET_R";
                            }
                            else
                            {
                                //IP preflop raiser
                                if (postflopHud.SetType == SetType.PostflopHuIpRaiser)
                                {
                                    if (actionSequences[2] == "x")
                                    {
                                        if (actionSequences[1] == "xx")
                                        {
                                            if (actionSequences[0] == "xx")
                                                cellName = action.ActionType == BettingActionType.Bet ? "SDELAY_R" : "SDELAYX_R";
                                            else if (actionSequences[0] == "xbc")
                                                cellName = action.ActionType == BettingActionType.Bet ? "DELAY_R" : "DELAYX_R";
                                            else if (actionSequences[0] == "xbrc")
                                            {
                                                postflopRows.UnionWith(new string[] { "DelFLOATXRCB_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "DelFLOATXRCB_R" : "DelFLOATXRCBX_R";
                                            }
                                            else if (actionSequences[0] == "bc")
                                            {
                                                postflopRows.UnionWith(new string[] { "DelFLOATDONK_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "DelFLOATDONK_R" : "DelFLOATDONKX_R";
                                            }
                                            else if (actionSequences[0] == "brc")
                                            {
                                                postflopRows.UnionWith(new string[] { "RvDONK_XB_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "RvDONK_F_XB" : "RvDONK_F_XX";
                                            }
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "xbc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "DELAY_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "DELAY_T_B" : "DELAY_T_X";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                                cellName = action.ActionType == BettingActionType.Bet ? "CB_R" : "CX_R";
                                            else if (actionSequences[0] == "xbrc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FLOATXRCB_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "FLOATXRCB_T_B" : "FLOATXRCB_T_X";
                                            }
                                            else if (actionSequences[0] == "bc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FLOATDONK_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "FLOATDONK_T_B" : "FLOATDONK_T_X";
                                            }
                                            else if (actionSequences[0] == "brc")
                                            {
                                                postflopRows.UnionWith(new string[] { "RvDONK_BB_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "RvDONK_F_BB" : "RvDONK_F_BX";
                                            }
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "xbrc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "FLOATXRDELAY_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "FLOATXRDELAY_R" : "FLOATXRDELAYX_R";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FLOATXRCB_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "FLOATXRCB_R" : "FLOATXRCBX_R";
                                            }
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "bc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "FLOATPROBE_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "FLOATPROBE_R" : "FLOATPROBEX_R";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FLOATDONK_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "FLOATDONK_R" : "FLOATDONKX_R";
                                            }
                                            else if (actionSequences[0] == "xbrc")
                                            {
                                                postflopRows.UnionWith(new string[] { "DblFLOATXRCB_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "DblFLOATXRCB_R" : "DblFLOATXRCBX_R";
                                            }
                                            else if (actionSequences[0] == "bc")
                                            {
                                                postflopRows.UnionWith(new string[] { "DblFLOATDONK_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "DblFLOATDONK_R" : "DblFLOATDONKX_R";
                                            }
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "brc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "RvPROBE_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "RvPROBE_T_B" : "RvPROBE_T_X";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                            {
                                                postflopRows.UnionWith(new string[] { "RvDONK_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "RvDONK_T_B" : "RvDONK_T_X";
                                            }
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else
                                            cellName = string.Empty;
                                    }
                                    else if (actionSequences[2] == "xbr")
                                    {
                                        if (action.ActionType == BettingActionType.Raise)
                                        {
                                            postflopRows.UnionWith(new string[] { "THREEBET_Row" });

                                            cellName = "THREEBET_R";

                                            if (actionSequences[1] == "xx")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    postflopRows.UnionWith(new string[] { "SDELAY_F_Row" });
                                                else if (actionSequences[0] == "xbc")
                                                    postflopRows.UnionWith(new string[] { "DELAY_F_Row" });
                                                else
                                                    cellName = string.Empty;
                                            }
                                            else
                                                cellName = string.Empty;
                                        }
                                        else
                                        {
                                            if (actionSequences[1] == "xx")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    postflopRows.UnionWith(new string[] { "SDELAY_F_Row" });

                                                    cellName = "SDELAY_C_R";
                                                }
                                                else if (actionSequences[0] == "xbc")
                                                {
                                                    postflopRows.UnionWith(new string[] { "DELAY_F_Row" });

                                                    cellName = "DELAY_C_R";
                                                }
                                                else if (actionSequences[0] == "xbrc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "bc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "brc")
                                                    cellName = string.Empty;
                                                else
                                                    cellName = string.Empty;
                                            }
                                            else if (actionSequences[1] == "xbc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbc")
                                                    cellName = "CCBvR_R";
                                                else if (actionSequences[0] == "xbrc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "bc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "brc")
                                                    cellName = string.Empty;
                                                else
                                                    cellName = string.Empty;
                                            }
                                            else if (actionSequences[1] == "xbrc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbrc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "bc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "brc")
                                                    cellName = string.Empty;
                                                else
                                                    cellName = string.Empty;
                                            }
                                            else if (actionSequences[1] == "bc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbrc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "bc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "brc")
                                                    cellName = string.Empty;
                                                else
                                                    cellName = string.Empty;
                                            }
                                            else if (actionSequences[1] == "brc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbrc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "bc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "brc")
                                                    cellName = string.Empty;
                                                else
                                                    cellName = string.Empty;
                                            }
                                            else
                                                cellName = string.Empty;
                                        }
                                    }
                                    else if (actionSequences[2] == "b")
                                    {
                                        if (actionSequences[1] == "xx")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                if (action.ActionType == BettingActionType.Raise)
                                                {
                                                    postflopRows.UnionWith(new string[] { "RvSPROBE_Row" });

                                                    cellName = "RvSPROBE_R";
                                                }
                                                else
                                                    cellName = "CvSPROBE_R";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                            {
                                                if (action.ActionType == BettingActionType.Raise)
                                                {
                                                    postflopRows.UnionWith(new string[] { "RvPROBE_Row" });

                                                    cellName = "RvPROBE_R";
                                                }
                                                else
                                                    cellName = "CvPROBE_R";
                                            }
                                            else if (actionSequences[0] == "xbrc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FCBvR_XB_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CCBvR_F_XB";
                                            }
                                            else if (actionSequences[0] == "bc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvDONK_XB_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvDONK_F_XB";
                                            }
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "xbc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvDONKDELAY_Row" });

                                                if (action.ActionType == BettingActionType.Raise)
                                                {
                                                    postflopRows.UnionWith(new string[] { "RvDONKDELAY_Row" });

                                                    cellName = "RvDONKDELAY_R";
                                                }
                                                else 
                                                    cellName = "CvDONKDELAY_R";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                                cellName = action.ActionType == BettingActionType.Raise ? "RvDONK_R" : "CvDONK_R";
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "xbrc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "DELAY_F_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "DELAY_C_T_B";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FCBvR_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CCBvR_T_B";
                                            }
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "bc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvPROBE_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvPROBE_T_B";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvDONK_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvDONK_T_B";
                                            }
                                            else if (actionSequences[0] == "xbrc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FCBvR_BB_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CCBvR_F_BB";
                                            }
                                            else if (actionSequences[0] == "bc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvDONK_BB_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvDONK_F_BB";
                                            }
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "brc")
                                        {
                                            if (actionSequences[0] == "xx")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else
                                            cellName = string.Empty;
                                    }
                                    else
                                        cellName = string.Empty;
                                }
                                //IP preflop caller
                                else if (postflopHud.SetType == SetType.PostflopHuIpCaller)
                                {
                                    if (actionSequences[2] == "x")
                                    {
                                        if (actionSequences[1] == "xx")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "SDelFLOAT_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "SDelFLOAT_R" : "SDelFLOATX_R";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FLOAT_XB_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "FLOAT_F_XB" : "FLOAT_F_XX";
                                            }
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                            {
                                                postflopRows.UnionWith(new string[] { "DelFLOAT_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "DelFLOAT_R" : "DelFLOATX_R";
                                            }
                                            else if (actionSequences[0] == "brc")
                                            {
                                                postflopRows.UnionWith(new string[] { "RvCB_XB_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "RvCB_F_XB" : "RvCB_F_XX";
                                            }
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "xbc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "DelFLOAT_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "DelFLOAT_T_B" : "DelFLOAT_T_X";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FLOAT_BB_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "FLOAT_F_BB" : "FLOAT_F_BX";
                                            }
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FLOAT_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "FLOAT_T_B" : "FLOAT_T_X";
                                            }
                                            else if (actionSequences[0] == "brc")
                                            {
                                                postflopRows.UnionWith(new string[] { "RvCB_BB_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "RvCB_F_BB" : "RvCB_F_BX";
                                            }
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "xbrc")
                                        {
                                            if (actionSequences[0] == "xx")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "bc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "FLOATDELAY_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "FLOATDELAY_R" : "FLOATDELAYX_R";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                                cellName = action.ActionType == BettingActionType.Bet ? "FLOAT_R" : "FLOATX_R";
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "brc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "RvDELAY_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "RvDELAY_T_B" : "RvDELAY_T_X";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                            {
                                                postflopRows.UnionWith(new string[] { "RvCB_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "RvCB_T_B" : "RvCB_T_X";
                                            }
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else
                                            cellName = string.Empty;
                                    }
                                    else if (actionSequences[2] == "xbr")
                                    {
                                        if (action.ActionType == BettingActionType.Raise)
                                        {
                                            postflopRows.UnionWith(new string[] { "THREEBET_Row" });

                                            cellName = "THREEBET_R";

                                            if (actionSequences[1] == "xx")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    postflopRows.UnionWith(new string[] { "SDelFLOAT_F_Row" });
                                                else if (actionSequences[0] == "bc")
                                                    postflopRows.UnionWith(new string[] { "DelFLOAT_F_Row" });
                                            }
                                            else if (actionSequences[1] == "bc")
                                            {
                                                if (actionSequences[0] == "bc")
                                                    postflopRows.UnionWith(new string[] { "FLOAT_F_Row" });
                                            }
                                        }
                                        else
                                        {
                                            if (actionSequences[1] == "xx")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    postflopRows.UnionWith(new string[] { "SDelFLOAT_F_Row" });

                                                    cellName = "SDelFLOAT_C_R";
                                                }
                                                else if (actionSequences[0] == "xbc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbrc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "bc")
                                                {
                                                    postflopRows.UnionWith(new string[] { "DelFLOAT_F_Row" });

                                                    cellName = "DelFLOAT_C_R";
                                                }
                                                else if (actionSequences[0] == "brc")
                                                    cellName = string.Empty;
                                                else
                                                    cellName = string.Empty;
                                            }
                                            else if (actionSequences[1] == "xbc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbrc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "bc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "brc")
                                                    cellName = string.Empty;
                                                else
                                                    cellName = string.Empty;
                                            }
                                            else if (actionSequences[1] == "xbrc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbrc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "bc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "brc")
                                                    cellName = string.Empty;
                                                else
                                                    cellName = string.Empty;
                                            }
                                            else if (actionSequences[1] == "bc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbrc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "bc")
                                                {
                                                    postflopRows.UnionWith(new string[] { "FLOAT_F_Row" });

                                                    cellName = "FLOAT_C_R";
                                                }
                                                else if (actionSequences[0] == "brc")
                                                    cellName = string.Empty;
                                                else
                                                    cellName = string.Empty;
                                            }
                                            else if (actionSequences[1] == "brc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbrc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "bc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "brc")
                                                    cellName = string.Empty;
                                                else
                                                    cellName = string.Empty;
                                            }
                                            else
                                                cellName = string.Empty;
                                        }
                                    }
                                    else if (actionSequences[2] == "b")
                                    {
                                        if (actionSequences[1] == "xx")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                if (action.ActionType == BettingActionType.Raise)
                                                {
                                                    postflopRows.UnionWith(new string[] { "RvSDELAY_Row" });

                                                    cellName = "RvSDELAY_R";
                                                }
                                                else
                                                    cellName = "CvSDELAY_R";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbrc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FLOAT_F_XB_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "FLOAT_C_F_XB";
                                            }
                                            else if (actionSequences[0] == "bc")
                                            {
                                                if (action.ActionType == BettingActionType.Raise)
                                                {
                                                    postflopRows.UnionWith(new string[] { "RvDELAY_Row" });

                                                    cellName = "RvDELAY_R";
                                                }
                                                else
                                                    cellName = "CvDELAY_R";
                                            }
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "xbc")
                                        {
                                            if (actionSequences[0] == "xx")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "xbrc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "DelFLOAT_F_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "DelFLOAT_C_T_B";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FLOAT_F_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "FLOAT_C_T_B";
                                            }
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "bc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvDELAY_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvDELAY_T_B";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbrc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FLOAT_F_BB_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "FLOAT_C_F_BB";
                                            }
                                            else if (actionSequences[0] == "bc")
                                                cellName = action.ActionType == BettingActionType.Raise ? "RvCB_R" : "CvCB_R";
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "brc")
                                        {
                                            if (actionSequences[0] == "xx")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else
                                            cellName = string.Empty;
                                    }
                                    else
                                        cellName = string.Empty;
                                }
                                //OOP preflop raiser
                                else if (postflopHud.SetType == SetType.PostflopHuOopRaiser)
                                {
                                    if (actionSequences[2] == string.Empty)
                                    {
                                        if (actionSequences[1] == "xx")
                                        {
                                            if (actionSequences[0] == "xx")
                                                cellName = action.ActionType == BettingActionType.Bet ? "SDELAY_R" : "SDELAYX_R";
                                            else if (actionSequences[0] == "xbc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbrc")
                                            {
                                                postflopRows.UnionWith(new string[] { "RvFLOAT_XB_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "RvFLOAT_F_XB" : "RvFLOAT_F_XX";
                                            }
                                            else if (actionSequences[0] == "bc")
                                                cellName = action.ActionType == BettingActionType.Bet ? "DELAY_R" : "DELAYX_R";
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "xbc")
                                        {
                                            if (actionSequences[0] == "xx")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "xbrc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "RvDelFLOAT_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "RvDelFLOAT_T_B" : "RvDelFLOAT_T_X";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                            {
                                                postflopRows.UnionWith(new string[] { "RvFLOAT_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "RvFLOAT_T_B" : "RvFLOAT_T_X";
                                            }
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "bc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "DELAY_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "DELAY_T_B" : "DELAY_T_X";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbrc")
                                            {
                                                postflopRows.UnionWith(new string[] { "RvFLOAT_BB_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "RvFLOAT_F_BB" : "RvFLOAT_F_BX";
                                            }
                                            else if (actionSequences[0] == "bc")
                                                cellName = action.ActionType == BettingActionType.Bet ? "CB_R" : "CX_R";
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "brc")
                                        {
                                            if (actionSequences[0] == "xx")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else
                                            cellName = string.Empty;
                                    }
                                    else if (actionSequences[2] == "xb")
                                    {
                                        if (actionSequences[1] == "xx")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvSDelFLOAT_Row" });

                                                if (action.ActionType == BettingActionType.Raise)
                                                {
                                                    postflopRows.UnionWith(new string[] { "RvSDelFLOAT_Row" });

                                                    cellName = "RvSDelFLOAT_R";
                                                }
                                                else
                                                    cellName = "CvSDelFLOAT_R";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvFLOAT_XB_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvFLOAT_F_XB";
                                            }
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvDelFLOAT_Row" });

                                                if (action.ActionType == BettingActionType.Raise)
                                                {
                                                    postflopRows.UnionWith(new string[] { "RvDelFLOAT_Row" });

                                                    cellName = "RvDelFLOAT_R";
                                                }
                                                else
                                                    cellName = "CvDelFLOAT_R";
                                            }
                                            else if (actionSequences[0] == "brc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FCBvR_XB_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CCBvR_F_XB";
                                            }
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "xbc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvDelFLOAT_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvDelFLOAT_T_B";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvFLOAT_BB_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvFLOAT_F_BB";
                                            }
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvFLOAT_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvFLOAT_T_B";
                                            }
                                            else if (actionSequences[0] == "brc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FCBvR_BB_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CCBvR_F_BB";
                                            }
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "xbrc")
                                        {
                                            if (actionSequences[0] == "xx")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "bc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvFLOATDELAY_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvFLOATDELAY_R";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                            {
                                                if (action.ActionType == BettingActionType.Raise)
                                                {
                                                    postflopRows.UnionWith(new string[] { "RvFLOAT_Row" });

                                                    cellName = "RvFLOAT_R";
                                                }
                                                else
                                                    cellName = "CvFLOAT_R";
                                            }
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "brc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "DELAY_F_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "DELAY_C_T_B";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FCBvR_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CCBvR_T_B";
                                            }
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else
                                            cellName = string.Empty;
                                    }
                                    else if (actionSequences[2] == "br")
                                    {
                                        if (action.ActionType == BettingActionType.Raise)
                                        {
                                            postflopRows.UnionWith(new string[] { "THREEBET_Row" });

                                            cellName = "THREEBET_R";

                                            if (actionSequences[1] == "xx")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    postflopRows.UnionWith(new string[] { "SDELAY_F_Row" });
                                                else if (actionSequences[0] == "bc")
                                                    postflopRows.UnionWith(new string[] { "DELAY_F_Row" });
                                            }
                                        }
                                        else
                                        {
                                            if (actionSequences[1] == "xx")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    postflopRows.UnionWith(new string[] { "SDELAY_F_Row" });

                                                    cellName = "SDELAY_C_R";
                                                }
                                                else if (actionSequences[0] == "xbc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbrc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "bc")
                                                {
                                                    postflopRows.UnionWith(new string[] { "DELAY_F_Row" });

                                                    cellName = "DELAY_C_R";
                                                }
                                                else if (actionSequences[0] == "brc")
                                                    cellName = string.Empty;
                                                else
                                                    cellName = string.Empty;
                                            }
                                            else if (actionSequences[1] == "xbc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbrc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "bc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "brc")
                                                    cellName = string.Empty;
                                                else
                                                    cellName = string.Empty;
                                            }
                                            else if (actionSequences[1] == "xbrc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbrc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "bc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "brc")
                                                    cellName = string.Empty;
                                                else
                                                    cellName = string.Empty;
                                            }
                                            else if (actionSequences[1] == "bc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbrc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "bc")
                                                    cellName = "CCBvR_R";
                                                else if (actionSequences[0] == "brc")
                                                    cellName = string.Empty;
                                                else
                                                    cellName = string.Empty;
                                            }
                                            else if (actionSequences[1] == "brc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbrc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "bc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "brc")
                                                    cellName = string.Empty;
                                                else
                                                    cellName = string.Empty;
                                            }
                                            else
                                                cellName = string.Empty;
                                        }
                                    }
                                    else
                                        cellName = string.Empty;
                                }
                                //OOP preflop caller
                                else if (postflopHud.SetType == SetType.PostflopHuOopCaller)
                                {
                                    if (actionSequences[2] == string.Empty)
                                    {
                                        if (actionSequences[1] == "xx")
                                        {
                                            if (actionSequences[0] == "xx")
                                                cellName = action.ActionType == BettingActionType.Bet ? "SPROBE_R" : "SPROBEX_R";
                                            else if (actionSequences[0] == "xbc")
                                                cellName = action.ActionType == BettingActionType.Bet ? "PROBE_R" : "PROBEX_R";
                                            else if (actionSequences[0] == "xbrc")
                                            {
                                                postflopRows.UnionWith(new string[] { "RvCB_XB_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "RvCB_F_XB" : "RvCB_F_XX";
                                            }
                                            else if (actionSequences[0] == "bc")
                                            {
                                                postflopRows.UnionWith(new string[] { "DONK_XB_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "DONK_F_XB" : "DONK_F_XX";
                                            }
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "xbc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "DONKDELAY_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "DONKDELAY_R" : "DONKDELAYX_R";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                                cellName = action.ActionType == BettingActionType.Bet ? "DONK_R" : "DONKX_R";
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "xbrc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "RvDELAY_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "RvDELAY_T_B" : "RvDELAY_T_X";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                            {
                                                postflopRows.UnionWith(new string[] { "RvCB_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "RvCB_T_B" : "RvCB_T_X";
                                            }
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "bc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "PROBE_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "PROBE_T_B" : "PROBE_T_X";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                            {
                                                postflopRows.UnionWith(new string[] { "DONK_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "DONK_T_B" : "DONK_T_X";
                                            }
                                            else if (actionSequences[0] == "xbrc")
                                            {
                                                postflopRows.UnionWith(new string[] { "RvCB_BB_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "RvCB_F_BB" : "RvCB_F_BX";
                                            }
                                            else if (actionSequences[0] == "bc")
                                            {
                                                postflopRows.UnionWith(new string[] { "DONK_BB_Row" });

                                                cellName = action.ActionType == BettingActionType.Bet ? "DONK_F_BB" : "DONK_F_BX";
                                            }
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "brc")
                                        {
                                            if (actionSequences[0] == "xx")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else
                                            cellName = string.Empty;
                                    }
                                    else if (actionSequences[2] == "xb")
                                    {
                                        if (actionSequences[1] == "xx")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                if (action.ActionType == BettingActionType.Raise)
                                                {
                                                    postflopRows.UnionWith(new string[] { "RvSDELAY_Row" });

                                                    cellName = "RvSDELAY_R";
                                                }
                                                else
                                                    cellName = "CvSDELAY_R";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                            {
                                                if (action.ActionType == BettingActionType.Raise)
                                                {
                                                    postflopRows.UnionWith(new string[] { "RvDELAY_Row" });

                                                    cellName = "RvDELAY_R";
                                                }
                                                else
                                                    cellName = "CvDELAY_R";
                                            }
                                            else if (actionSequences[0] == "xbrc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvDelFLOATXRCB_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvDelFLOATXRCB_R";
                                            }
                                            else if (actionSequences[0] == "bc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvDelFLOATDONK_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvDelFLOATDONK_R";
                                            }
                                            else if (actionSequences[0] == "brc")
                                            {
                                                postflopRows.UnionWith(new string[] { "DONK_F_XB_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "DONK_C_F_XB";
                                            }
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "xbc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvDELAY_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvDELAY_T_B";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                                cellName = action.ActionType == BettingActionType.Raise ? "RvCB_R" : "CvCB_R";
                                            else if (actionSequences[0] == "xbrc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvFLOATXRCB_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvFLOATXRCB_T_B";
                                            }
                                            else if (actionSequences[0] == "bc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvFLOATDONK_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvFLOATDONK_T_B";
                                            }
                                            else if (actionSequences[0] == "brc")
                                            {
                                                postflopRows.UnionWith(new string[] { "DONK_F_BB_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "DONK_C_F_BB";
                                            }
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "xbrc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvFLOATXRDELAY_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvFLOATXRDELAY_R";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvFLOATXRCB_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvFLOATXRCB_R";
                                            }
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "bc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvFLOATPROBE_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvFLOATPROBE_R";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvFLOATDONK_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvFLOATDONK_R";
                                            }
                                            else if (actionSequences[0] == "xbrc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvDblFLOATXRCB_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvDblFLOATXRCB_R";
                                            }
                                            else if (actionSequences[0] == "bc")
                                            {
                                                postflopRows.UnionWith(new string[] { "FvDblFLOATDONK_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "CvDblFLOATDONK_R";
                                            }
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else if (actionSequences[1] == "brc")
                                        {
                                            if (actionSequences[0] == "xx")
                                            {
                                                postflopRows.UnionWith(new string[] { "PROBE_F_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "PROBE_C_T_B";
                                            }
                                            else if (actionSequences[0] == "xbc")
                                            {
                                                postflopRows.UnionWith(new string[] { "DONK_F_B_Row" });

                                                cellName = action.ActionType == BettingActionType.Raise ? string.Empty : "DONK_C_T_B";
                                            }
                                            else if (actionSequences[0] == "xbrc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "bc")
                                                cellName = string.Empty;
                                            else if (actionSequences[0] == "brc")
                                                cellName = string.Empty;
                                            else
                                                cellName = string.Empty;
                                        }
                                        else
                                            cellName = string.Empty;
                                    }
                                    else if (actionSequences[2] == "br")
                                    {
                                        if (action.ActionType == BettingActionType.Raise)
                                        {
                                            postflopRows.UnionWith(new string[] { "THREEBET_Row" });

                                            cellName = "THREEBET_R";

                                            if (actionSequences[1] == "xx")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    postflopRows.UnionWith(new string[] { "SPROBE_F_Row" });
                                                else if (actionSequences[0] == "xbc")
                                                    postflopRows.UnionWith(new string[] { "PROBE_F_Row" });
                                            }
                                            else if (actionSequences[1] == "xbc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    postflopRows.UnionWith(new string[] { "DONKDELAY_F_Row" });
                                            }
                                        }
                                        else
                                        {
                                            if (actionSequences[1] == "xx")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    postflopRows.UnionWith(new string[] { "SPROBE_F_Row" });

                                                    cellName = "SPROBE_C_R";
                                                }
                                                else if (actionSequences[0] == "xbc")
                                                {
                                                    postflopRows.UnionWith(new string[] { "PROBE_F_Row" });

                                                    cellName = "PROBE_C_R";
                                                }
                                                else if (actionSequences[0] == "xbrc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "bc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "brc")
                                                    cellName = string.Empty;
                                                else
                                                    cellName = string.Empty;
                                            }
                                            else if (actionSequences[1] == "xbc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                {
                                                    postflopRows.UnionWith(new string[] { "DONKDELAY_F_Row" });

                                                    cellName = "DONKDELAY_C_R";
                                                }
                                                else if (actionSequences[0] == "xbc")
                                                    cellName = "DONK_C_R";
                                                else if (actionSequences[0] == "xbrc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "bc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "brc")
                                                    cellName = string.Empty;
                                                else
                                                    cellName = string.Empty;
                                            }
                                            else if (actionSequences[1] == "xbrc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbrc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "bc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "brc")
                                                    cellName = string.Empty;
                                                else
                                                    cellName = string.Empty;
                                            }
                                            else if (actionSequences[1] == "bc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbrc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "bc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "brc")
                                                    cellName = string.Empty;
                                                else
                                                    cellName = string.Empty;
                                            }
                                            else if (actionSequences[1] == "brc")
                                            {
                                                if (actionSequences[0] == "xx")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "xbrc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "bc")
                                                    cellName = string.Empty;
                                                else if (actionSequences[0] == "brc")
                                                    cellName = string.Empty;
                                                else
                                                    cellName = string.Empty;
                                            }
                                            else
                                                cellName = string.Empty;
                                        }
                                    }
                                    else
                                        cellName = string.Empty;
                                }
                            }
                        }
                    }
                }

                //Update pot type
                string actionAbbr = string.Empty;

                if (action.ActionType == BettingActionType.Fold)
                    actionAbbr = "f";
                else if (action.ActionType == BettingActionType.Check)
                    actionAbbr = "x";
                else if (action.ActionType == BettingActionType.Call)
                    actionAbbr = "c";
                else if (action.ActionType == BettingActionType.Bet)
                    actionAbbr = "b";
                else if (action.ActionType == BettingActionType.Raise)
                    actionAbbr = "r";

                actionSequences[action.Round - 2] += actionAbbr;
            }

            //Update at hero turn
            //Multiway
            if (postflopHud.SetType == SetType.PostflopGeneral)
            {
                int betsRaisesCount = actionSequences[gc.Round - 2].Count(a => a is 'b' or 'r');

                //Raise pot
                if (betsRaisesCount == 2)
                {
                    postflopRows.Add("FvThreeBet_Row");

                    if (postflopActions.Where(a => a.ActionType is BettingActionType.Bet or BettingActionType.Raise).Skip(1).First().Player != player)
                        postflopRows.Add("ThreeBet_Row");
                }
                //3bet pot
                else if (betsRaisesCount == 3)
                {
                    if (postflopActions.Where(a => a.ActionType is BettingActionType.Bet or BettingActionType.Raise).Skip(2).First().Player != player)
                        postflopRows.Add("FourBet_Row");
                }
            }
            //Hu
            else
            {
                //Flop
                if (gc.Round == 2)
                {
                    if (actionSequences[0] is "b" or "xb")
                        postflopRows.UnionWith(new string[] { "THREEBET_Row" });
                    else if (actionSequences[0] is "br" or "xbr")
                        postflopRows.UnionWith(new string[] { "FvTHREEBET_Row", "FOURBET_Row" });

                    //IP preflop raiser
                    if (postflopHud.SetType == SetType.PostflopHuIpRaiser)
                    {
                        if (actionSequences[0] == string.Empty)
                            postflopRows.UnionWith(new string[] { "FvDONK_BB_Row", "FLOATDONK_Row" });
                        else if (actionSequences[0] == "xb")
                            postflopRows.UnionWith(new string[] { "FCBvR_BB_Row", "FLOATXRCB_Row" });
                        else if (actionSequences[0] == "br")
                            postflopRows.UnionWith(new string[] { "RvDONK_BB_Row" });
                    }
                    //IP preflop caller
                    else if (postflopHud.SetType == SetType.PostflopHuIpCaller)
                    {
                        if (actionSequences[0] == string.Empty)
                        {
                            //Nothing
                        }
                        else if (actionSequences[0] == "xb")
                            postflopRows.UnionWith(new string[] { "FLOAT_BB_Row", "FLOAT_F_Row" });
                        else if (actionSequences[0] == "br")
                            postflopRows.UnionWith(new string[] { "RvCB_BB_Row" });
                    }
                    //OOP preflop raiser
                    else if (postflopHud.SetType == SetType.PostflopHuOopRaiser)
                    {
                        if (actionSequences[0] == "x")
                            postflopRows.UnionWith(new string[] { "FvDelFLOAT_Row", "FvFLOAT_BB_Row", "RvFLOAT_Row" });
                        else if (actionSequences[0] == "xbr")
                            postflopRows.UnionWith(new string[] { "RvFLOAT_BB_Row" });
                        else if (actionSequences[0] == "b")
                            postflopRows.UnionWith(new string[] { "FCBvR_BB_Row" });
                    }
                    //OOP preflop caller
                    else if (postflopHud.SetType == SetType.PostflopHuOopCaller)
                    {
                        if (actionSequences[0] == "x")
                        {
                            //Nothing
                        }
                        else if (actionSequences[0] == "xbr")
                            postflopRows.UnionWith(new string[] { "RvCB_BB_Row", "FvFLOATXRCB_Row" });
                        else if (actionSequences[0] == "b")
                            postflopRows.UnionWith(new string[] { "DONK_BB_Row", "DONK_F_BB_Row", "FvFLOATDONK_Row" });
                    }
                }
                //Turn
                else if (gc.Round == 3)
                {
                    if (actionSequences[1] is "b" or "xb")
                        postflopRows.UnionWith(new string[] { "THREEBET_Row" });
                    else if (actionSequences[1] is "br" or "xbr")
                        postflopRows.UnionWith(new string[] { "FvTHREEBET_Row", "FOURBET_Row" });

                    //IP preflop raiser
                    if (postflopHud.SetType == SetType.PostflopHuIpRaiser)
                    {
                        if (actionSequences[1] == string.Empty)
                        {
                            if (actionSequences[0] == "xx")
                                postflopRows.UnionWith(new string[] { "FvPROBE_B_Row", "RvPROBE_Row", "FLOATPROBE_Row" });
                            else if (actionSequences[0] == "xbc")
                                postflopRows.UnionWith(new string[] { "FvDONK_B_Row", "FLOATDONK_Row" });
                            else if (actionSequences[0] == "xbrc")
                                postflopRows.UnionWith(new string[] { "FCBvR_BB_Row", "FLOATXRCB_Row" });
                            else if (actionSequences[0] == "bc")
                                postflopRows.UnionWith(new string[] { "FvDONK_BB_Row", "FLOATDONK_Row" });
                            else if (actionSequences[0] == "brc")
                                postflopRows.UnionWith(new string[] { "RvDONK_BB_Row" });
                        }
                        else if (actionSequences[1] == "xb")
                        {
                            if (actionSequences[0] == "xx")
                                postflopRows.UnionWith(new string[] { "DELAY_B_Row", "DELAY_F_Row", "DELAY_F_B_Row", "FvDONKDELAY_Row", "FLOATXRDELAY_Row" });
                            else if (actionSequences[0] == "xbc")
                                postflopRows.UnionWith(new string[] { "FCBvR_B_Row", "FLOATXRCB_Row" });
                            else if (actionSequences[0] == "xbrc")
                                postflopRows.UnionWith(new string[] { "FLOATXRCB_B_Row" });
                            else if (actionSequences[0] == "bc")
                                postflopRows.UnionWith(new string[] { "FLOATDONK_B_Row" });
                            else if (actionSequences[0] == "brc")
                            {
                                //Nothing
                            }
                        }
                        else if (actionSequences[1] == "br")
                        {
                            if (actionSequences[0] == "xx")
                                postflopRows.UnionWith(new string[] { "RvPROBE_B_Row" });
                            else if (actionSequences[0] == "xbc")
                                postflopRows.UnionWith(new string[] { "RvDONK_B_Row" });
                            else if (actionSequences[0] == "xbrc")
                            {
                                //Nothing
                            }
                            else if (actionSequences[0] == "bc")
                            {
                                //Nothing
                            }
                            else if (actionSequences[0] == "brc")
                            {
                                //Nothing
                            }
                        }
                    }
                    //IP preflop caller
                    else if (postflopHud.SetType == SetType.PostflopHuIpCaller)
                    {
                        if (actionSequences[1] == string.Empty)
                        {
                            if (actionSequences[0] == "xx")
                                postflopRows.UnionWith(new string[] { "FvDELAY_B_Row", "RvDELAY_Row", "DelFLOAT_Row", "FLOATDELAY_Row" });
                            else if (actionSequences[0] == "xbc")
                                postflopRows.UnionWith(new string[] { "FLOAT_BB_Row" });
                            else if (actionSequences[0] == "xbrc")
                                postflopRows.UnionWith(new string[] { "FLOAT_F_BB_Row" });
                            else if (actionSequences[0] == "bc")
                            {
                                //Nothing
                            }
                            else if (actionSequences[0] == "brc")
                                postflopRows.UnionWith(new string[] { "RvCB_BB_Row" });
                        }
                        else if (actionSequences[1] == "xb")
                        {
                            if (actionSequences[0] == "xx")
                                postflopRows.UnionWith(new string[] { "DelFLOAT_B_Row", "DelFLOAT_F_Row" });
                            else if (actionSequences[0] == "xbc")
                            {
                                //Nothing
                            }
                            else if (actionSequences[0] == "xbrc")
                            {
                                //Nothing
                            }
                            else if (actionSequences[0] == "bc")
                                postflopRows.UnionWith(new string[] { "FLOAT_B_Row", "FLOAT_F_Row" });
                            else if (actionSequences[0] == "brc")
                            {
                                //Nothing
                            }
                        }
                        else if (actionSequences[1] == "br")
                        {
                            if (actionSequences[0] == "xx")
                                postflopRows.UnionWith(new string[] { "RvDELAY_B_Row" });
                            else if (actionSequences[0] == "xbc")
                            {
                                //Nothing
                            }
                            else if (actionSequences[0] == "xbrc")
                            {
                                //Nothing
                            }
                            else if (actionSequences[0] == "bc")
                                postflopRows.UnionWith(new string[] { "RvCB_B_Row" });
                            else if (actionSequences[0] == "brc")
                            {
                                //Nothing
                            }
                        }
                    }
                    //OOP preflop raiser
                    else if (postflopHud.SetType == SetType.PostflopHuOopRaiser)
                    {
                        if (actionSequences[1] == "x")
                        {
                            if (actionSequences[0] == "xx")
                                postflopRows.UnionWith(new string[] { "FvDelFLOAT_Row", "FvDelFLOAT_B_Row", "RvDelFLOAT_Row" });
                            else if (actionSequences[0] == "xbc")
                                postflopRows.UnionWith(new string[] { "FvFLOAT_BB_Row", "FvFLOAT_XB_Row" });
                            else if (actionSequences[0] == "xbrc")
                                postflopRows.UnionWith(new string[] { "RvFLOAT_XB_Row" });
                            else if (actionSequences[0] == "bc")
                                postflopRows.UnionWith(new string[] { "FvFLOAT_B_Row", "FvDelFLOAT_Row", "RvFLOAT_Row" });
                            else if (actionSequences[0] == "brc")
                                postflopRows.UnionWith(new string[] { "FCBvR_BB_Row", "FCBvR_XB_Row" });
                        }
                        else if (actionSequences[1] == "xbr")
                        {
                            if (actionSequences[0] == "xx")
                                postflopRows.UnionWith(new string[] { "RvDelFLOAT_B_Row" });
                            else if (actionSequences[0] == "xbc")
                            {
                                //Nothing
                            }
                            else if (actionSequences[0] == "xbrc")
                            {
                                //Nothing
                            }
                            else if (actionSequences[0] == "bc")
                                postflopRows.UnionWith(new string[] { "RvFLOAT_B_Row" });
                            else if (actionSequences[0] == "brc")
                            {
                                //Nothing
                            }
                        }
                        else if (actionSequences[1] == "b")
                        {
                            if (actionSequences[0] == "xx")
                                postflopRows.UnionWith(new string[] { "DELAY_B_Row", "DELAY_F_Row", "DELAY_F_B_Row", "FvFLOATDELAY_Row" });
                            else if (actionSequences[0] == "xbc")
                            {
                                //Nothing
                            }
                            else if (actionSequences[0] == "xbrc")
                                postflopRows.UnionWith(new string[] { "RvFLOAT_BB_Row" });
                            else if (actionSequences[0] == "bc")
                                postflopRows.UnionWith(new string[] { "FCBvR_B_Row" });
                            else if (actionSequences[0] == "brc")
                            {
                                //Nothing
                            }
                        }
                    }
                    //OOP preflop caller
                    else if (postflopHud.SetType == SetType.PostflopHuOopCaller)
                    {
                        if (actionSequences[1] == "x")
                        {
                            if (actionSequences[0] == "xx")
                                postflopRows.UnionWith(new string[] { "RvDELAY_Row", "DONKDELAY_Row", "FvDELAY_B_Row" });
                            else if (actionSequences[0] == "xbc")
                            {
                                //Nothing
                            }
                            else if (actionSequences[0] == "xbrc")
                                postflopRows.UnionWith(new string[] { "FvFLOATXRCB_Row", "FvFLOATXRCB_B_Row", "FvDelFLOATXRCB_Row", "RvCB_XB_Row" });
                            else if (actionSequences[0] == "bc")
                                postflopRows.UnionWith(new string[] { "FvFLOATDONK_Row", "FvFLOATDONK_B_Row", "DONK_XB_Row", "FvDelFLOATDONK_Row" });
                            else if (actionSequences[0] == "brc")
                                postflopRows.UnionWith(new string[] { "DONK_F_BB_Row", "DONK_F_XB_Row" });
                        }
                        else if (actionSequences[1] == "xbr")
                        {
                            if (actionSequences[0] == "xx")
                                postflopRows.UnionWith(new string[] { "RvDELAY_B_Row", "FvFLOATXRDELAY_Row" });
                            else if (actionSequences[0] == "xbc")
                                postflopRows.UnionWith(new string[] { "RvCB_B_Row", "FvFLOATXRCB_Row" });
                            else if (actionSequences[0] == "xbrc")
                            {
                                //Nothing
                            }
                            else if (actionSequences[0] == "bc")
                            {
                                //Nothing
                            }
                            else if (actionSequences[0] == "brc")
                            {
                                //Nothing
                            }
                        }
                        else if (actionSequences[1] == "b")
                        {
                            if (actionSequences[0] == "xx")
                                postflopRows.UnionWith(new string[] { "PROBE_B_Row", "PROBE_F_Row", "PROBE_F_B_Row", "FvFLOATPROBE_Row" });
                            else if (actionSequences[0] == "xbc")
                                postflopRows.UnionWith(new string[] { "DONK_B_Row", "DONK_F_B_Row", "FvFLOATDONK_Row" });
                            else if (actionSequences[0] == "xbrc")
                                postflopRows.UnionWith(new string[] { "FvDblFLOATXRCB_Row" });
                            else if (actionSequences[0] == "bc")
                                postflopRows.UnionWith(new string[] { "FvDblFLOATDONK_Row" });
                            else if (actionSequences[0] == "brc")
                            {
                                //Nothing
                            }
                        }
                    }
                }
                //River
                else if (gc.Round == 4)
                {
                    if (actionSequences[2] is "b" or "xb")
                        postflopRows.UnionWith(new string[] { "THREEBET_Row" });
                    else if (actionSequences[2] is "br" or "xbr")
                        postflopRows.UnionWith(new string[] { "FvTHREEBET_Row", "FOURBET_Row" });

                    //IP preflop raiser
                    if (postflopHud.SetType == SetType.PostflopHuIpRaiser)
                    {
                        if (actionSequences[2] == string.Empty)
                        {
                            if (actionSequences[1] == "xx")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "RvSPROBE_Row" });
                                else if (actionSequences[0] == "xbc")
                                    postflopRows.UnionWith(new string[] { "RvPROBE_Row" });
                                else if (actionSequences[0] == "xbrc")
                                    postflopRows.UnionWith(new string[] { "DelFLOATXRCB_Row", "FCBvR_XB_Row" });
                                else if (actionSequences[0] == "bc")
                                    postflopRows.UnionWith(new string[] { "DelFLOATDONK_Row", "FvDONK_XB_Row" });
                                else if (actionSequences[0] == "brc")
                                    postflopRows.UnionWith(new string[] { "RvDONK_XB_Row" });
                            }
                            else if (actionSequences[1] == "xbc")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "DELAY_B_Row", "FvDONKDELAY_Row", "RvDONKDELAY_Row" });
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                    postflopRows.UnionWith(new string[] { "FLOATXRCB_B_Row" });
                                else if (actionSequences[0] == "bc")
                                    postflopRows.UnionWith(new string[] { "FLOATDONK_B_Row" });
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "xbrc")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "DELAY_F_B_Row", "FLOATXRDELAY_Row" });
                                else if (actionSequences[0] == "xbc")
                                    postflopRows.UnionWith(new string[] { "FCBvR_B_Row", "FLOATXRCB_Row" });
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "bc")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "FvPROBE_B_Row", "FLOATPROBE_Row" });
                                else if (actionSequences[0] == "xbc")
                                    postflopRows.UnionWith(new string[] { "FvDONK_B_Row", "FLOATDONK_Row" });
                                else if (actionSequences[0] == "xbrc")
                                    postflopRows.UnionWith(new string[] { "DblFLOATXRCB_Row" });
                                else if (actionSequences[0] == "bc")
                                    postflopRows.UnionWith(new string[] { "DblFLOATDONK_Row" });
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "brc")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "RvPROBE_B_Row" });
                                else if (actionSequences[0] == "xbc")
                                    postflopRows.UnionWith(new string[] { "RvDONK_B_Row" });
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                        }
                        else if (actionSequences[2] == "xb")
                        {
                            if (actionSequences[1] == "xx")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "SDELAY_F_Row" });
                                else if (actionSequences[0] == "xbc")
                                    postflopRows.UnionWith(new string[] { "DELAY_F_Row" });
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "xbc")
                            {
                                if (actionSequences[0] == "xx")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "xbrc")
                            {
                                if (actionSequences[0] == "xx")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "bc")
                            {
                                if (actionSequences[0] == "xx")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "brc")
                            {
                                if (actionSequences[0] == "xx")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                        }
                    }
                    //IP preflop caller
                    else if (postflopHud.SetType == SetType.PostflopHuIpCaller)
                    {
                        if (actionSequences[2] == string.Empty)
                        {
                            if (actionSequences[1] == "xx")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "RvSDELAY_Row", "SDelFLOAT_Row" });
                                else if (actionSequences[0] == "xbc")
                                    postflopRows.UnionWith(new string[] { "FLOAT_XB_Row" });
                                else if (actionSequences[0] == "xbrc")
                                    postflopRows.UnionWith(new string[] { "FLOAT_F_XB_Row" });
                                else if (actionSequences[0] == "bc")
                                    postflopRows.UnionWith(new string[] { "RvDELAY_Row", "DelFLOAT_Row" });
                                else if (actionSequences[0] == "brc")
                                    postflopRows.UnionWith(new string[] { "RvCB_XB_Row" });
                            }
                            else if (actionSequences[1] == "xbc")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "DelFLOAT_B_Row" });
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                    postflopRows.UnionWith(new string[] { "FLOAT_B_Row" });
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "xbrc")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "DelFLOAT_F_B_Row" });
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                    postflopRows.UnionWith(new string[] { "FLOAT_F_B_Row" });
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "bc")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "FvDELAY_B_Row", "FLOATDELAY_Row" });
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                    postflopRows.UnionWith(new string[] { "FLOAT_F_BB_Row" });
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "brc")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "RvDELAY_B_Row" });
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                    postflopRows.UnionWith(new string[] { "RvCB_B_Row" });
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                        }
                        else if (actionSequences[2] == "xb")
                        {
                            if (actionSequences[1] == "xx")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "SDelFLOAT_F_Row" });
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                    postflopRows.UnionWith(new string[] { "DelFLOAT_F_Row" });
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "xbc")
                            {
                                if (actionSequences[0] == "xx")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "xbrc")
                            {
                                if (actionSequences[0] == "xx")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "bc")
                            {
                                if (actionSequences[0] == "xx")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                    postflopRows.UnionWith(new string[] { "FLOAT_F_Row" });
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "brc")
                            {
                                if (actionSequences[0] == "xx")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                        }
                    }
                    //OOP preflop raiser
                    else if (postflopHud.SetType == SetType.PostflopHuOopRaiser)
                    {
                        if (actionSequences[2] == "x")
                        {
                            if (actionSequences[1] == "xx")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "FvSDelFLOAT_Row", "RvSDelFLOAT_Row" });
                                else if (actionSequences[0] == "xbc")
                                    postflopRows.UnionWith(new string[] { "FvFLOAT_XB_Row" });
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                    postflopRows.UnionWith(new string[] { "FvDelFLOAT_Row" });
                                else if (actionSequences[0] == "brc")
                                    postflopRows.UnionWith(new string[] { "FCBvR_XB_Row" });
                            }
                            else if (actionSequences[1] == "xbc")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "FvDelFLOAT_B_Row" });
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                    postflopRows.UnionWith(new string[] { "FvFLOAT_B_Row" });
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "xbrc")
                            {
                                if (actionSequences[0] == "xx")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "bc")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "FvFLOATDELAY_Row" });
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                    postflopRows.UnionWith(new string[] { "RvFLOAT_Row" });
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "brc")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "DELAY_F_B_Row" });
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                    postflopRows.UnionWith(new string[] { "FCBvR_B_Row" });
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                        }
                        else if (actionSequences[2] == "b")
                        {
                            if (actionSequences[1] == "xx")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "SDELAY_F_Row" });
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                    postflopRows.UnionWith(new string[] { "DELAY_F_Row" });
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "xbc")
                            {
                                if (actionSequences[0] == "xx")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "xbrc")
                            {
                                if (actionSequences[0] == "xx")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "bc")
                            {
                                if (actionSequences[0] == "xx")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "brc")
                            {
                                if (actionSequences[0] == "xx")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                        }
                    }
                    //OOP preflop caller
                    else if (postflopHud.SetType == SetType.PostflopHuOopCaller)
                    {
                        if (actionSequences[2] == "x")
                        {
                            if (actionSequences[1] == "xx")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "RvSDELAY_Row" });
                                else if (actionSequences[0] == "xbc")
                                    postflopRows.UnionWith(new string[] { "RvDELAY_Row" });
                                else if (actionSequences[0] == "xbrc")
                                    postflopRows.UnionWith(new string[] { "FvDelFLOATXRCB_Row" });
                                else if (actionSequences[0] == "bc")
                                    postflopRows.UnionWith(new string[] { "FvDelFLOATDONK_Row" });
                                else if (actionSequences[0] == "brc")
                                    postflopRows.UnionWith(new string[] { "DONK_F_XB_Row" });
                            }
                            else if (actionSequences[1] == "xbc")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "FvDELAY_B_Row" });
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                    postflopRows.UnionWith(new string[] { "FvFLOATXRCB_B_Row" });
                                else if (actionSequences[0] == "bc")
                                    postflopRows.UnionWith(new string[] { "FvFLOATDONK_B_Row" });
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "xbrc")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "FvFLOATXRDELAY_Row" });
                                else if (actionSequences[0] == "xbc")
                                    postflopRows.UnionWith(new string[] { "FvFLOATXRCB_Row" });
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "bc")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "FvFLOATPROBE_Row" });
                                else if (actionSequences[0] == "xbc")
                                    postflopRows.UnionWith(new string[] { "FvFLOATDONK_Row" });
                                else if (actionSequences[0] == "xbrc")
                                    postflopRows.UnionWith(new string[] { "FvDblFLOATXRCB_Row" });
                                else if (actionSequences[0] == "bc")
                                    postflopRows.UnionWith(new string[] { "FvDblFLOATDONK_Row" });
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "brc")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "PROBE_F_B_Row" });
                                else if (actionSequences[0] == "xbc")
                                    postflopRows.UnionWith(new string[] { "DONK_F_B_Row" });
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                        }
                        else if (actionSequences[2] == "b")
                        {
                            if (actionSequences[1] == "xx")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "SPROBE_F_Row" });
                                else if (actionSequences[0] == "xbc")
                                    postflopRows.UnionWith(new string[] { "PROBE_F_Row" });
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "xbc")
                            {
                                if (actionSequences[0] == "xx")
                                    postflopRows.UnionWith(new string[] { "DONKDELAY_F_Row" });
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "xbrc")
                            {
                                if (actionSequences[0] == "xx")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "bc")
                            {
                                if (actionSequences[0] == "xx")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                            else if (actionSequences[1] == "brc")
                            {
                                if (actionSequences[0] == "xx")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "xbrc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "bc")
                                {
                                    //Nothing
                                }
                                else if (actionSequences[0] == "brc")
                                {
                                    //Nothing
                                }
                            }
                        }
                    }
                }
            }

            postflopHud.SetRows(postflopRows.ToArray());

        #endregion

        End:

            DataCell selectedCell = null;

            if (cellName != string.Empty)
                selectedCell = preflopHud?[cellName] ?? postflopHud?[cellName];

            return selectedCell;
        }
    }
}
