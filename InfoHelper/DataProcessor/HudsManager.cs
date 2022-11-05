using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using GameInformationUtility;
using InfoHelper.StatsEntities;
using InfoHelper.ViewModel.DataEntities;
using InfoHelper.ViewModel.States;

namespace InfoHelper.DataProcessor
{
    public class HudsManager
    {
        private readonly ViewModelMain _vmMain;

        public HudsManager(ViewModelMain vmMain)
        {
            _vmMain = vmMain;
        }

        public void UpdateHuds(GameContext gc, StatSet[][] statSets)
        {
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
                    _vmMain.HudsParentStates[i].PostflopHuIpHudState.Visible = false;
                    _vmMain.HudsParentStates[i].PostflopHuOopHudState.Visible = false;
                    _vmMain.HudsParentStates[i].PostflopGeneralHudState.Visible = false;
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

                    _vmMain.HudsParentStates[i].ActionsState.Actions = actionsString;

                    #region Retrieve Set variables

                    GameType gameType = gc.SmallBlindPosition == gc.ButtonPosition ? GameType.Hu : GameType.SixMax;

                    Round round = gc.Round < 2 ? Round.Preflop : Round.Postflop;

                    Position position = GetPlayerPosition(i + 1);

                    Position oppPosition = Position.Any;

                    RelativePosition relativePosition = RelativePosition.Any;

                    PlayersOnFlop playersOnFlop = PlayersOnFlop.Any;

                    PreflopPotType preflopPotType = PreflopPotType.Any;

                    PreflopActions preflActions = PreflopActions.Any;

                    if (gc.Round > 1)
                    {
                        if (gc.PlayersSawFlop != 2)
                        {
                            oppPosition = Position.Any;

                            relativePosition = RelativePosition.Any;

                            playersOnFlop = PlayersOnFlop.Multiway;
                        }
                        else
                        {
                            int oppIndexPosition = gc.IsPlayerIn.Select((p, k) => new {Value = p, Index = k}).First(item => item.Value != null && (bool)item.Value && item.Index != i).Index;

                            oppPosition = GetPlayerPosition(oppIndexPosition + 1);

                            if (gameType == GameType.Hu)
                                relativePosition = gc.SmallBlindPosition == i + 1 ? RelativePosition.Ip : RelativePosition.Oop;
                            else
                                relativePosition = position > oppPosition ? RelativePosition.Ip : RelativePosition.Oop;

                            playersOnFlop = PlayersOnFlop.Hu;
                        }

                        BettingAction[] preflopActions = gc.Actions.Where(a => a.Round == 1).ToArray();

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
                                    preflopPotType = PreflopPotType.Unknown;

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

                    //General hud
                    StatSet generalSet = GetStatSet(SetType.General);

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

                    bool showHud = lastPlayerAction == null || lastPlayerAction.ActionType != BettingActionType.Fold;

                    //Btn preflop hud
                    StatSet btnPreflopSet = GetStatSet(SetType.PreflopBtn);

                    if (btnPreflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].BtnPreflopHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].BtnPreflopHudState.Visible = true;

                        _vmMain.HudsParentStates[i].BtnPreflopHudState.SetData(btnPreflopSet.Cells);

                        _vmMain.HudsParentStates[i].BtnPreflopHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].BtnPreflopHudState.SetName = $"{btnPreflopSet}";
                    }

                    //Sb preflop hud
                    StatSet sbPreflopSet = GetStatSet(SetType.PreflopSb);

                    if (sbPreflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].SbPreflopHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].SbPreflopHudState.Visible = true;

                        _vmMain.HudsParentStates[i].SbPreflopHudState.SetData(sbPreflopSet.Cells);

                        _vmMain.HudsParentStates[i].SbPreflopHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].SbPreflopHudState.SetName = $"{sbPreflopSet}";
                    }

                    //Bb preflop hud
                    StatSet bbPreflopSet = GetStatSet(SetType.PreflopBb);

                    if (bbPreflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].BbPreflopHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].BbPreflopHudState.Visible = true;

                        _vmMain.HudsParentStates[i].BbPreflopHudState.SetData(bbPreflopSet.Cells);

                        _vmMain.HudsParentStates[i].BbPreflopHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].BbPreflopHudState.SetName = $"{bbPreflopSet}";
                    }

                    //Ep preflop hud
                    StatSet epPreflopSet = GetStatSet(SetType.PreflopEp);

                    if (epPreflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].EpPreflopHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].EpPreflopHudState.Visible = true;

                        _vmMain.HudsParentStates[i].EpPreflopHudState.SetData(epPreflopSet.Cells);

                        _vmMain.HudsParentStates[i].EpPreflopHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].EpPreflopHudState.SetName = $"{epPreflopSet}";
                    }

                    //Mp preflop hud
                    StatSet mpPreflopSet = GetStatSet(SetType.PreflopMp);

                    if (mpPreflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].MpPreflopHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].MpPreflopHudState.Visible = true;

                        _vmMain.HudsParentStates[i].MpPreflopHudState.SetData(mpPreflopSet.Cells);

                        _vmMain.HudsParentStates[i].MpPreflopHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].MpPreflopHudState.SetName = $"{mpPreflopSet}";
                    }

                    //Co preflop hud
                    StatSet coPreflopSet = GetStatSet(SetType.PreflopCo);

                    if (coPreflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].CoPreflopHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].CoPreflopHudState.Visible = true;

                        _vmMain.HudsParentStates[i].CoPreflopHudState.SetData(coPreflopSet.Cells);

                        _vmMain.HudsParentStates[i].CoPreflopHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].CoPreflopHudState.SetName = $"{coPreflopSet}";
                    }

                    //Sb vs Bb preflop hud
                    StatSet sbvsbbPreflopSet = GetStatSet(SetType.PreflopSbvsBb);

                    if (sbvsbbPreflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].SbvsBbPreflopHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].SbvsBbPreflopHudState.Visible = true;

                        _vmMain.HudsParentStates[i].SbvsBbPreflopHudState.SetData(sbvsbbPreflopSet.Cells);

                        _vmMain.HudsParentStates[i].SbvsBbPreflopHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].SbvsBbPreflopHudState.SetName = $"{sbvsbbPreflopSet}";
                    }

                    //Bb vs Sb preflop hud
                    StatSet bbvssbPreflopSet = GetStatSet(SetType.PreflopBbvsSb);

                    if (bbvssbPreflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].BbvsSbPreflopHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].BbvsSbPreflopHudState.Visible = true;

                        _vmMain.HudsParentStates[i].BbvsSbPreflopHudState.SetData(bbvssbPreflopSet.Cells);

                        _vmMain.HudsParentStates[i].BbvsSbPreflopHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].BbvsSbPreflopHudState.SetName = $"{bbvssbPreflopSet}";
                    }

                    //Postflop Hu Ip hud
                    StatSet huIpPostflopSet = GetStatSet(SetType.PostflopHuIp);

                    if (huIpPostflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].PostflopHuIpHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].PostflopHuIpHudState.Visible = true;

                        _vmMain.HudsParentStates[i].PostflopHuIpHudState.SetData(huIpPostflopSet.Cells);

                        _vmMain.HudsParentStates[i].PostflopHuIpHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].PostflopHuIpHudState.SetName = $"{huIpPostflopSet}";
                    }

                    //Postflop Hu Oop hud
                    StatSet huOopPostflopSet = GetStatSet(SetType.PostflopHuOop);

                    if (huIpPostflopSet != null || huOopPostflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].PostflopHuOopHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].PostflopHuOopHudState.Visible = true;

                        _vmMain.HudsParentStates[i].PostflopHuOopHudState.SetData(huOopPostflopSet.Cells);

                        _vmMain.HudsParentStates[i].PostflopHuOopHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].PostflopHuOopHudState.SetName = $"{huOopPostflopSet}";
                    }

                    //Postflop general hud
                    StatSet generalPostflopSet = GetStatSet(SetType.PostflopGeneral);

                    if (huIpPostflopSet != null || huOopPostflopSet != null || generalPostflopSet == null || !showHud)
                        _vmMain.HudsParentStates[i].PostflopGeneralHudState.Visible = false;
                    else
                    {
                        _vmMain.HudsParentStates[i].PostflopGeneralHudState.Visible = true;

                        _vmMain.HudsParentStates[i].PostflopGeneralHudState.SetData(generalPostflopSet.Cells);

                        _vmMain.HudsParentStates[i].PostflopGeneralHudState.PlayerName = gc.Players[i];

                        _vmMain.HudsParentStates[i].PostflopGeneralHudState.SetName = $"{generalPostflopSet}";
                    }

                    Position GetPlayerPosition(int player)
                    {
                        if (player == gc.SmallBlindPosition)
                            return Position.Sb;

                        if (player == gc.BigBlindPosition)
                            return Position.Bb;

                        if (player == gc.ButtonPosition)
                            return Position.Btn;

                        int nextToAct = gc.BigBlindPosition + 1;

                        if (nextToAct == 7)
                            nextToAct = 1;

                        int positionShift = 1;

                        while (true)
                        {
                            if (player == nextToAct)
                                break;

                            positionShift++;

                            nextToAct++;

                            if (nextToAct == 7)
                                nextToAct = 1;
                        }

                        if (positionShift == 1)
                            return Position.Ep;

                        if (positionShift == 2)
                            return Position.Mp;

                        if (positionShift == 3)
                            return Position.Co;

                        throw new Exception($"Player {player} position was not found");
                    }

                    StatSet GetStatSet(SetType setType)
                    {
                        if (statSets[i] == null)
                            return null;

                        List<StatSet> matchedSets = new List<StatSet>();

                        foreach (StatSet set in statSets[i])
                        {
                            if ((set.GameType & gameType) == 0)
                                continue;

                            if ((set.Round & round) == 0)
                                continue;

                            if ((set.Position & position) == 0)
                                continue;

                            if ((set.RelativePosition & relativePosition) == 0)
                                continue;

                            if ((set.OppPosition & oppPosition) == 0)
                                continue;

                            if ((set.PlayersOnFlop & playersOnFlop) == 0)
                                continue;

                            if ((set.PreflopPotType & preflopPotType) == 0)
                                continue;

                            if ((set.PreflopActions & preflActions) == 0)
                                continue;

                            if ((set.SetType & setType) == 0)
                                continue;

                            matchedSets.Add(set);
                        }

                        if (matchedSets.Count == 1)
                            return matchedSets[0];

                        if (matchedSets.Count > 1)
                        {
                            StatSet firstSet = matchedSets[0];

                            for (int j = 1; j < matchedSets.Count; j++)
                            {
                                StatSet set = matchedSets[j];

                                if (firstSet.GameType != set.GameType)
                                    return firstSet.GameType < set.GameType ? firstSet : set;

                                if (firstSet.Round != set.Round)
                                    return firstSet.Round < set.Round ? firstSet : set;

                                if (firstSet.Position != set.Position)
                                    return firstSet.Position < set.Position ? firstSet : set;

                                if (firstSet.RelativePosition != set.RelativePosition)
                                    return firstSet.RelativePosition < set.RelativePosition ? firstSet : set;

                                if (firstSet.OppPosition != set.OppPosition)
                                    return firstSet.OppPosition < set.OppPosition ? firstSet : set;

                                if (firstSet.PlayersOnFlop != set.PlayersOnFlop)
                                    return firstSet.PlayersOnFlop < set.PlayersOnFlop ? firstSet : set;

                                if (firstSet.PreflopPotType != set.PreflopPotType)
                                    return firstSet.PreflopPotType < set.PreflopPotType ? firstSet : set;

                                if (firstSet.PreflopActions != set.PreflopActions)
                                    return firstSet.PreflopActions < set.PreflopActions ? firstSet : set;

                                if (firstSet.SetType != set.SetType)
                                    return firstSet.SetType < set.SetType ? firstSet : set;

                                throw new Exception($"Stat set for player {i + 1} contain identical records");
                            }
                        }

                        return null;
                    }
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
                _vmMain.HudsParentStates[i].PostflopHuIpHudState.UpdateBindings();
                _vmMain.HudsParentStates[i].PostflopHuOopHudState.UpdateBindings();
                _vmMain.HudsParentStates[i].PostflopGeneralHudState.UpdateBindings();
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
                vmHudsParent.PostflopHuIpHudState.Visible = false;
                vmHudsParent.PostflopHuOopHudState.Visible = false;
                vmHudsParent.PostflopGeneralHudState.Visible = false;

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
                vmHudsParent.PostflopHuIpHudState.UpdateBindings();
                vmHudsParent.PostflopHuOopHudState.UpdateBindings();
                vmHudsParent.PostflopGeneralHudState.UpdateBindings();
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
    }
}
