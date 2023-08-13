using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using GameInformationUtility;
using HandUtility;
using HoldemHand;
using InfoHelper.StatsEntities;
using InfoHelper.Utils;
using InfoHelper.ViewModel.DataEntities;
using InfoHelper.ViewModel.States;
using StatUtility;

namespace InfoHelper.DataProcessor
{
    public partial class HudsManager
    {
        private readonly ViewModelMain _vmMain;

        public HudsManager(ViewModelMain vmMain)
        {
            _vmMain = vmMain;
        }

        public void UpdateHuds(GameContext gc, StatSet[][] statSets)
        {
            //Pot odds
            if (gc.Error == string.Empty)
            {
                float[] playerBets = new float[6];

                BettingAction lastHeroActionCurrentRound = null;

                BettingAction lastOppAggrActionCurrentRound = null;

                float currentPot = 0;

                float oppBetToPotRatio = 0;

                foreach (BettingAction action in gc.Actions.Where(a => a.Round == gc.Round))
                {
                    if (gc.IsPlayerIn[action.Player - 1] != null && action.Amount > playerBets[action.Player - 1])
                    {
                        float amount = (float)action.Amount;

                        if (amount > playerBets[gc.HeroPosition - 1] + gc.Stacks[gc.HeroPosition - 1])
                            amount = (float)(playerBets[gc.HeroPosition - 1] + gc.Stacks[gc.HeroPosition - 1]);

                        float amountDelta = amount - playerBets[action.Player - 1];

                        if (action.Player == gc.HeroPosition)
                        {
                            if (action.ActionType is not (BettingActionType.PostSb or BettingActionType.PostBb))
                            {
                                lastHeroActionCurrentRound = action;

                                lastOppAggrActionCurrentRound = null;

                                oppBetToPotRatio = 0;
                            }
                        }
                        else
                        {
                            if (action.ActionType is BettingActionType.Bet or BettingActionType.Raise)
                            {
                                if (lastOppAggrActionCurrentRound != null)
                                {
                                    lastOppAggrActionCurrentRound = null;

                                    oppBetToPotRatio = 0;
                                }
                                else
                                {
                                    float maxBet = playerBets.Max();

                                    float amountToEqual = maxBet - playerBets[action.Player - 1];

                                    lastOppAggrActionCurrentRound = action;

                                    oppBetToPotRatio = (float)((amountDelta - amountToEqual) / (currentPot + gc.RoundPot + amountToEqual));
                                }
                            }
                        }

                        playerBets[action.Player - 1] = amount;

                        currentPot += amountDelta;
                    }
                }

                float potOdds = (float)(gc.AmountToCall / (playerBets.Sum() + gc.AmountToCall + gc.RoundPot));

                if (potOdds == 0)
                    _vmMain.ControlsState.PotOdds = null;
                else
                {
                    string potOddsStr = $"P/O: {Math.Round(potOdds * 100, 1).ToString(CultureInfo.InvariantCulture)}%";

                    if (lastOppAggrActionCurrentRound != null)
                    {
                        if (gc.Round == 1)
                        {
                            if (lastHeroActionCurrentRound == null)
                                potOddsStr += $" (Opp raise: {Math.Round(playerBets[lastOppAggrActionCurrentRound.Player - 1], 1).ToString(CultureInfo.InvariantCulture)} bb)";
                            else
                                potOddsStr += $" (Opp raise: {Math.Round(playerBets[lastOppAggrActionCurrentRound.Player - 1] / playerBets[lastHeroActionCurrentRound.Player - 1], 1).ToString(CultureInfo.InvariantCulture)}x)";
                        }
                        else
                        {
                            if(lastHeroActionCurrentRound  == null)
                                potOddsStr += $" (Opp bet: {Math.Round(oppBetToPotRatio * 100, 1).ToString(CultureInfo.InvariantCulture)}% p)";
                            else
                                potOddsStr += $" (Opp raise: {Math.Round(oppBetToPotRatio * 100, 1).ToString(CultureInfo.InvariantCulture)}% p)";
                        }
                    }

                    _vmMain.ControlsState.PotOdds = potOddsStr;
                }
            }
            else
                _vmMain.ControlsState.PotOdds = null;

            //Hand ev and type
            if (gc.Error == string.Empty && gc.Round > 1)
            {
                HeroHandInfo handInfo = (HeroHandInfo)gc.HeroHandData;

                _vmMain.ControlsState.Ev = $"Ev: {Math.Round(handInfo.Ev, 1).ToString(CultureInfo.InvariantCulture)}";
                _vmMain.ControlsState.HandType = handInfo.HandType;
            }
            else
            {
                _vmMain.ControlsState.Ev = null;
                _vmMain.ControlsState.HandType = HandType.None;
            }

            _vmMain.AnalyzerInfoState.Info = gc.Error;

            for (int i = 0; i < gc.Players.Length; i++)
            {
                if (i == gc.HeroPosition - 1)
                    continue;

                if (gc.InitialStacks[i] == null || gc.Players[i] == null)
                {
                    _vmMain.HudsParentStates[i].ActionsState.Visible = false;
                    _vmMain.HudsParentStates[i].NameState.Visible = false;

                    _vmMain.HudsParentStates[i].GeneralHudState.Visible = false;
                    _vmMain.HudsParentStates[i].AggressionHudState.Visible = false;
                    _vmMain.HudsParentStates[i].IpRaiser4PreflopHudState.Visible = false;
                    _vmMain.HudsParentStates[i].IpCaller4PreflopHudState.Visible = false;
                    _vmMain.HudsParentStates[i].OopRaiser4PreflopHudState.Visible = false;
                    _vmMain.HudsParentStates[i].OopCaller4PreflopHudState.Visible = false;
                    _vmMain.HudsParentStates[i].BtnPreflopHudState.Visible = false;
                    _vmMain.HudsParentStates[i].SbPreflopHudState.Visible = false;
                    _vmMain.HudsParentStates[i].BbPreflopHudState.Visible = false;
                    _vmMain.HudsParentStates[i].EpPreflopHudState.Visible = false;
                    _vmMain.HudsParentStates[i].MpPreflopHudState.Visible = false;
                    _vmMain.HudsParentStates[i].CoPreflopHudState.Visible = false;
                    _vmMain.HudsParentStates[i].SbvsBbPreflopHudState.Visible = false;
                    _vmMain.HudsParentStates[i].BbvsSbPreflopHudState.Visible = false;
                    _vmMain.HudsParentStates[i].PostflopHuIpRaiserHudState.Visible = false;
                    _vmMain.HudsParentStates[i].PostflopHuIpCallerHudState.Visible = false;
                    _vmMain.HudsParentStates[i].PostflopHuOopRaiserHudState.Visible = false;
                    _vmMain.HudsParentStates[i].PostflopHuOopCallerHudState.Visible = false;
                    _vmMain.HudsParentStates[i].PostflopGeneralHudState.Visible = false;

                    _vmMain.HudsParentStates[i].PreflopMatrixState.Visible = false;
                    _vmMain.HudsParentStates[i].PostflopHandsPanelState.Visible = false;

                    _vmMain.HudsParentStates[i].PreflopMatrixAltState.Visible = false;
                    _vmMain.HudsParentStates[i].PostflopHandsPanelAltState.Visible = false;

                    _vmMain.HudsParentStates[i].PostflopSizingTableStates[0].Visible = false;
                    _vmMain.HudsParentStates[i].PostflopSizingTableStates[1].Visible = false;
                    _vmMain.HudsParentStates[i].PostflopSizingTableStates[2].Visible = false;
                    _vmMain.HudsParentStates[i].PostflopSizingTableStates[3].Visible = false;
                    _vmMain.HudsParentStates[i].PostflopSizingTableStates[4].Visible = false;
                }
                else
                {
                    BettingAction[] playerActions = gc.Actions.Where(a => a.Player == i + 1 && a.ActionType != BettingActionType.PostSb && a.ActionType != BettingActionType.PostBb).ToArray();

                    //Name hud
                    _vmMain.HudsParentStates[i].NameState.Visible = true;

                    _vmMain.HudsParentStates[i].NameState.Name = gc.Players[i].Length > 18 ? $"{gc.Players[i].Substring(0, 18)}..." : gc.Players[i];

                    _vmMain.HudsParentStates[i].NameState.IsConfirmed = gc.IsPlayerConfirmed[i];

                    //Actions hud
                    _vmMain.HudsParentStates[i].ActionsState.Visible = true;

                    string actionsString = string.Empty;

                    if (gc.Error == string.Empty)
                    {
                        for (int j = 0; j < playerActions.Length; j++)
                        {
                            if (j > 0 && playerActions[j - 1].Round < playerActions[j].Round)
                                actionsString += "/";

                            if (playerActions[j].ActionType == BettingActionType.Fold)
                                actionsString += "f";
                            else if (playerActions[j].ActionType == BettingActionType.Check)
                                actionsString += "x";
                            else if (playerActions[j].ActionType == BettingActionType.Call)
                                actionsString += "c";
                            else if (playerActions[j].ActionType == BettingActionType.Bet)
                                actionsString += "b";
                            else if (playerActions[j].ActionType == BettingActionType.Raise)
                                actionsString += "r";
                        }
                    }

                    _vmMain.HudsParentStates[i].ActionsState.Actions = actionsString;

                    #region Retrieve Set variables

                    bool[] initialPlayers = gc.InitialStacks.Select(s => s != null).ToArray();

                    GameType gameType = gc.SmallBlindPosition == gc.ButtonPosition ? GameType.Hu : GameType.SixMax;

                    Round round = gc.Round < 2 ? Round.Preflop : Round.Postflop;

                    Position position = Common.GetPlayerPosition(new int[] { gc.SmallBlindPosition, gc.BigBlindPosition, gc.ButtonPosition }, i + 1, initialPlayers);

                    Position oppPosition = Position.Any;

                    RelativePosition relativePosition = RelativePosition.Any;

                    PlayersOnFlop playersOnFlop = PlayersOnFlop.Any;

                    PreflopPotType preflopPotType = PreflopPotType.Any;

                    PreflopActions preflActions = PreflopActions.Any;

                    OtherPlayersActed otherPlayersActed = OtherPlayersActed.Any;

                    void UpdateSetVariables(int rnd, BettingAction[] prflActions, bool?[] isPlayerIn)
                    {
                        oppPosition = Position.Any;

                        relativePosition = RelativePosition.Any;

                        playersOnFlop = PlayersOnFlop.Any;

                        preflopPotType = PreflopPotType.Any;

                        preflActions = PreflopActions.Any;

                        otherPlayersActed = OtherPlayersActed.Any;

                        if (rnd > 1)
                        {
                            if (isPlayerIn.Count(p => p != null && (bool)p) > 2)
                            {
                                playersOnFlop = PlayersOnFlop.Multiway;

                                otherPlayersActed = OtherPlayersActed.Yes;
                            }
                            else
                            {
                                int oppIndexPosition = isPlayerIn.Select((p, k) => new { Value = p, Index = k }).First(item => item.Value != null && (bool)item.Value && item.Index != i).Index;

                                otherPlayersActed = OtherPlayersActed.No;

                                foreach (BettingAction action in prflActions)
                                {
                                    if (action.Player == i + 1 || action.Player == oppIndexPosition + 1)
                                        continue;

                                    if (action.ActionType is BettingActionType.Check or BettingActionType.Call or BettingActionType.Raise)
                                    {
                                        otherPlayersActed = OtherPlayersActed.Yes;

                                        break;
                                    }
                                }

                                oppPosition = Common.GetPlayerPosition(new int[] { gc.SmallBlindPosition, gc.BigBlindPosition, gc.ButtonPosition }, oppIndexPosition + 1, initialPlayers);

                                if (gameType == GameType.Hu)
                                    relativePosition = gc.SmallBlindPosition == i + 1 ? RelativePosition.Ip : RelativePosition.Oop;
                                else
                                    relativePosition = position > oppPosition ? RelativePosition.Ip : RelativePosition.Oop;

                                playersOnFlop = PlayersOnFlop.Hu;
                            }

                            int callCount = 0;
                            int raiseCount = 0;

                            foreach (BettingAction action in prflActions)
                            {
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
                            }

                            BettingAction[] playerPreflopActions = prflActions.Where(a => a.Player == i + 1 && a.ActionType != BettingActionType.PostSb && a.ActionType != BettingActionType.PostBb).ToArray();

                            if(playerPreflopActions[^1].ActionType == BettingActionType.Fold)
                                preflActions = PreflopActions.Fold;
                            else
                            {
                                if (playerPreflopActions.Length == 1)
                                {
                                    if (playerPreflopActions[0].ActionType == BettingActionType.Check)
                                        preflActions = PreflopActions.Check;
                                    else if (playerPreflopActions[0].ActionType == BettingActionType.Call)
                                        preflActions = PreflopActions.Call;
                                    else if (playerPreflopActions[0].ActionType == BettingActionType.Raise)
                                        preflActions = PreflopActions.Raise;
                                }
                                else if (playerPreflopActions.Length == 2)
                                {
                                    if (playerPreflopActions[0].ActionType == BettingActionType.Call)
                                    {
                                        if (playerPreflopActions[1].ActionType == BettingActionType.Call)
                                            preflActions = PreflopActions.CallCall;
                                        else if (playerPreflopActions[1].ActionType == BettingActionType.Raise)
                                            preflActions = PreflopActions.CallRaise;
                                    }
                                    else if (playerPreflopActions[0].ActionType == BettingActionType.Raise)
                                    {
                                        if (playerPreflopActions[1].ActionType == BettingActionType.Call)
                                            preflActions = PreflopActions.RaiseCall;
                                        else if (playerPreflopActions[1].ActionType == BettingActionType.Raise)
                                            preflActions = PreflopActions.RaiseRaise;
                                    }
                                }
                                else if (playerPreflopActions.Length > 2)
                                {
                                    if (playerPreflopActions[^1].ActionType == BettingActionType.Call)
                                        preflActions = PreflopActions.AnyCall;
                                    else if (playerPreflopActions[^1].ActionType == BettingActionType.Raise)
                                        preflActions = PreflopActions.AnyRaise;
                                }
                            }
                        }
                    }

                    #endregion

                    BettingAction[] preflopActions = gc.Actions.Where(a => a.Round == 1).ToArray();

                    UpdateSetVariables(gc.Round, preflopActions, gc.IsPlayerIn);

                    StatSetManager statSetManager = new StatSetManager(statSets[i])
                    {
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

                    ViewModelStatsHud preflopHud = null;
                    ViewModelStatsHud postflopHud = null;

                    //General hud
                    StatSet generalSet = statSetManager.GetStatSet(SetType.General);

                    if (generalSet == null)
                        _vmMain.HudsParentStates[i].GeneralHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].GeneralHudState.Visible = true;

                        _vmMain.HudsParentStates[i].GeneralHudState.SetData(generalSet.Cells);

                        _vmMain.HudsParentStates[i].GeneralHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].GeneralHudState.SetName = $"{generalSet}";
                    }

                    BettingAction lastPlayerAction = playerActions.LastOrDefault();

                    //bool showHud = gc.Error == string.Empty && (lastPlayerAction == null || lastPlayerAction.ActionType != BettingActionType.Fold);
                    bool showHud = lastPlayerAction == null || lastPlayerAction.ActionType != BettingActionType.Fold;

                    //Aggression hud
                    StatSet aggressionSet = statSetManager.GetStatSet(SetType.AggFqPostflop);

                    if (aggressionSet == null || !showHud)
                        _vmMain.HudsParentStates[i].AggressionHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].AggressionHudState.Visible = true;

                        _vmMain.HudsParentStates[i].AggressionHudState.SetData(aggressionSet.Cells);

                        _vmMain.HudsParentStates[i].AggressionHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].AggressionHudState.SetName = $"{aggressionSet}";
                    }

                    BettingAction lastPreflopRaiserAction = preflopActions.LastOrDefault(a => a.ActionType == BettingActionType.Raise);

                    StatSet ipRaiserPostflop4PreflopSet = null;
                    StatSet oopRaiserPostflop4PreflopSet = null;

                    if (lastPreflopRaiserAction != null)
                    {
                        if (!lastPreflopRaiserAction.IsAllIn)
                        {
                            List<BettingAction> preflopActionsTemp = preflopActions.ToList();

                            preflopActionsTemp.Add(new BettingAction(gc.HeroPosition, 1, 0, BettingActionType.Call, false));

                            bool?[] isPlayerInTemp = gc.IsPlayerIn.ToArray();

                            int nextPlayer = gc.HeroPosition + 1;

                            while (true)
                            {
                                if (nextPlayer == 7)
                                    nextPlayer = 1;

                                if (nextPlayer == lastPreflopRaiserAction.Player)
                                    break;

                                if (isPlayerInTemp[nextPlayer - 1] != null && (bool)isPlayerInTemp[nextPlayer - 1])
                                {
                                    preflopActionsTemp.Add(new BettingAction(nextPlayer, 1, 0, BettingActionType.Fold, false));

                                    isPlayerInTemp[nextPlayer - 1] = false;
                                }

                                nextPlayer++;
                            }

                            UpdateSetVariables(2, preflopActionsTemp.ToArray(), isPlayerInTemp);

                            StatSetManager statSetManagerAsHeroCaller = new StatSetManager(statSets[i])
                            {
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

                            ipRaiserPostflop4PreflopSet = statSetManagerAsHeroCaller.GetStatSet(SetType.IpRaiser4Preflop);
                            oopRaiserPostflop4PreflopSet = statSetManagerAsHeroCaller.GetStatSet(SetType.OopRaiser4Preflop);
                        }
                    }

                    //Ip raiser postflop 4 preflop hud
                    if (ipRaiserPostflop4PreflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].IpRaiser4PreflopHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].IpRaiser4PreflopHudState.Visible = true;

                        _vmMain.HudsParentStates[i].IpRaiser4PreflopHudState.SetData(ipRaiserPostflop4PreflopSet.Cells);

                        _vmMain.HudsParentStates[i].IpRaiser4PreflopHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].IpRaiser4PreflopHudState.SetName = $"{ipRaiserPostflop4PreflopSet}";

                        _vmMain.HudsParentStates[i].IpRaiser4PreflopHudState.SetType = SetType.IpRaiser4Preflop;
                    }

                    //Oop raiser postflop 4 preflop hud
                    if (oopRaiserPostflop4PreflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].OopRaiser4PreflopHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].OopRaiser4PreflopHudState.Visible = true;

                        _vmMain.HudsParentStates[i].OopRaiser4PreflopHudState.SetData(oopRaiserPostflop4PreflopSet.Cells);

                        _vmMain.HudsParentStates[i].OopRaiser4PreflopHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].OopRaiser4PreflopHudState.SetName = $"{oopRaiserPostflop4PreflopSet}";

                        _vmMain.HudsParentStates[i].OopRaiser4PreflopHudState.SetType = SetType.OopRaiser4Preflop;
                    }

                    StatSet ipCallerPostflop4PreflopSet = null;
                    StatSet oopCallerPostflop4PreflopSet = null;

                    bool unopenedPot = preflopActions.All(a => a.ActionType is BettingActionType.PostSb or BettingActionType.PostBb or BettingActionType.Fold);

                    if (lastPreflopRaiserAction != null || unopenedPot)
                    {
                        if(!(lastPreflopRaiserAction?.IsAllIn ?? false))
                        {
                            List<BettingAction> preflopActionTemp = preflopActions.ToList();

                            preflopActionTemp.Add(new BettingAction(gc.HeroPosition, 1, 0, BettingActionType.Raise, false));

                            bool?[] isPlayerInTemp = gc.IsPlayerIn.ToArray();

                            int nextPlayer = gc.HeroPosition + 1;

                            while (true)
                            {
                                if (nextPlayer == 7)
                                    nextPlayer = 1;

                                if (nextPlayer == gc.HeroPosition)
                                    break;

                                if (isPlayerInTemp[nextPlayer - 1] != null && (bool)isPlayerInTemp[nextPlayer - 1])
                                {
                                    if ((lastPreflopRaiserAction != null && nextPlayer == lastPreflopRaiserAction.Player) || unopenedPot)
                                        preflopActionTemp.Add(new BettingAction(nextPlayer, 1, 0, BettingActionType.Call, false));
                                    else
                                    {
                                        preflopActionTemp.Add(new BettingAction(nextPlayer, 1, 0, BettingActionType.Fold, false));

                                        isPlayerInTemp[nextPlayer - 1] = false;
                                    }
                                }

                                nextPlayer++;
                            }

                            UpdateSetVariables(2, preflopActionTemp.ToArray(), isPlayerInTemp);

                            StatSetManager statSetManagerAsHeroRaiser = new StatSetManager(statSets[i])
                            {
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

                            ipCallerPostflop4PreflopSet = statSetManagerAsHeroRaiser.GetStatSet(SetType.IpCaller4Preflop);
                            oopCallerPostflop4PreflopSet = statSetManagerAsHeroRaiser.GetStatSet(SetType.OopCaller4Preflop);
                        }
                    }

                    //Ip caller postflop 4 preflop hud
                    if (ipCallerPostflop4PreflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].IpCaller4PreflopHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].IpCaller4PreflopHudState.Visible = true;

                        _vmMain.HudsParentStates[i].IpCaller4PreflopHudState.SetData(ipCallerPostflop4PreflopSet.Cells);

                        _vmMain.HudsParentStates[i].IpCaller4PreflopHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].IpCaller4PreflopHudState.SetName = $"{ipCallerPostflop4PreflopSet}";

                        _vmMain.HudsParentStates[i].IpCaller4PreflopHudState.SetType = SetType.IpCaller4Preflop;
                    }

                    //Oop caller postflop 4 preflop hud
                    if (oopCallerPostflop4PreflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].OopCaller4PreflopHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].OopCaller4PreflopHudState.Visible = true;

                        _vmMain.HudsParentStates[i].OopCaller4PreflopHudState.SetData(oopCallerPostflop4PreflopSet.Cells);

                        _vmMain.HudsParentStates[i].OopCaller4PreflopHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].OopCaller4PreflopHudState.SetName = $"{oopCallerPostflop4PreflopSet}";

                        _vmMain.HudsParentStates[i].OopCaller4PreflopHudState.SetType = SetType.OopCaller4Preflop;
                    }

                    //Btn preflop hud
                    StatSet btnPreflopSet = statSetManager.GetStatSet(SetType.PreflopBtn);

                    if (btnPreflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].BtnPreflopHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].BtnPreflopHudState.Visible = true;

                        _vmMain.HudsParentStates[i].BtnPreflopHudState.SetData(btnPreflopSet.Cells);

                        _vmMain.HudsParentStates[i].BtnPreflopHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].BtnPreflopHudState.SetName = $"{btnPreflopSet}";

                        _vmMain.HudsParentStates[i].BtnPreflopHudState.SetType = SetType.PreflopBtn;

                        preflopHud = _vmMain.HudsParentStates[i].BtnPreflopHudState;
                    }

                    //Sb preflop hud
                    StatSet sbPreflopSet = statSetManager.GetStatSet(SetType.PreflopSb);

                    if (sbPreflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].SbPreflopHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].SbPreflopHudState.Visible = true;

                        _vmMain.HudsParentStates[i].SbPreflopHudState.SetData(sbPreflopSet.Cells);

                        _vmMain.HudsParentStates[i].SbPreflopHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].SbPreflopHudState.SetName = $"{sbPreflopSet}";

                        _vmMain.HudsParentStates[i].SbPreflopHudState.SetType = SetType.PreflopSb;

                        preflopHud = _vmMain.HudsParentStates[i].SbPreflopHudState;
                    }

                    //Bb preflop hud
                    StatSet bbPreflopSet = statSetManager.GetStatSet(SetType.PreflopBb);

                    if (bbPreflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].BbPreflopHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].BbPreflopHudState.Visible = true;

                        _vmMain.HudsParentStates[i].BbPreflopHudState.SetData(bbPreflopSet.Cells);

                        _vmMain.HudsParentStates[i].BbPreflopHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].BbPreflopHudState.SetName = $"{bbPreflopSet}";

                        _vmMain.HudsParentStates[i].BbPreflopHudState.SetType = SetType.PreflopBb;

                        preflopHud = _vmMain.HudsParentStates[i].BbPreflopHudState;
                    }

                    //Ep preflop hud
                    StatSet epPreflopSet = statSetManager.GetStatSet(SetType.PreflopEp);

                    if (epPreflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].EpPreflopHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].EpPreflopHudState.Visible = true;

                        _vmMain.HudsParentStates[i].EpPreflopHudState.SetData(epPreflopSet.Cells);

                        _vmMain.HudsParentStates[i].EpPreflopHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].EpPreflopHudState.SetName = $"{epPreflopSet}";

                        _vmMain.HudsParentStates[i].EpPreflopHudState.SetType = SetType.PreflopEp;

                        preflopHud = _vmMain.HudsParentStates[i].EpPreflopHudState;
                    }

                    //Mp preflop hud
                    StatSet mpPreflopSet = statSetManager.GetStatSet(SetType.PreflopMp);

                    if (mpPreflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].MpPreflopHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].MpPreflopHudState.Visible = true;

                        _vmMain.HudsParentStates[i].MpPreflopHudState.SetData(mpPreflopSet.Cells);

                        _vmMain.HudsParentStates[i].MpPreflopHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].MpPreflopHudState.SetName = $"{mpPreflopSet}";

                        _vmMain.HudsParentStates[i].MpPreflopHudState.SetType = SetType.PreflopMp;

                        preflopHud = _vmMain.HudsParentStates[i].MpPreflopHudState;
                    }

                    //Co preflop hud
                    StatSet coPreflopSet = statSetManager.GetStatSet(SetType.PreflopCo);

                    if (coPreflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].CoPreflopHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].CoPreflopHudState.Visible = true;

                        _vmMain.HudsParentStates[i].CoPreflopHudState.SetData(coPreflopSet.Cells);

                        _vmMain.HudsParentStates[i].CoPreflopHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].CoPreflopHudState.SetName = $"{coPreflopSet}";

                        _vmMain.HudsParentStates[i].CoPreflopHudState.SetType = SetType.PreflopCo;

                        preflopHud = _vmMain.HudsParentStates[i].CoPreflopHudState;
                    }

                    //Sb vs Bb preflop hud
                    StatSet sbvsbbPreflopSet = statSetManager.GetStatSet(SetType.PreflopSbvsBb);

                    if (sbvsbbPreflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].SbvsBbPreflopHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].SbvsBbPreflopHudState.Visible = true;

                        _vmMain.HudsParentStates[i].SbvsBbPreflopHudState.SetData(sbvsbbPreflopSet.Cells);

                        _vmMain.HudsParentStates[i].SbvsBbPreflopHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].SbvsBbPreflopHudState.SetName = $"{sbvsbbPreflopSet}";

                        _vmMain.HudsParentStates[i].SbvsBbPreflopHudState.SetType = SetType.PreflopSbvsBb;

                        preflopHud = _vmMain.HudsParentStates[i].SbvsBbPreflopHudState;
                    }

                    //Bb vs Sb preflop hud
                    StatSet bbvssbPreflopSet = statSetManager.GetStatSet(SetType.PreflopBbvsSb);

                    if (bbvssbPreflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].BbvsSbPreflopHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].BbvsSbPreflopHudState.Visible = true;

                        _vmMain.HudsParentStates[i].BbvsSbPreflopHudState.SetData(bbvssbPreflopSet.Cells);

                        _vmMain.HudsParentStates[i].BbvsSbPreflopHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].BbvsSbPreflopHudState.SetName = $"{bbvssbPreflopSet}";

                        _vmMain.HudsParentStates[i].BbvsSbPreflopHudState.SetType = SetType.PreflopBbvsSb;

                        preflopHud = _vmMain.HudsParentStates[i].BbvsSbPreflopHudState;
                    }

                    //Postflop Hu Ip as raiser hud
                    StatSet huIpRaiserPostflopSet = statSetManager.GetStatSet(SetType.PostflopHuIpRaiser);

                    if (huIpRaiserPostflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].PostflopHuIpRaiserHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].PostflopHuIpRaiserHudState.Visible = true;

                        _vmMain.HudsParentStates[i].PostflopHuIpRaiserHudState.SetData(huIpRaiserPostflopSet.Cells);

                        _vmMain.HudsParentStates[i].PostflopHuIpRaiserHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].PostflopHuIpRaiserHudState.SetName = $"{huIpRaiserPostflopSet}";

                        _vmMain.HudsParentStates[i].PostflopHuIpRaiserHudState.SetType = SetType.PostflopHuIpRaiser;

                        postflopHud = _vmMain.HudsParentStates[i].PostflopHuIpRaiserHudState;
                    }

                    //Postflop Hu Ip as caller hud
                    StatSet huIpCallerPostflopSet = statSetManager.GetStatSet(SetType.PostflopHuIpCaller);

                    if (postflopHud != null || huIpCallerPostflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].PostflopHuIpCallerHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].PostflopHuIpCallerHudState.Visible = true;

                        _vmMain.HudsParentStates[i].PostflopHuIpCallerHudState.SetData(huIpCallerPostflopSet.Cells);

                        _vmMain.HudsParentStates[i].PostflopHuIpCallerHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].PostflopHuIpCallerHudState.SetName = $"{huIpCallerPostflopSet}";

                        _vmMain.HudsParentStates[i].PostflopHuIpCallerHudState.SetType = SetType.PostflopHuIpCaller;

                        postflopHud = _vmMain.HudsParentStates[i].PostflopHuIpCallerHudState;
                    }

                    //Postflop Hu Oop as raiser hud
                    StatSet huOopRaiserPostflopSet = statSetManager.GetStatSet(SetType.PostflopHuOopRaiser);

                    if (postflopHud != null || huOopRaiserPostflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].PostflopHuOopRaiserHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].PostflopHuOopRaiserHudState.Visible = true;

                        _vmMain.HudsParentStates[i].PostflopHuOopRaiserHudState.SetData(huOopRaiserPostflopSet.Cells);

                        _vmMain.HudsParentStates[i].PostflopHuOopRaiserHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].PostflopHuOopRaiserHudState.SetName = $"{huOopRaiserPostflopSet}";

                        _vmMain.HudsParentStates[i].PostflopHuOopRaiserHudState.SetType = SetType.PostflopHuOopRaiser;

                        postflopHud = _vmMain.HudsParentStates[i].PostflopHuOopRaiserHudState;
                    }

                    //Postflop Hu Oop as caller hud
                    StatSet huOopCallerPostflopSet = statSetManager.GetStatSet(SetType.PostflopHuOopCaller);

                    if (postflopHud != null || huOopCallerPostflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].PostflopHuOopCallerHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].PostflopHuOopCallerHudState.Visible = true;

                        _vmMain.HudsParentStates[i].PostflopHuOopCallerHudState.SetData(huOopCallerPostflopSet.Cells);

                        _vmMain.HudsParentStates[i].PostflopHuOopCallerHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].PostflopHuOopCallerHudState.SetName = $"{huOopCallerPostflopSet}";

                        _vmMain.HudsParentStates[i].PostflopHuOopCallerHudState.SetType = SetType.PostflopHuOopCaller;

                        postflopHud = _vmMain.HudsParentStates[i].PostflopHuOopCallerHudState;
                    }

                    //Postflop general hud
                    StatSet generalPostflopSet = statSetManager.GetStatSet(SetType.PostflopGeneral);

                    if (postflopHud != null || generalPostflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].PostflopGeneralHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].PostflopGeneralHudState.Visible = true;

                        _vmMain.HudsParentStates[i].PostflopGeneralHudState.SetData(generalPostflopSet.Cells);

                        _vmMain.HudsParentStates[i].PostflopGeneralHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].PostflopGeneralHudState.SetName = $"{generalPostflopSet}";

                        _vmMain.HudsParentStates[i].PostflopGeneralHudState.SetType = SetType.PostflopGeneral;

                        postflopHud = _vmMain.HudsParentStates[i].PostflopGeneralHudState;
                    }

                    DataCell selectedCell = ProcessSelectedHuds(gc, preflopHud, postflopHud);

                    DataCell missedCell = null;

                    if (selectedCell == null)
                    {
                        _vmMain.HudsParentStates[i].PreflopMatrixState.Visible = false;
                        _vmMain.HudsParentStates[i].PostflopHandsPanelState.Visible = false;
                    }
                    else
                    {
                        if (selectedCell.CellData == null)
                            throw new Exception($"Cell {selectedCell.Name} doesn't contain data");

                        if (selectedCell.CellData is PreflopData preflopData)
                        {
                            _vmMain.HudsParentStates[i].PreflopMatrixState.Visible = true;
                            _vmMain.HudsParentStates[i].PostflopHandsPanelState.Visible = false;

                            _vmMain.HudsParentStates[i].PreflopMatrixState.PreflopHandsGroup = (PreflopHandsGroup)preflopData.MainGroup;
                            _vmMain.HudsParentStates[i].PreflopMatrixState.Header = selectedCell.Description;
                        }
                        else if(selectedCell.CellData is PostflopData postflopData)
                        {
                            _vmMain.HudsParentStates[i].PreflopMatrixState.Visible = false;
                            _vmMain.HudsParentStates[i].PostflopHandsPanelState.Visible = true;

                            _vmMain.HudsParentStates[i].PostflopHandsPanelState.Round = selectedCell.Round;
                            _vmMain.HudsParentStates[i].PostflopHandsPanelState.ShowGroupHeader = true;
                            _vmMain.HudsParentStates[i].PostflopHandsPanelState.PostflopHandsGroup = (PostflopHandsGroup)postflopData.MainGroup;
                            _vmMain.HudsParentStates[i].PostflopHandsPanelState.Header = selectedCell.Description;
                        }

                        selectedCell.CellSelectedState = CellSelectedState.Selected;

                        if (selectedCell.ConnectedCells != null)
                        {
                            if (selectedCell.ConnectedCells.Length > 0)
                            {
                                missedCell = selectedCell.ConnectedCells[^1];

                                foreach (DataCell mc in selectedCell.ConnectedCells)
                                    mc.CellSelectedState = CellSelectedState.Missed;
                            }
                        }
                    }

                    if (selectedCell == null || selectedCell.Round == 1)
                    {
                        _vmMain.HudsParentStates[i].PostflopSizingTableStates[0].Visible = false;
                        _vmMain.HudsParentStates[i].PostflopSizingTableStates[1].Visible = false;
                        _vmMain.HudsParentStates[i].PostflopSizingTableStates[2].Visible = false;
                        _vmMain.HudsParentStates[i].PostflopSizingTableStates[3].Visible = false;
                        _vmMain.HudsParentStates[i].PostflopSizingTableStates[4].Visible = false;
                    }
                    else
                    {
                        if(selectedCell.CellData is not PostflopData postflopData)
                            throw new Exception($"Cell {selectedCell.Name} has wrong data format");

                        (int sampleMg, float evMg, float evDeltaMg) = GetPostflopSizingData((PostflopHandsGroup)postflopData.MainGroup);

                        _vmMain.HudsParentStates[i].PostflopSizingTableStates[0].Visible = true;
                        _vmMain.HudsParentStates[i].PostflopSizingTableStates[0].LowBound = float.MinValue;
                        _vmMain.HudsParentStates[i].PostflopSizingTableStates[0].UpperBound = float.MaxValue;
                        _vmMain.HudsParentStates[i].PostflopSizingTableStates[0].Sample = sampleMg;
                        _vmMain.HudsParentStates[i].PostflopSizingTableStates[0].Ev = evMg;
                        _vmMain.HudsParentStates[i].PostflopSizingTableStates[0].EvDelta = evDeltaMg;

                        if (selectedCell.BetRanges == null)
                        {
                            _vmMain.HudsParentStates[i].PostflopSizingTableStates[1].Visible = false;
                            _vmMain.HudsParentStates[i].PostflopSizingTableStates[2].Visible = false;
                            _vmMain.HudsParentStates[i].PostflopSizingTableStates[3].Visible = false;
                            _vmMain.HudsParentStates[i].PostflopSizingTableStates[4].Visible = false;
                        }
                        else
                        {
                            _vmMain.HudsParentStates[i].PostflopSizingTableStates[1].Visible = selectedCell.BetRanges.Length > 0;
                            _vmMain.HudsParentStates[i].PostflopSizingTableStates[2].Visible = selectedCell.BetRanges.Length > 1;
                            _vmMain.HudsParentStates[i].PostflopSizingTableStates[3].Visible = selectedCell.BetRanges.Length > 2;
                            _vmMain.HudsParentStates[i].PostflopSizingTableStates[4].Visible = selectedCell.BetRanges.Length > 3;

                            HandsGroupBase[] subGroups = postflopData.SubGroups;

                            for (int j = 0; j < selectedCell.BetRanges.Length; j++)
                            {
                                _vmMain.HudsParentStates[i].PostflopSizingTableStates[j + 1].LowBound = selectedCell.BetRanges[j].LowBound;
                                _vmMain.HudsParentStates[i].PostflopSizingTableStates[j + 1].UpperBound = selectedCell.BetRanges[j].UpperBound;

                                (int sampleSg, float evSg, float evDeltaSg) = GetPostflopSizingData((PostflopHandsGroup)subGroups[j]);

                                _vmMain.HudsParentStates[i].PostflopSizingTableStates[j + 1].Sample = sampleSg;
                                _vmMain.HudsParentStates[i].PostflopSizingTableStates[j + 1].Ev = evSg;
                                _vmMain.HudsParentStates[i].PostflopSizingTableStates[j + 1].EvDelta = evDeltaSg;
                            }
                        }

                        (int, float, float) GetPostflopSizingData(PostflopHandsGroup phg)
                        {
                            int sample = 0;

                            float ev = 0, evDelta = float.NaN;

                            sample = phg.Hands.Select(v => (int)v).Sum();

                            if (sample > 0)
                            {
                                ev = (float)phg.AccumulatedEv / sample;

                                if (ev > 0)
                                {
                                    float value = float.NaN;

                                    if (!float.IsNaN(phg.GtoValue))
                                        value = phg.GtoValue;
                                    else if (!float.IsNaN(phg.DefaultValue))
                                        value = phg.DefaultValue;

                                    if (!float.IsNaN(value))
                                        evDelta = ev - value;
                                }
                            }

                            return (sample, ev, evDelta);
                        }
                    }

                    if (missedCell == null || Regex.IsMatch(missedCell.Name, @"Fold|FCB|Fv|_F_F|_F_T|_F_R"))
                    {
                        _vmMain.HudsParentStates[i].PreflopMatrixAltState.Visible = false;
                        _vmMain.HudsParentStates[i].PostflopHandsPanelAltState.Visible = false;
                    }
                    else
                    {
                        if (missedCell.CellData == null)
                            throw new Exception($"Cell {missedCell.Name} doesn't contain data");

                        if (missedCell.CellData is PreflopData preflopData)
                        {
                            _vmMain.HudsParentStates[i].PreflopMatrixAltState.Visible = true;
                            _vmMain.HudsParentStates[i].PostflopHandsPanelAltState.Visible = false;

                            _vmMain.HudsParentStates[i].PreflopMatrixAltState.PreflopHandsGroup = (PreflopHandsGroup)preflopData.MainGroup;
                            _vmMain.HudsParentStates[i].PreflopMatrixAltState.Header = missedCell.Description;
                        }
                        else if (missedCell.CellData is PostflopData postflopData)
                        {
                            _vmMain.HudsParentStates[i].PreflopMatrixAltState.Visible = false;
                            _vmMain.HudsParentStates[i].PostflopHandsPanelAltState.Visible = true;

                            _vmMain.HudsParentStates[i].PostflopHandsPanelAltState.Round = missedCell.Round;
                            _vmMain.HudsParentStates[i].PostflopHandsPanelAltState.ShowGroupHeader = true;
                            _vmMain.HudsParentStates[i].PostflopHandsPanelAltState.PostflopHandsGroup = (PostflopHandsGroup)postflopData.MainGroup;
                            _vmMain.HudsParentStates[i].PostflopHandsPanelAltState.Header = missedCell.Description;
                        }
                    }

                    ViewModelStatsHud.SelectedCell = selectedCell?.Name;
                }

                _vmMain.HudsParentStates[i].NameState.UpdateBindings();
                _vmMain.HudsParentStates[i].ActionsState.UpdateBindings();

                _vmMain.HudsParentStates[i].GeneralHudState.UpdateBindings();
                _vmMain.HudsParentStates[i].AggressionHudState.UpdateBindings();
                _vmMain.HudsParentStates[i].IpRaiser4PreflopHudState.UpdateBindings();
                _vmMain.HudsParentStates[i].IpCaller4PreflopHudState.UpdateBindings();
                _vmMain.HudsParentStates[i].OopRaiser4PreflopHudState.UpdateBindings();
                _vmMain.HudsParentStates[i].OopCaller4PreflopHudState.UpdateBindings();
                _vmMain.HudsParentStates[i].BtnPreflopHudState.UpdateBindings();
                _vmMain.HudsParentStates[i].SbPreflopHudState.UpdateBindings();
                _vmMain.HudsParentStates[i].BbPreflopHudState.UpdateBindings();
                _vmMain.HudsParentStates[i].EpPreflopHudState.UpdateBindings();
                _vmMain.HudsParentStates[i].MpPreflopHudState.UpdateBindings();
                _vmMain.HudsParentStates[i].CoPreflopHudState.UpdateBindings();
                _vmMain.HudsParentStates[i].SbvsBbPreflopHudState.UpdateBindings();
                _vmMain.HudsParentStates[i].BbvsSbPreflopHudState.UpdateBindings();
                _vmMain.HudsParentStates[i].PostflopHuIpRaiserHudState.UpdateBindings();
                _vmMain.HudsParentStates[i].PostflopHuIpCallerHudState.UpdateBindings();
                _vmMain.HudsParentStates[i].PostflopHuOopRaiserHudState.UpdateBindings();
                _vmMain.HudsParentStates[i].PostflopHuOopCallerHudState.UpdateBindings();
                _vmMain.HudsParentStates[i].PostflopGeneralHudState.UpdateBindings();

                _vmMain.HudsParentStates[i].PreflopMatrixState.UpdateBindings();
                _vmMain.HudsParentStates[i].PreflopMatrixAltState.UpdateBindings();

                _vmMain.HudsParentStates[i].PostflopHandsPanelState.UpdateBindings();
                _vmMain.HudsParentStates[i].PostflopHandsPanelAltState.UpdateBindings();

                _vmMain.HudsParentStates[i].PostflopSizingTableStates[0].UpdateBindings();
                _vmMain.HudsParentStates[i].PostflopSizingTableStates[1].UpdateBindings();
                _vmMain.HudsParentStates[i].PostflopSizingTableStates[2].UpdateBindings();
                _vmMain.HudsParentStates[i].PostflopSizingTableStates[3].UpdateBindings();
                _vmMain.HudsParentStates[i].PostflopSizingTableStates[4].UpdateBindings();
            }

            _vmMain.GtoParentState.Error = gc.Error != string.Empty ? null : gc.GtoError;

            _vmMain.GtoParentState.IsSolverRunning = gc.Error == string.Empty && gc.IsSolving;

            _vmMain.GtoParentState.GtoState.Visible = gc.Error == string.Empty && gc.GtoError == null && !gc.IsSolving;

            _vmMain.GtoParentState.GtoState.GtoInfo = _vmMain.GtoParentState.GtoState.Visible ? (GtoInfo)gc.GtoData : null;

            _vmMain.GtoParentState.GtoState.UpdateBindings();
        }

        public void UpdateWindows(WindowInfo[] winInfos)
        {
            _vmMain.WindowsInfoState.WinInfos = winInfos;

            _vmMain.WindowsInfoState.UpdateBindings();
        }

        public void ResetControls()
        {
            _vmMain.ControlsState.PotOdds = null;

            _vmMain.ControlsState.Ev = null;
            _vmMain.ControlsState.HandType = HandType.None;

            _vmMain.AnalyzerInfoState.Info = null;

            foreach (ViewModelHudsParent vmHudsParent in _vmMain.HudsParentStates)
            {
                vmHudsParent.NameState.Visible = false;
                vmHudsParent.ActionsState.Visible = false;

                vmHudsParent.GeneralHudState.Visible = false;
                vmHudsParent.AggressionHudState.Visible = false;
                vmHudsParent.IpRaiser4PreflopHudState.Visible = false;
                vmHudsParent.IpCaller4PreflopHudState.Visible = false;
                vmHudsParent.OopRaiser4PreflopHudState.Visible = false;
                vmHudsParent.OopCaller4PreflopHudState.Visible = false;
                vmHudsParent.BtnPreflopHudState.Visible = false;
                vmHudsParent.SbPreflopHudState.Visible = false;
                vmHudsParent.BbPreflopHudState.Visible = false;
                vmHudsParent.EpPreflopHudState.Visible = false;
                vmHudsParent.MpPreflopHudState.Visible = false;
                vmHudsParent.CoPreflopHudState.Visible = false;
                vmHudsParent.SbvsBbPreflopHudState.Visible = false;
                vmHudsParent.BbvsSbPreflopHudState.Visible = false;
                vmHudsParent.PostflopHuIpRaiserHudState.Visible = false;
                vmHudsParent.PostflopHuIpCallerHudState.Visible = false;
                vmHudsParent.PostflopHuOopRaiserHudState.Visible = false;
                vmHudsParent.PostflopHuOopCallerHudState.Visible = false;
                vmHudsParent.PostflopGeneralHudState.Visible = false;

                vmHudsParent.PreflopMatrixState.Visible = false;
                vmHudsParent.PostflopHandsPanelState.Visible = false;

                vmHudsParent.PreflopMatrixAltState.Visible = false;
                vmHudsParent.PostflopHandsPanelAltState.Visible = false;

                vmHudsParent.PostflopSizingTableStates[0].Visible = false;
                vmHudsParent.PostflopSizingTableStates[1].Visible = false;
                vmHudsParent.PostflopSizingTableStates[2].Visible = false;
                vmHudsParent.PostflopSizingTableStates[3].Visible = false;
                vmHudsParent.PostflopSizingTableStates[4].Visible = false;

                vmHudsParent.NameState.UpdateBindings();
                vmHudsParent.ActionsState.UpdateBindings();

                vmHudsParent.GeneralHudState.UpdateBindings();
                vmHudsParent.AggressionHudState.UpdateBindings();
                vmHudsParent.IpRaiser4PreflopHudState.UpdateBindings();
                vmHudsParent.IpCaller4PreflopHudState.UpdateBindings();
                vmHudsParent.OopRaiser4PreflopHudState.UpdateBindings();
                vmHudsParent.OopCaller4PreflopHudState.UpdateBindings();
                vmHudsParent.BtnPreflopHudState.UpdateBindings();
                vmHudsParent.SbPreflopHudState.UpdateBindings();
                vmHudsParent.BbPreflopHudState.UpdateBindings();
                vmHudsParent.EpPreflopHudState.UpdateBindings();
                vmHudsParent.MpPreflopHudState.UpdateBindings();
                vmHudsParent.CoPreflopHudState.UpdateBindings();
                vmHudsParent.SbvsBbPreflopHudState.UpdateBindings();
                vmHudsParent.BbvsSbPreflopHudState.UpdateBindings();
                vmHudsParent.PostflopHuIpRaiserHudState.UpdateBindings();
                vmHudsParent.PostflopHuIpCallerHudState.UpdateBindings();
                vmHudsParent.PostflopHuOopRaiserHudState.UpdateBindings();
                vmHudsParent.PostflopHuOopCallerHudState.UpdateBindings();
                vmHudsParent.PostflopGeneralHudState.UpdateBindings();

                vmHudsParent.PreflopMatrixState.UpdateBindings();
                vmHudsParent.PreflopMatrixAltState.UpdateBindings();

                vmHudsParent.PostflopHandsPanelState.UpdateBindings();
                vmHudsParent.PostflopHandsPanelAltState.UpdateBindings();

                vmHudsParent.PostflopSizingTableStates[0].UpdateBindings();
                vmHudsParent.PostflopSizingTableStates[1].UpdateBindings();
                vmHudsParent.PostflopSizingTableStates[2].UpdateBindings();
                vmHudsParent.PostflopSizingTableStates[3].UpdateBindings();
                vmHudsParent.PostflopSizingTableStates[4].UpdateBindings();
            }

            _vmMain.GtoParentState.Error = string.Empty;

            _vmMain.GtoParentState.IsSolverRunning = false;

            _vmMain.GtoParentState.GtoState.Visible = false;

            _vmMain.GtoParentState.GtoState.UpdateBindings();
        }

        public void ResetWindowsPanel()
        {
            _vmMain.WindowsInfoState.WinInfos = null;

            _vmMain.WindowsInfoState.UpdateBindings();
        }

        private partial DataCell ProcessSelectedHuds(GameContext gc, ViewModelStatsHud preflopHud, ViewModelStatsHud postflopHud);
    }
}
