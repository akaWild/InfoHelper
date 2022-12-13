using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
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

                foreach (BettingAction action in gc.Actions.Where(a => a.Round == gc.Round))
                {
                    if (gc.IsPlayerIn[action.Player - 1] != null && action.Amount > playerBets[action.Player - 1])
                        playerBets[action.Player - 1] = (float)action.Amount;
                }

                for (int i = 0; i < playerBets.Length; i++)
                {
                    if (playerBets[i] > playerBets[gc.HeroPosition - 1] + gc.Stacks[gc.HeroPosition - 1])
                        playerBets[i] = (float)(playerBets[gc.HeroPosition - 1] + gc.Stacks[gc.HeroPosition - 1]);
                }

                float potOdds = (float)(gc.AmountToCall / (playerBets.Sum() + gc.AmountToCall + gc.RoundPot));

                _vmMain.ControlsState.PotOdds = potOdds == 0 ? null : $"P/O: {Math.Round(potOdds * 100, 1).ToString(CultureInfo.InvariantCulture)}%";
            }
            else
                _vmMain.ControlsState.PotOdds = null;

            //Hand equity and type
            if (gc.Error == string.Empty && gc.Round > 1)
            {
                HeroHandInfo handInfo = (HeroHandInfo)gc.HeroHandData;

                _vmMain.ControlsState.Equity = $"Eq: {Math.Round(handInfo.Equity, 1).ToString(CultureInfo.InvariantCulture)}%";
                _vmMain.ControlsState.HandType = handInfo.HandType;
            }
            else
            {
                _vmMain.ControlsState.Equity = null;
                _vmMain.ControlsState.HandType = HandType.None;
            }

            _vmMain.AnalyzerInfoState.Info = gc.Error;

            for (int i = 0; i < gc.Players.Length; i++)
            {
                if (gc.InitialStacks[i] == null || gc.Players[i] == null)
                {
                    _vmMain.HudsParentStates[i].ActionsState.Visible = false;
                    _vmMain.HudsParentStates[i].NameState.Visible = false;

                    _vmMain.HudsParentStates[i].GeneralHudState.Visible = false;
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

                    bool[] initialPlayers = gc.Stacks.Select(s => s != null).ToArray();

                    GameType gameType = gc.SmallBlindPosition == gc.ButtonPosition ? GameType.Hu : GameType.SixMax;

                    Round round = gc.Round < 2 ? Round.Preflop : Round.Postflop;

                    Position position = Common.GetPlayerPosition(new int[] { gc.SmallBlindPosition, gc.BigBlindPosition, gc.ButtonPosition }, i + 1, initialPlayers);

                    Position oppPosition = Position.Any;

                    RelativePosition relativePosition = RelativePosition.Any;

                    PlayersOnFlop playersOnFlop = PlayersOnFlop.Any;

                    PreflopPotType preflopPotType = PreflopPotType.Any;

                    PreflopActions preflActions = PreflopActions.Any;

                    OtherPlayersActed otherPlayersActed = OtherPlayersActed.Any;

                    if (gc.Round > 1)
                    {
                        BettingAction[] preflopActions = gc.Actions.Where(a => a.Round == 1).ToArray();

                        if (gc.PlayersSawFlop != 2)
                        {
                            playersOnFlop = PlayersOnFlop.Multiway;

                            otherPlayersActed = OtherPlayersActed.Yes;
                        }
                        else
                        {
                            int oppIndexPosition = gc.IsPlayerIn.Select((p, k) => new {Value = p, Index = k}).First(item => item.Value != null && (bool)item.Value && item.Index != i).Index;

                            otherPlayersActed = OtherPlayersActed.No;

                            foreach (BettingAction action in preflopActions)
                            {
                                if(action.Player == i + 1 || action.Player == oppIndexPosition + 1)
                                    continue;

                                if (action.ActionType is BettingActionType.Check or BettingActionType.Call or BettingActionType.Raise)
                                {
                                    otherPlayersActed = OtherPlayersActed.Yes;

                                    break;
                                }
                            }

                            if (otherPlayersActed == OtherPlayersActed.No)
                            {
                                oppPosition = Common.GetPlayerPosition(new int[] {gc.SmallBlindPosition, gc.BigBlindPosition, gc.ButtonPosition}, oppIndexPosition + 1, initialPlayers);

                                if (gameType == GameType.Hu)
                                    relativePosition = gc.SmallBlindPosition == i + 1 ? RelativePosition.Ip : RelativePosition.Oop;
                                else
                                    relativePosition = position > oppPosition ? RelativePosition.Ip : RelativePosition.Oop;
                            }

                            playersOnFlop = PlayersOnFlop.Hu;
                        }

                        int callCount = 0;
                        int raiseCount = 0;

                        foreach (BettingAction action in preflopActions)
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

                        BettingAction[] playerPreflopActions = playerActions.Where(a => a.Round == 1).ToArray();

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

                    #endregion

                    StatSetManager statSetManager = new StatSetManager()
                    {
                        StatSets = statSets[i],
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

                    bool showHud = gc.Error == string.Empty && (lastPlayerAction == null || lastPlayerAction.ActionType != BettingActionType.Fold);

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

                            _vmMain.HudsParentStates[i].PostflopHandsPanelAltState.PostflopHandsGroup = (PostflopHandsGroup)postflopData.MainGroup;
                            _vmMain.HudsParentStates[i].PostflopHandsPanelAltState.Header = missedCell.Description;
                        }
                    }

                    ViewModelStatsHud.SelectedCell = selectedCell?.Name;
                }

                _vmMain.HudsParentStates[i].NameState.UpdateBindings();
                _vmMain.HudsParentStates[i].ActionsState.UpdateBindings();

                _vmMain.HudsParentStates[i].GeneralHudState.UpdateBindings();
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

            _vmMain.ControlsState.Equity = null;
            _vmMain.ControlsState.HandType = HandType.None;

            _vmMain.AnalyzerInfoState.Info = null;

            foreach (ViewModelHudsParent vmHudsParent in _vmMain.HudsParentStates)
            {
                vmHudsParent.NameState.Visible = false;
                vmHudsParent.ActionsState.Visible = false;

                vmHudsParent.GeneralHudState.Visible = false;
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

                vmHudsParent.NameState.UpdateBindings();
                vmHudsParent.ActionsState.UpdateBindings();

                vmHudsParent.GeneralHudState.UpdateBindings();
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
