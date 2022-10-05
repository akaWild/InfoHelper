using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GameInformationUtility;
using GtoDbUtility;
using GtoUtility;
using InfoHelper.Db;
using InfoHelper.Utils;
using InfoHelper.ViewModel.DataEntities;
using Microsoft.Data.SqlClient;

namespace InfoHelper.DataProcessor
{
    public class PreflopGtoManager
    {
        private readonly Dictionary<KeyGto, GtoStrategyContainer> _dictGtoPreflop;

        public PreflopGtoManager()
        {
            byte[] dataBytes = File.ReadAllBytes("Resources\\gtoPreflop.bin");

            _dictGtoPreflop = GtoFormatter.Deserialize(dataBytes);
        }

        public void GetPreflopGtoStrategy(GameContext gc)
        {
            string hc1 = gc.HoleCards[gc.HeroPosition - 1][0];
            string hc2 = gc.HoleCards[gc.HeroPosition - 1][1];

            string pocket = Common.ConvertPocket($"{hc1}{hc2}");

            GameType gameType = gc.SmallBlindPosition == gc.BigBlindPosition ? GameType.Hu : GameType.SixMax;

            float heroInitStack = (float)gc.InitialStacks[gc.HeroPosition - 1];

            List<float> initStacksRemaining = new List<float>();

            for (int i = 0; i < gc.InitialStacks.Length; i++)
            {
                if(gc.IsPlayerIn[i] == null || !(bool)gc.IsPlayerIn[i])
                    continue;

                initStacksRemaining.Add((float)gc.InitialStacks[i]);
            }

            float[] initStacks = initStacksRemaining.OrderByDescending(s => s).ToArray();

            float effStack = heroInitStack >= initStacks[0] ? initStacks[0] : heroInitStack;

            BettingAction[] actions = gc.Actions.Where(a => a.ActionType != BettingActionType.PostSb && a.ActionType != BettingActionType.PostBb).ToArray();

            int?[] playerRelativePositions = new int?[6];

            int playersOutOfGameBehindHero = 0;

            int nextToAct = gc.HeroPosition;

            if (nextToAct == 7)
                nextToAct = 1;

            while (true)
            {
                if (nextToAct == gc.BigBlindPosition)
                    break;

                if (gc.IsPlayerIn[nextToAct - 1] == null)
                    playersOutOfGameBehindHero++;

                nextToAct++;

                if (nextToAct == 7)
                    nextToAct = 1;
            }

            nextToAct = gc.BigBlindPosition + 1;

            if (nextToAct == 7)
                nextToAct = 1;

            int relPositionCounter = 0;

            while (true)
            {
                if (gc.IsPlayerIn[nextToAct - 1] != null)
                {
                    if (nextToAct == gc.SmallBlindPosition)
                        playerRelativePositions[nextToAct - 1] = 4;
                    else if (nextToAct == gc.BigBlindPosition)
                        playerRelativePositions[nextToAct - 1] = 5;
                    else if (nextToAct == gc.ButtonPosition)
                        playerRelativePositions[nextToAct - 1] = 3;
                    else
                        playerRelativePositions[nextToAct - 1] = relPositionCounter + playersOutOfGameBehindHero;
                }

                if (nextToAct == gc.BigBlindPosition)
                    break;

                nextToAct++;

                if (nextToAct == 7)
                    nextToAct = 1;

                relPositionCounter++;
            }

            List<KeyValuePair<KeyGto, GtoStrategyContainer>> filteredStrategies = new List<KeyValuePair<KeyGto, GtoStrategyContainer>>();

            int actionsCounter = 0;

            foreach (var at in _dictGtoPreflop)
            {
                if (at.Key.GameType != gameType)
                    continue;

                bool okActionType = true;

                actionsCounter = 0;

                if (at.Key.Actions.Length != 0)
                {
                    if (actions.Length == 0)
                    {
                        if (at.Key.Actions.All(a => a.Action == GtoAction.Fold))
                        {
                            if (at.Key.Actions.Length== playerRelativePositions.Count(p => p == null))
                            {
                                foreach (GtoActionInfo actionInfo in at.Key.Actions)
                                {
                                    int gtoPlayerPosition = Array.IndexOf(playerRelativePositions, actionInfo.Player);

                                    if (gtoPlayerPosition == -1) 
                                        continue;

                                    okActionType = false;

                                    break;
                                }
                            }
                            else
                                okActionType = false;
                        }
                        else
                            okActionType = false;
                    }
                    else
                    {
                        for (int i = 0; i < at.Key.Actions.Length; i++)
                        {
                            BettingActionType gtoActionType = Common.ConvertGtoAction(at.Key.Actions[i].Action);

                            int gtoPlayerPosition = Array.IndexOf(playerRelativePositions, at.Key.Actions[i].Player);

                            if (gtoActionType == BettingActionType.Fold)
                            {
                                if (gtoPlayerPosition == -1)
                                {
                                    //OK
                                }
                                else if (gc.IsPlayerIn[gtoPlayerPosition] == null)
                                {
                                    //OK
                                }
                                else if (actionsCounter < actions.Length && actions[actionsCounter].ActionType == BettingActionType.Fold && gtoPlayerPosition == actions[actionsCounter].Player - 1)
                                {
                                    //OK
                                    actionsCounter++;
                                }
                                else
                                {
                                    okActionType = false;

                                    break;
                                }
                            }
                            else if (gtoActionType == BettingActionType.Call)
                            {
                                if (actionsCounter < actions.Length && actions[actionsCounter].ActionType == BettingActionType.Call && gtoPlayerPosition != -1 && gtoPlayerPosition == actions[actionsCounter].Player - 1)
                                {
                                    //OK
                                    actionsCounter++;
                                }
                                else if (gtoPlayerPosition != -1 && gc.IsPlayerAllIn[gtoPlayerPosition])
                                {
                                    //OK
                                }
                                else
                                {
                                    okActionType = false;

                                    break;
                                }
                            }
                            else
                            {
                                if (actionsCounter >= actions.Length || gtoActionType != actions[actionsCounter].ActionType || gtoPlayerPosition == -1 || gtoPlayerPosition != actions[actionsCounter].Player - 1)
                                {
                                    okActionType = false;

                                    break;
                                }

                                //OK
                                actionsCounter++;
                            }

                            if (i == at.Key.Actions.Length - 1)
                            {
                                if (actionsCounter != actions.Length)
                                    okActionType = false;
                            }
                        }
                    }
                }
                else
                {
                    if (actions.Length != 0)
                        okActionType = false;
                }

                if (okActionType)
                    filteredStrategies.Add(at);
            }

            filteredStrategies.Sort(new ActionTypeEsComparer());

            float destEs = 0;

            float[] ess = filteredStrategies.Select(at => at.Key.EffectiveStack).Distinct().ToArray();

            float prevEsDelta = float.MaxValue;

            for (int i = 0; i < ess.Length; i++)
            {
                float delta = Math.Abs(effStack - ess[i]);

                if (delta < prevEsDelta)
                {
                    prevEsDelta = delta;

                    if (i == ess.Length - 1)
                        destEs = ess[i];

                    continue;
                }

                if (Math.Abs(delta - prevEsDelta) < 0.001)
                {
                    destEs = ess[i];

                    break;
                }

                destEs = ess[i - 1];

                break;
            }

            float bbDiff = destEs - effStack;
            float bbDiffPercent = bbDiff * 100 / effStack;

            KeyValuePair<KeyGto, GtoStrategyContainer>[] keyValues = filteredStrategies.Where(a => Math.Abs(a.Key.EffectiveStack - destEs) < 0.001).ToArray();

            if(keyValues.Length == 0)
                goto Skip;

            List<KeyValuePair<KeyGto, GtoStrategyContainer>> outputKeys = new List<KeyValuePair<KeyGto, GtoStrategyContainer>>(keyValues);

            int actionsRaisesCounter = 0;

            for (int i = 0; i < actions.Length; i++)
            {
                BettingAction action = actions[i];

                if (action.ActionType != BettingActionType.Raise)
                    continue;

                actionsRaisesCounter++;

                int gtoRaisesCounter = 0;

                int gtoActionsIndex = 0;

                for (int j = 0; j < keyValues[0].Key.Actions.Length; j++)
                {
                    if (outputKeys[0].Key.Actions[j].Action == GtoAction.Raise)
                        gtoRaisesCounter++;

                    if (actionsRaisesCounter == gtoRaisesCounter)
                    {
                        gtoActionsIndex = j;

                        break;
                    }
                }

                outputKeys.Sort(new ActionTypeAmountComparer(gtoActionsIndex));

                keyValues = outputKeys.ToArray();

                float prevAmountDelta = float.MaxValue;

                for (int j = 0; j < keyValues.Length; j++)
                {
                    BettingAction prevAction = null;

                    for (int k = i - 1; k >= actions.Length; k--)
                    {
                        if (actions[k].ActionType == BettingActionType.Raise)
                        {
                            prevAction = actions[k];

                            break;
                        }
                    }

                    float prevActionAmount = (float)(prevAction?.Amount ?? 1);

                    GtoActionInfo prevGtoAction = default;

                    for (int k = gtoActionsIndex - 1; k >= keyValues[j].Key.Actions.Length; k--)
                    {
                        if (keyValues[j].Key.Actions[k].Action == GtoAction.Raise)
                        {
                            prevGtoAction = keyValues[j].Key.Actions[k];

                            break;
                        }
                    }

                    float prevGtoAmount = prevGtoAction != default ? prevGtoAction.Amount : 1;

                    float delta = (float)Math.Abs(keyValues[j].Key.Actions[gtoActionsIndex].Amount / prevGtoAmount - actions[i].Amount / prevActionAmount);

                    if (delta < prevAmountDelta)
                    {
                        prevAmountDelta = delta;

                        if (j == keyValues.Length - 1)
                        {
                            for (int k = 0; k <= j - 1; k++)
                                outputKeys.Remove(keyValues[k]);
                        }

                        continue;
                    }

                    if (Math.Abs(delta - prevAmountDelta) < 0.001)
                    {
                        for (int k = 0; k <= j - 2; k++)
                            outputKeys.Remove(keyValues[k]);

                        for (int k = j + 1; k < keyValues.Length; k++)
                        {
                            float nextDelta = (float)Math.Abs(keyValues[k].Key.Actions[gtoActionsIndex].Amount - actions[i].Amount);

                            if (Math.Abs(nextDelta - delta) < 0.001)
                                continue;

                            if (nextDelta > delta)
                            {
                                for (int l = k; l < keyValues.Length; l++)
                                    outputKeys.Remove(keyValues[l]);

                                break;
                            }

                            if (nextDelta < delta)
                            {
                                delta = nextDelta;

                                for (int l = 0; l <= k - 1; l++)
                                    outputKeys.Remove(keyValues[l]);
                            }
                        }

                        break;
                    }

                    for (int k = 0; k <= j - 2; k++)
                        outputKeys.Remove(keyValues[k]);

                    for (int k = j; k < keyValues.Length; k++)
                        outputKeys.Remove(keyValues[k]);

                    break;
                }
            }

            keyValues = outputKeys.ToArray();

        Skip:
            GtoStrategyContainer gtoStrategyContainer = null;

            float amountDiff = 0;
            float amountDiffPercent = 0;

            string title = "PREFLOP: ";

            if (keyValues.Length > 0)
            {
                KeyValuePair<KeyGto, GtoStrategyContainer> lastKeyValue = keyValues.Last();

                GtoActionInfo[] gtoActions = lastKeyValue.Key.Actions;

                title += gameType == GameType.Hu ? "HU " : "6 max ";

                title += lastKeyValue.Key.EffectiveStack + " bbs";

                if (gtoActions.Length == 0)
                    title += " (Unopened)";
                else
                {
                    title += " (";

                    string ConvertAction(GtoActionInfo action, bool hero)
                    {
                        switch (action.Action)
                        {
                            case GtoAction.Call:
                                return (hero ? "C" : "c") + action.Amount.ToString(CultureInfo.InvariantCulture);

                            case GtoAction.Check:
                                return hero ? "X" : "x";

                            case GtoAction.Fold:
                                return hero ? "F" : "f";

                            case GtoAction.Raise:
                                return (hero ? "R" : "r") + action.Amount.ToString(CultureInfo.InvariantCulture);
                        }

                        return string.Empty;
                    }

                    for (int i = 0; i < gtoActions.Length; i++)
                    {
                        bool hero = gtoActions[i].Player == playerRelativePositions[gc.HeroPosition - 1];

                        title += ConvertAction(gtoActions[i], hero);
                        title += i == gtoActions.Length - 1 ? ")" : " - ";
                    }
                }

                float[] gtoPlayerBets = new float[6];

                gtoPlayerBets[gc.SmallBlindPosition - 1] = 0.5f;
                gtoPlayerBets[gc.BigBlindPosition - 1] = 1;

                foreach (GtoActionInfo gtoAction in gtoActions)
                {
                    int playerIndex = Array.IndexOf(playerRelativePositions, gtoAction.Player);

                    if (playerIndex != -1 && gtoAction.Amount > gtoPlayerBets[playerIndex])
                        gtoPlayerBets[playerIndex] = gtoAction.Amount;
                }

                float gtoPot = gtoPlayerBets.Sum();
                float gtoAmtToCall = gtoPlayerBets.Max() - gtoPlayerBets[gc.HeroPosition - 1];

                float gtoPotOdds = gtoAmtToCall / (gtoPot + gtoAmtToCall);

                float potOdds = (float)(gc.AmountToCall / (gc.TotalPot + gc.AmountToCall));

                amountDiff = gtoPotOdds - potOdds;
                amountDiffPercent = potOdds == 0 ? 0 : amountDiff * 100 / potOdds;

                gtoStrategyContainer = lastKeyValue.Value;
            }

            if (gtoStrategyContainer != null)
            {
                bool containsHands = false;

                foreach (var kv in gtoStrategyContainer)
                {
                    if (kv.Value[0].Abs > 0)
                    {
                        containsHands = true;

                        break;
                    }
                }

                if (containsHands)
                {
                    if (gtoStrategyContainer[pocket][0].Abs == 0)
                    {
                        int firstFvIndex = Array.IndexOf(Common.FaceValues, pocket[0]);

                        string nextPocket;

                        bool matchFound = false;

                        if (pocket[0] == pocket[1])
                        {
                            //Downshift
                            for (int i = firstFvIndex - 1; i >= 0; i--)
                            {
                                nextPocket = $"{Common.FaceValues[i]}{Common.FaceValues[i]}";

                                if (gtoStrategyContainer[nextPocket][0].Abs != 0)
                                {
                                    gtoStrategyContainer.AddOrUpdateStrategy(pocket, gtoStrategyContainer[nextPocket]);

                                    matchFound = true;

                                    break;
                                }
                            }

                            //Upshift
                            if (!matchFound)
                            {
                                for (int i = firstFvIndex + 1; i < Common.FaceValues.Length; i++)
                                {
                                    nextPocket = $"{Common.FaceValues[i]}{Common.FaceValues[i]}";

                                    if (gtoStrategyContainer[nextPocket][0].Abs != 0)
                                    {
                                        gtoStrategyContainer.AddOrUpdateStrategy(pocket, gtoStrategyContainer[nextPocket]);

                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            while (true)
                            {
                                if(firstFvIndex == Common.FaceValues.Length)
                                    break;

                                int secondFvIndex = Array.IndexOf(Common.FaceValues, pocket[1]);

                                //Downshift
                                for (int i = secondFvIndex; i >= 0; i--)
                                {
                                    nextPocket = $"{Common.FaceValues[firstFvIndex]}{Common.FaceValues[i]}{pocket[2]}";

                                    if (gtoStrategyContainer[nextPocket][0].Abs != 0)
                                    {
                                        gtoStrategyContainer.AddOrUpdateStrategy(pocket, gtoStrategyContainer[nextPocket]);

                                        matchFound = true;

                                        break;
                                    }
                                }

                                if(matchFound)
                                    break;

                                //Upshift
                                for (int i = secondFvIndex + 1; i < Common.FaceValues.Length; i++)
                                {
                                    if(firstFvIndex == i)
                                        break;

                                    nextPocket = $"{Common.FaceValues[firstFvIndex]}{Common.FaceValues[i]}{pocket[2]}";

                                    if (gtoStrategyContainer[nextPocket][0].Abs != 0)
                                    {
                                        gtoStrategyContainer.AddOrUpdateStrategy(pocket, gtoStrategyContainer[nextPocket]);

                                        matchFound = true;

                                        break;
                                    }
                                }

                                if (matchFound)
                                    break;

                                firstFvIndex++;
                            }
                        }
                    }

                    GtoInfo preflopGtoInfo = new GtoInfo()
                    {
                        GtoDiffs = new GtoDiffs()
                        {
                            AmountDiff = amountDiff,
                            AmountDiffPercent = amountDiffPercent,
                            BbDiff = bbDiff,
                            BbDiffPercent = bbDiffPercent
                        },
                        GtoStrategyContainer = gtoStrategyContainer,
                        PocketStrategies = gtoStrategyContainer[pocket],
                        Pocket = pocket,
                        PocketRender = pocket,
                        Title = title,
                        Round = 1
                    };

                    lock (gc.GtoLock)
                    {
                        gc.GtoData = preflopGtoInfo;

                        gc.GtoError = null;
                    }
                }
                else
                    gc.GtoError = "There are no hands for current GTO situation";
            }
            else
            {
                lock (gc.GtoLock)
                    gc.GtoError = "There is no suitable preflop GTO strategy for current situation";
            }
        }

        #region Private classes

        private class ActionTypeEsComparer : IComparer<KeyValuePair<KeyGto, GtoStrategyContainer>>
        {
            public int Compare(KeyValuePair<KeyGto, GtoStrategyContainer> x, KeyValuePair<KeyGto, GtoStrategyContainer> y)
            {
                if (x.Key.EffectiveStack < y.Key.EffectiveStack)
                    return -1;

                return x.Key.EffectiveStack > y.Key.EffectiveStack ? 1 : 0;
            }
        }

        private class ActionTypeAmountComparer : IComparer<KeyValuePair<KeyGto, GtoStrategyContainer>>
        {
            private readonly int _index;

            public ActionTypeAmountComparer(int index)
            {
                _index = index;
            }

            public int Compare(KeyValuePair<KeyGto, GtoStrategyContainer> x, KeyValuePair<KeyGto, GtoStrategyContainer> y)
            {
                if (x.Key.Actions[_index].Amount < y.Key.Actions[_index].Amount)
                    return -1;

                if (x.Key.Actions[_index].Amount > y.Key.Actions[_index].Amount)
                    return 1;

                return 0;
            }
        }

        #endregion
    }
}
