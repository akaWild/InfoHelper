using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using HoldemHand;
using InfoHelper.Db;
using InfoHelper.Utils;
using InfoHelper.ViewModel.DataEntities;
using K4os.Compression.LZ4;
using Microsoft.Data.SqlClient;
using RemoteSolverUtility;
using SolverUtility;

namespace InfoHelper.DataProcessor
{
    public delegate void SolverExceptionThrownEventHandler(Exception exception);

    public class PostflopGtoManager
    {
        public event SolverExceptionThrownEventHandler SolverExceptionThrown;

        private readonly PostflopEntry[] _postflopEntries;

        private readonly GtoDbContext _gtoDbContext;

        private readonly BinaryFormatter _binaryFormatter;

        private readonly ISolverManager _solverManager;

        private readonly SolverTreeProxy _solverTreeProxy;

        private const int SolverCommandTimeout = 3;

        private const int SolverCommandExtTimeout = 10;

        public PostflopGtoManager()
        {
            SqlConnectionStringBuilder sqlBuilder = new SqlConnectionStringBuilder
            {
                DataSource = Shared.ServerName,
                IntegratedSecurity = true,
                InitialCatalog = Shared.GtoDbName
            };

            _gtoDbContext = new GtoDbContext(sqlBuilder.ConnectionString);

            _postflopEntries = _gtoDbContext.PostflopEntries.ToArray();

            _binaryFormatter = new BinaryFormatter();

            _solverManager = Shared.IsLocalServer ? new SolverManager(Shared.SolverFolder, Shared.SolverFile) : RemoteSolverProxy.GetRemoteSolver(Shared.SolverIp, Shared.SolverPort);

            _solverTreeProxy = new SolverTreeProxy(_solverManager) { SolverCommandTimeout = SolverCommandTimeout };
        }

        public void GetPostflopGtoStrategy(GameContext gc)
        {
            if (gc.PlayersSawFlop != 2)
            {
                lock (gc.GtoLock)
                    gc.GtoError = "Unable to solve situation in GTO because number of players postflop != 2";

                return;
            }

            if (gc.Round == 2)
            {
                if (gc.RoundChanged)
                {
                    InitializeFlopTree(gc);

                    if (gc.GtoError == null)
                        ProcessTree(gc);
                }
            }
            else if (gc.Round == 3)
            {
                if (gc.FlopTree != null)
                {
                    if (!gc.IsSolving)
                        SolveTree(gc);
                }
                else
                {
                    lock (gc.GtoLock)
                        gc.GtoError = "Flop tree is missing. Unable to solve GTO on turn";
                }
            }
            else if (gc.Round == 4)
            {
                if (gc.TurnTree != null)
                {
                    if (!gc.IsSolving)
                        SolveTree(gc);
                }
                else
                {
                    lock (gc.GtoLock)
                        gc.GtoError = "Turn tree is missing. Unable to solve GTO on river";
                }
            }
        }

        private void ProcessTree(GameContext gc)
        {
            SolverTree postflopTree = null;

            if (gc.Round == 2)
                postflopTree = (SolverTree)gc.FlopTree;
            else if (gc.Round == 3)
                postflopTree = (SolverTree)gc.TurnTree;
            else if (gc.Round == 4)
                postflopTree = (SolverTree)gc.RiverTree;

            if (postflopTree == null)
            {
                lock (gc.GtoLock)
                    gc.GtoError = "There is no solver tree";

                return;
            }

            float heroRoundStack = (float)gc.Stacks[gc.HeroPosition - 1].Value;

            float villainRoundStack = -1;

            for (int i = 0; i < gc.IsPlayerIn.Length; i++)
            {
                if (gc.IsPlayerIn[i] != null && (bool)gc.IsPlayerIn[i] && i != gc.HeroPosition - 1)
                {
                    villainRoundStack = (float)gc.Stacks[i].Value;

                    break;
                }
            }

            float effStackTree = postflopTree.InitialStack / 10F;
            float effStack = heroRoundStack >= villainRoundStack ? villainRoundStack : heroRoundStack;

            int heroRelativePosition = -1;

            int nextToAct = gc.ButtonPosition + 1;

            if (nextToAct == 7)
                nextToAct = 1;

            while (true)
            {
                if (gc.IsPlayerIn[nextToAct - 1] != null && (bool)gc.IsPlayerIn[nextToAct - 1])
                {
                    heroRelativePosition = nextToAct == gc.HeroPosition ? 0 : 1;

                    break;
                }

                nextToAct++;

                if (nextToAct == 7)
                    nextToAct = 1;
            }

            float bbDiff = effStackTree - effStack;
            float bbDiffPercent = bbDiff * 100 / effStack;

            SolverNode destNode = null;

            float[] betsCurrent = new float[6];
            float[] betsTotal = new float[6];

            BettingAction[] postflopActions = gc.Actions.Where(a => a.Round == postflopTree.Round).ToArray();

            if (postflopActions.Length == 0)
                destNode = postflopTree["r:0"];
            else
            {
                SolverNode rootNode = postflopTree.RootNode;

                SolverNode nextNode = rootNode;

                for (int i = 0; i < postflopActions.Length; i++)
                {
                    BettingAction action = postflopActions[i];

                    float currentPot = (float)(gc.RoundPot + betsTotal.Sum());

                    if (action.ActionType == BettingActionType.Check)
                    {
                        SolverNode checkNode = nextNode.ChildNodes?.FirstOrDefault(n => n.ActionType == SolverActionType.Check);

                        if (checkNode == null)
                        {
                            nextNode = null;

                            break;
                        }

                        nextNode = checkNode;
                    }
                    else if (action.ActionType == BettingActionType.Call)
                    {
                        SolverNode callNode = nextNode.ChildNodes?.FirstOrDefault(n => n.ActionType == SolverActionType.Call);

                        if (callNode == null)
                        {
                            nextNode = null;

                            break;
                        }

                        nextNode = callNode;
                    }
                    else if (action.ActionType == BettingActionType.Bet)
                    {
                        if (nextNode.ChildNodes == null)
                        {
                            nextNode = null;

                            break;
                        }

                        SolverNode betNode = null;

                        SolverNode[] betNodes = nextNode.ChildNodes.Where(n => n.ActionType == SolverActionType.Bet).ToArray();

                        float prevAmountDelta = float.MaxValue;

                        for (int j = 0; j < betNodes.Length; j++)
                        {
                            float delta = (float)Math.Abs(action.Amount / currentPot - (float)betNodes[j].Amount / betNodes[j].CurrentPot);

                            if (delta < prevAmountDelta)
                            {
                                prevAmountDelta = delta;

                                if (j == betNodes.Length - 1)
                                    betNode = betNodes[j];

                                continue;
                            }

                            betNode = betNodes[j - (Math.Abs(delta - prevAmountDelta) < 0.001 ? 0 : 1)];

                            break;
                        }

                        if (betNode == null)
                        {
                            nextNode = null;

                            break;
                        }

                        nextNode = betNode;
                    }
                    else if (action.ActionType == BettingActionType.Raise)
                    {
                        if (nextNode.ChildNodes == null)
                        {
                            nextNode = null;

                            break;
                        }

                        SolverNode raiseNode = null;

                        SolverNode[] raiseNodes = nextNode.ChildNodes.Where(n => n.ActionType == SolverActionType.Raise).ToArray();

                        float prevActionAmount = 0;

                        for (int j = i - 1; j >= 0; j--)
                        {
                            if (postflopActions[j].Player == action.Player)
                            {
                                prevActionAmount = (float)postflopActions[j].Amount;

                                break;
                            }
                        }

                        SolverNode prevPlayerNode = null;

                        SolverNode nextPlayerNode = raiseNodes.Length > 0 ? raiseNodes[0].ParentNode : null;

                        if (nextPlayerNode != null)
                        {
                            while (true)
                            {
                                if (nextPlayerNode.NodeType == NodeType.Root)
                                    break;

                                if (nextPlayerNode.Position == raiseNodes[0].Position)
                                {
                                    prevPlayerNode = nextPlayerNode;

                                    break;
                                }

                                nextPlayerNode = nextPlayerNode.ParentNode;
                            }
                        }

                        float prevAmountDelta = float.MaxValue;

                        for (int j = 0; j < raiseNodes.Length; j++)
                        {
                            float delta = (float)Math.Abs((action.Amount - prevActionAmount) / currentPot - (float)(raiseNodes[j].Amount - (prevPlayerNode?.Amount ?? 0)) / raiseNodes[j].CurrentPot);

                            if (delta < prevAmountDelta)
                            {
                                prevAmountDelta = delta;

                                if (j == raiseNodes.Length - 1)
                                    raiseNode = raiseNodes[j];

                                continue;
                            }

                            raiseNode = raiseNodes[j - (Math.Abs(delta - prevAmountDelta) < 0.001 ? 0 : 1)];

                            break;
                        }

                        if (raiseNode == null)
                        {
                            nextNode = null;

                            break;
                        }

                        nextNode = raiseNode;
                    }

                    betsTotal[action.Player - 1] += (float)action.Amount - betsCurrent[action.Player - 1];

                    betsCurrent[action.Player - 1] = (float)action.Amount;
                }

                destNode = nextNode;
            }

            if (destNode != null)
            {
                float gtoPot = destNode.CurrentPot + destNode.Amount;
                float gtoAmtToCall = destNode.Amount + (destNode.Position == -1 ? 0 : (destNode.Bets[destNode.Position] - destNode.Bets[heroRelativePosition]));

                float gtoPotOdds = gtoAmtToCall / (gtoPot + gtoAmtToCall);

                float[] playerBets = new float[6];

                foreach (BettingAction action in postflopActions)
                {
                    if (gc.IsPlayerIn[action.Player - 1] != null && action.Amount > playerBets[action.Player - 1])
                        playerBets[action.Player - 1] = (float)action.Amount;
                }

                for (int i = 0; i < playerBets.Length; i++)
                {
                    if (playerBets[i] > playerBets[gc.HeroPosition - 1] + gc.Stacks[gc.HeroPosition - 1])
                        playerBets[i] = (float)(playerBets[gc.HeroPosition - 1] + gc.Stacks[gc.HeroPosition - 1]);
                }

                float potOdds = (float)(gc.AmountToCall / (gc.RoundPot + playerBets.Sum() + gc.AmountToCall));

                float amountDiff = gtoPotOdds - potOdds;
                float amountDiffPercent = potOdds == 0 ? 0 : amountDiff * 100 / potOdds;

                string roundCapture = string.Empty;

                switch (postflopTree.Round)
                {
                    case 2:
                        roundCapture = "FLOP";
                        break;

                    case 3:
                        roundCapture = "TURN";
                        break;

                    case 4:
                        roundCapture = "RIVER";
                        break;
                }

                string title = $"{roundCapture}: ";

                title += $"{Math.Round(postflopTree.InitialStack / 10F, 1).ToString(CultureInfo.InvariantCulture)}/{Math.Round(postflopTree.InitialPot / 10F, 1).ToString(CultureInfo.InvariantCulture)} bbs";

                if (destNode.NodeType == NodeType.Root)
                    title += " (Unopened)";
                else
                {
                    title += " (";

                    List<SolverNode> prevNodes = new List<SolverNode> { destNode };

                    SolverNode currentNode = destNode;

                    while (true)
                    {
                        if (currentNode.ParentNode.NodeType == NodeType.Root)
                            break;

                        currentNode = currentNode.ParentNode;

                        prevNodes.Add(currentNode);
                    }

                    prevNodes.Reverse();

                    for (int i = 0; i < prevNodes.Count; i++)
                        title += $"{ConvertAction(prevNodes[i].ActionType, (prevNodes[i].Amount + prevNodes[i].Bets[prevNodes[i].Position]) / 10F, prevNodes[i].Position == heroRelativePosition)}" +
                                 $"{(i == prevNodes.Count - 1 ? ")" : "-")}";

                    string ConvertAction(SolverActionType action, float amount, bool hero)
                    {
                        return action switch
                        {
                            SolverActionType.Call => (hero ? "C" : "c") + $"{amount.ToString(CultureInfo.InvariantCulture)}",
                            SolverActionType.Check => hero ? "X" : "x",
                            SolverActionType.Fold => hero ? "F" : "f",
                            SolverActionType.Raise => (hero ? "R" : "r") + $"{amount.ToString(CultureInfo.InvariantCulture)}",
                            SolverActionType.Bet => (hero ? "B" : "b") + $"{amount.ToString(CultureInfo.InvariantCulture)}",
                            _ => string.Empty
                        };
                    }
                }

                if (postflopTree.Accuracy != null)
                    title += $" Expl. = {Math.Round(postflopTree.Accuracy.Value, 2).ToString(CultureInfo.InvariantCulture)}%";

                //Filling postflop gto data
                GtoStrategyContainer gtoStrategyContainer = new GtoStrategyContainer();

                string hc1 = gc.HoleCards[gc.HeroPosition - 1][0];
                string hc2 = gc.HoleCards[gc.HeroPosition - 1][1];

                string pocketRender = $"{hc1}{hc2}";

                string pocket = Common.ConvertPocket(pocketRender);

                float lineFrequency = float.MaxValue;

                if (gc.Round > 2)
                    lineFrequency = _solverTreeProxy.GetLineFrequency(gc.TableId, destNode);

                ulong flopMask = Hand.ParseHand($"{gc.FlopCard1}{gc.FlopCard2}{gc.FlopCard3}");
                ulong gtoFlopMask = Hand.ParseHandUnique(flopMask);

                ulong pocketMask = Hand.ParseHand(pocketRender);

                GtoStrategy[] pocketStrategies = null;

                Dictionary<ulong, GtoStrategy[]> handStrategies = new Dictionary<ulong, GtoStrategy[]>();

                for (int i = 0; i < Common.PioRangesMask.Length; i++)
                {
                    ulong handMask = Common.PioRangesMask[i];

                    int handIndex = Common.HandRangesLookUp[handMask];

                    GtoStrategy[] strategies = new GtoStrategy[destNode.ChildNodes.Length];

                    float percentageSum = destNode.ChildNodes.Sum(n => _solverTreeProxy.GetStrategy(gc.TableId, n)[handIndex]);

                    for (int j = 0; j < destNode.ChildNodes.Length; j++)
                    {
                        SolverNode childNode = destNode.ChildNodes[j];

                        float percentage = percentageSum == 0 ? 0 : _solverTreeProxy.GetStrategy(gc.TableId, childNode)[handIndex] / percentageSum;

                        if (percentage < 0)
                            percentage = 0;

                        float ev = lineFrequency == 0 ? float.NaN : _solverTreeProxy.GetEvs(gc.TableId, childNode, childNode.Position)[handIndex];

                        float rangeData = _solverTreeProxy.GetRange(gc.TableId, destNode.ParentNode ?? destNode, childNode.Position)[handIndex];

                        float amount = (childNode.Amount + childNode.Bets[childNode.Position]) / 10F;

                        GtoAction action = Common.ConvertSolverAction(childNode.ActionType);

                        strategies[j] = new GtoStrategy(new GtoActionInfo(action, amount, heroRelativePosition), percentage, ev, rangeData * 100);
                    }

                    if (pocketMask == (gc.Round == 2 ? Hand.ConvertHandFromGto(gtoFlopMask, flopMask, handMask) : handMask))
                        pocketStrategies = strategies;

                    handStrategies[Common.PioRangesMask[handIndex]] = strategies;
                }

                for (int i = 0; i < Common.HoleCards.Length; i++)
                {
                    string hand = Common.HoleCards[i];

                    int[] pioIndexes = GetHandPostflopIndexes(hand);

                    GtoStrategy[] transformedStrategies = new GtoStrategy[destNode.ChildNodes.Length];

                    for (int j = 0; j < transformedStrategies.Length; j++)
                    {
                        float absSum = pioIndexes.Select(pi => handStrategies[Common.PioRangesMask[pi]][j].Abs).Sum();

                        GtoActionInfo action = handStrategies[Common.PioRangesMask[pioIndexes[0]]][j].ActionInfo;

                        float percentage = 0;

                        float ev = 0;

                        for (int k = 0; k < pioIndexes.Length; k++)
                        {
                            GtoStrategy handStrat = handStrategies[Common.PioRangesMask[pioIndexes[k]]][j];

                            percentage += absSum == 0 ? 0 : handStrat.Percent * handStrat.Abs / absSum;

                            ev += absSum == 0 ? 0 : handStrat.Ev * handStrat.Abs / absSum;
                        }

                        transformedStrategies[j] = new GtoStrategy(action, percentage, ev, absSum / pioIndexes.Length);
                    }

                    gtoStrategyContainer.Add(hand, transformedStrategies);
                }

                int[] GetHandPostflopIndexes(string hand)
                {
                    List<int> pioIndexes = new List<int>();

                    if (hand.Length == 2)
                    {
                        for (int j = 0; j < Common.PioRanges.Length; j++)
                        {
                            if (Common.PioRanges[j][0] == Common.PioRanges[j][2] && Common.PioRanges[j][0] == hand[0])
                                pioIndexes.Add(j);
                        }
                    }
                    else
                    {
                        if (hand[2] == 's')
                        {
                            for (int j = 0; j < Common.PioRanges.Length; j++)
                            {
                                if (Common.PioRanges[j][1] == Common.PioRanges[j][3] && Common.PioRanges[j][0] == hand[0] && Common.PioRanges[j][2] == hand[1])
                                    pioIndexes.Add(j);
                            }
                        }
                        else
                        {
                            for (int j = 0; j < Common.PioRanges.Length; j++)
                            {
                                if (Common.PioRanges[j][1] != Common.PioRanges[j][3] && Common.PioRanges[j][0] == hand[0] && Common.PioRanges[j][2] == hand[1])
                                    pioIndexes.Add(j);
                            }
                        }
                    }

                    return pioIndexes.ToArray();
                }

                GtoInfo postflopGtoInfo = new GtoInfo()
                {
                    GtoDiffs = new GtoDiffs()
                    {
                        AmountDiff = amountDiff,
                        AmountDiffPercent = amountDiffPercent,
                        BbDiff = bbDiff,
                        BbDiffPercent = bbDiffPercent
                    },
                    GtoStrategyContainer = gtoStrategyContainer,
                    PocketStrategies = pocketStrategies,
                    Pocket = pocket,
                    PocketRender = pocketRender,
                    Title = title,
                    Round = gc.Round
                };

                lock (gc.GtoLock)
                {
                    gc.GtoData = postflopGtoInfo;

                    gc.GtoError = null;
                }
            }
            else
            {
                lock (gc.GtoLock)
                    gc.GtoError = "Solver tree doesn't contain current actions sequence";
            }
        }

        private void InitializeFlopTree(GameContext gc)
        {
            GameType gameType = gc.SmallBlindPosition == gc.ButtonPosition ? GameType.Hu : GameType.SixMax;

            float heroInitStack = (float)gc.InitialStacks[gc.HeroPosition - 1];

            float villainInitStack = -1;

            for (int i = 0; i < gc.IsPlayerIn.Length; i++)
            {
                if (gc.IsPlayerIn[i] != null && (bool)gc.IsPlayerIn[i] && i != gc.HeroPosition - 1)
                {
                    villainInitStack = (float)gc.InitialStacks[i].Value;

                    break;
                }
            }

            float effStack = heroInitStack >= villainInitStack ? villainInitStack : heroInitStack;

            int[] playerRelativePositions = new int[6];

            int nextToAct = gc.BigBlindPosition + 1;

            if (nextToAct == 7)
                nextToAct = 1;

            int relPositionCounter = 0;

            while (true)
            {
                if (nextToAct == gc.SmallBlindPosition)
                    playerRelativePositions[nextToAct - 1] = 4;
                else if (nextToAct == gc.BigBlindPosition)
                    playerRelativePositions[nextToAct - 1] = 5;
                else if (nextToAct == gc.ButtonPosition)
                    playerRelativePositions[nextToAct - 1] = 3;
                else
                    playerRelativePositions[nextToAct - 1] = relPositionCounter;

                if (nextToAct == gc.BigBlindPosition)
                    break;

                nextToAct++;

                if (nextToAct == 7)
                    nextToAct = 1;

                relPositionCounter++;
            }

            BettingAction[] preflopActions = gc.Actions.Where(a => a.Round == 1 && a.ActionType != BettingActionType.PostSb && a.ActionType != BettingActionType.PostBb).ToArray();

            List<PostflopEntryFacade> filteredPostflopEntries = new List<PostflopEntryFacade>();

            int gtoStartIndent = (int)gameType - gc.IsPlayerIn.Count(p => p != null);

            foreach (PostflopEntry entry in _postflopEntries)
            {
                if (entry.TableSize != (int)gameType)
                    continue;

                BettingAction[] gtoActions = ConvertGtoActions(entry.PreflopActions.Split("_"));

                bool okActionType = gtoActions.Length >= gtoStartIndent;

                if (!okActionType)
                    continue;

                okActionType = gtoActions.Take(gtoStartIndent).All(action => action.ActionType == BettingActionType.Fold);

                if(!okActionType)
                    continue;

                gtoActions = gtoActions.TakeLast(gtoActions.Length - gtoStartIndent).ToArray();

                if (gtoActions.Length != preflopActions.Length)
                    continue;

                for (int i = 0; i < gtoActions.Length; i++)
                {
                    if (gtoActions[i].ActionType != preflopActions[i].ActionType)
                    {
                        okActionType = false;

                        break;
                    }
                }

                if (okActionType)
                    filteredPostflopEntries.Add(new PostflopEntryFacade()
                    {
                        Id = entry.Id,
                        TableSize = entry.TableSize,
                        EffStack = entry.PreflopEffStack,
                        PotSize = entry.PotSize,
                        Actions = gtoActions,
                        RangeOop = entry.RangeOop,
                        RangeIp = entry.RangeIp
                    });
            }

            if (filteredPostflopEntries.Count == 0)
            {
                lock (gc.GtoLock)
                    gc.GtoError = "Unable to get flop tree because there is no suitable preflop GTO strategy";

                return;
            }

            filteredPostflopEntries.Sort(new PostflopEntryEsComparer());

            float destEs = 0;

            float[] ess = filteredPostflopEntries.Select(at => at.EffStack).Distinct().ToArray();

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

            PostflopEntryFacade[] destPostflopEntries = filteredPostflopEntries.Where(a => Math.Abs(a.EffStack - destEs) < 0.001).ToArray();

            List<PostflopEntryFacade> outputKeys = new List<PostflopEntryFacade>(destPostflopEntries);

            for (int i = 0; i < preflopActions.Length; i++)
            {
                BettingAction preflopAction = preflopActions[i];

                if (preflopAction.ActionType != BettingActionType.Raise)
                    continue;

                outputKeys.Sort(new PostflopEntryAmountComparer(i));

                destPostflopEntries = outputKeys.ToArray();

                float prevAmountDelta = float.MaxValue;

                for (int j = 0; j < destPostflopEntries.Length; j++)
                {
                    BettingAction prevAction = null;

                    for (int k = i - 1; k >= preflopActions.Length; k--)
                    {
                        if (preflopActions[k].ActionType == BettingActionType.Raise)
                        {
                            prevAction = preflopActions[k];

                            break;
                        }
                    }

                    float prevActionAmount = (float)(prevAction?.Amount ?? 1);

                    BettingAction prevGtoAction = null;

                    for (int k = i - 1; k >= destPostflopEntries[j].Actions.Length; k--)
                    {
                        if (destPostflopEntries[j].Actions[k].ActionType == BettingActionType.Raise)
                        {
                            prevGtoAction = destPostflopEntries[j].Actions[k];

                            break;
                        }
                    }

                    float prevGtoAmount = (float)(prevGtoAction?.Amount ?? 1);

                    float delta = (float)Math.Abs(destPostflopEntries[j].Actions[i].Amount / prevGtoAmount - preflopActions[i].Amount / prevActionAmount);

                    if (delta < prevAmountDelta)
                    {
                        prevAmountDelta = delta;

                        if (j == destPostflopEntries.Length - 1)
                        {
                            for (int k = 0; k <= j - 1; k++)
                                outputKeys.Remove(destPostflopEntries[k]);
                        }

                        continue;
                    }

                    if (Math.Abs(delta - prevAmountDelta) < 0.001)
                    {
                        for (int k = 0; k <= j - 2; k++)
                            outputKeys.Remove(destPostflopEntries[k]);

                        for (int k = j + 1; k < destPostflopEntries.Length; k++)
                        {
                            float nextDelta = (float)Math.Abs(destPostflopEntries[k].Actions[i].Amount - preflopActions[i].Amount);

                            if (Math.Abs(nextDelta - delta) < 0.001)
                                continue;

                            if (nextDelta > delta)
                            {
                                for (int l = k; l < destPostflopEntries.Length; l++)
                                    outputKeys.Remove(destPostflopEntries[l]);

                                break;
                            }

                            if (nextDelta < delta)
                            {
                                delta = nextDelta;

                                for (int l = 0; l <= k - 1; l++)
                                    outputKeys.Remove(destPostflopEntries[l]);
                            }
                        }

                        break;
                    }

                    for (int k = 0; k <= j - 2; k++)
                        outputKeys.Remove(destPostflopEntries[k]);

                    for (int k = j; k < destPostflopEntries.Length; k++)
                        outputKeys.Remove(destPostflopEntries[k]);

                    break;
                }
            }

            destPostflopEntries = outputKeys.ToArray();

            PostflopEntryFacade matchedPostflopEntry = destPostflopEntries.Last();

            string flopCards = $"{gc.FlopCard1}{gc.FlopCard2}{gc.FlopCard3}";

            PostflopData matchedPostflopData = _gtoDbContext.PostflopData.FirstOrDefault(d => d.FlopMask == Hand.ParseHandUnique(flopCards) && d.PostflopEntryId == matchedPostflopEntry.Id);

            if (matchedPostflopData != null)
            {
                byte[] source = matchedPostflopData.Data;

                byte[] decoded = new byte[source.Length * 20];

                int decodedLength = LZ4Codec.Decode(source, 0, source.Length, decoded, 0, decoded.Length);

                using MemoryStream memoryStream = new MemoryStream(decoded, 0, decodedLength);

                gc.FlopTree = (SolverTree)_binaryFormatter.Deserialize(memoryStream);
            }
            else
            {
                lock (gc.GtoLock)
                    gc.GtoError = "There is no suitable GTO situation in database";
            }

            BettingAction[] ConvertGtoActions(string[] gtoActions)
            {
                BettingAction[] outputActions = new BettingAction[gtoActions.Length];

                for (int i = 0; i < gtoActions.Length; i++)
                {
                    Match match = Regex.Match(gtoActions[i], "^(?<action>[FXCR])\\[(?<amount>\\d*(?:\\.\\d{0,2})?)\\](?<position>[0-5])$");

                    outputActions[i] = new BettingAction(playerRelativePositions[int.Parse(match.Groups["position"].Value)] + 1, 1, double.Parse(match.Groups["amount"].Value,
                        NumberStyles.Any, CultureInfo.InvariantCulture), ConvertGtoActionType(match.Groups["action"].Value), false);
                }

                BettingActionType ConvertGtoActionType(string gtoAction)
                {

                    return gtoAction switch
                    {
                        "F" => BettingActionType.Fold,
                        "X" => BettingActionType.Check,
                        "C" => BettingActionType.Call,
                        "R" => BettingActionType.Raise,
                        _ => BettingActionType.NoAction
                    };
                }

                return outputActions;
            }
        }

        private void SolveTree(GameContext gc)
        {
            int initialButtonPosition = gc.ButtonPosition;

            int initialHoleCardsHash = $"{gc.HoleCards[gc.HeroPosition - 1][0]}{gc.HoleCards[gc.HeroPosition - 1][1]}".GetHashCode();

            SolverTree previousTree = null;
            SolverTree currentTree = null;

            lock (gc.GtoLock)
            {
                gc.IsSolving = true;

                gc.GtoData = null;

                if (gc.Round == 3)
                {
                    previousTree = (SolverTree)gc.FlopTree;
                    currentTree = (SolverTree)gc.TurnTree;

                    gc.TurnTree = null;
                }
                else if (gc.Round == 4)
                {
                    previousTree = (SolverTree)gc.TurnTree;
                    currentTree = (SolverTree)gc.RiverTree;

                    gc.RiverTree = null;
                }
            }

            Task.Factory.StartNew((args) =>
            {
                SolverTree[] trees = (SolverTree[])args;

                SolverTree solverTree = SolveTree(gc, trees[0], trees[1], out string solverError, out int solverRound);

                _solverManager.DumpTree(gc.TableId, $"D:\\Backup\\Projects\\Projects_GG\\GGPoker\\InfoHelper\\InfoHelper\\bin\\x64\\Debug\\net5.0-windows\\{gc.Round}.cfr", SolverTreeDumpMode.Full, 10);

                lock (gc.GtoLock)
                {
                    if (initialButtonPosition == gc.ButtonPosition && initialHoleCardsHash == $"{gc.HoleCards[gc.HeroPosition - 1][0]}{gc.HoleCards[gc.HeroPosition - 1][1]}".GetHashCode())
                    {
                        if (solverTree != null)
                        {
                            if (solverRound == 3)
                                gc.TurnTree = solverTree;
                            else if (solverRound == 4)
                                gc.RiverTree = solverTree;

                            gc.GtoError = solverError;

                            if(solverError == null)
                                ProcessTree(gc);
                        }
                        else
                            gc.GtoError = solverError;
                    }

                    gc.IsSolving = false;
                }
            }, new SolverTree[] { previousTree, currentTree }).ContinueWith(task =>
            {
                SolverExceptionThrown?.Invoke(task.Exception.Flatten());
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private SolverTree SolveTree(GameContext gc, SolverTree previousRoundTree, SolverTree currentRoundTree, out string solverError, out int solverRound)
        {
            try
            {
                long solverId = gc.TableId;

                Dictionary<string, SolverNode> previousRoundTreeNodes = previousRoundTree.Nodes;

                int heroRelativePosition = -1;

                int nextToAct = gc.ButtonPosition + 1;

                if (nextToAct == 7)
                    nextToAct = 1;

                while (true)
                {
                    if (gc.IsPlayerIn[nextToAct - 1] != null && (bool)gc.IsPlayerIn[nextToAct - 1])
                    {
                        heroRelativePosition = nextToAct == gc.HeroPosition ? 0 : 1;

                        break;
                    }

                    nextToAct++;

                    if (nextToAct == 7)
                        nextToAct = 1;
                }

                float[] oopBetsTurn = null;
                float[] oopDonkBetsTurn = null;
                float[] ipBetsTurn = null;

                float[] oopRaisesTurn = null;
                float[] ipRaisesTurn = null;

                float[] oopReraisesTurn = null;
                float[] ipReraisesTurn = null;

                float[] oopBetsRiver = null;
                float[] oopDonkBetsRiver = null;
                float[] ipBetsRiver = null;

                float[] oopRaisesRiver = null;
                float[] ipRaisesRiver = null;

                float[] oopReraisesRiver = null;
                float[] ipReraisesRiver = null;

                if (gc.Round == 3)
                {
                    oopBetsTurn = heroRelativePosition == 0 ? Shared.SolverSizingsInfo.TurnTree.HeroOop.OopBetsTurn : Shared.SolverSizingsInfo.TurnTree.HeroIp.OopBetsTurn;
                    oopDonkBetsTurn = heroRelativePosition == 0 ? Shared.SolverSizingsInfo.TurnTree.HeroOop.OopDonkBetsTurn : Shared.SolverSizingsInfo.TurnTree.HeroIp.OopDonkBetsTurn;
                    ipBetsTurn = heroRelativePosition == 0 ? Shared.SolverSizingsInfo.TurnTree.HeroOop.IpBetsTurn : Shared.SolverSizingsInfo.TurnTree.HeroIp.IpBetsTurn;

                    oopRaisesTurn = heroRelativePosition == 0 ? Shared.SolverSizingsInfo.TurnTree.HeroOop.OopRaisesTurn : Shared.SolverSizingsInfo.TurnTree.HeroIp.OopRaisesTurn;
                    ipRaisesTurn = heroRelativePosition == 0 ? Shared.SolverSizingsInfo.TurnTree.HeroOop.IpRaisesTurn : Shared.SolverSizingsInfo.TurnTree.HeroIp.IpRaisesTurn;

                    oopReraisesTurn = heroRelativePosition == 0 ? Shared.SolverSizingsInfo.TurnTree.HeroOop.OopReraisesTurn : Shared.SolverSizingsInfo.TurnTree.HeroIp.OopReraisesTurn;
                    ipReraisesTurn = heroRelativePosition == 0 ? Shared.SolverSizingsInfo.TurnTree.HeroOop.IpReraisesTurn : Shared.SolverSizingsInfo.TurnTree.HeroIp.IpReraisesTurn;

                    oopBetsRiver = heroRelativePosition == 0 ? Shared.SolverSizingsInfo.TurnTree.HeroOop.OopBetsRiver : Shared.SolverSizingsInfo.TurnTree.HeroIp.OopBetsRiver;
                    oopDonkBetsRiver = heroRelativePosition == 0 ? Shared.SolverSizingsInfo.TurnTree.HeroOop.OopDonkBetsRiver : Shared.SolverSizingsInfo.TurnTree.HeroIp.OopDonkBetsRiver;
                    ipBetsRiver = heroRelativePosition == 0 ? Shared.SolverSizingsInfo.TurnTree.HeroOop.IpBetsRiver : Shared.SolverSizingsInfo.TurnTree.HeroIp.IpBetsRiver;

                    oopRaisesRiver = heroRelativePosition == 0 ? Shared.SolverSizingsInfo.TurnTree.HeroOop.OopRaisesRiver : Shared.SolverSizingsInfo.TurnTree.HeroIp.OopRaisesRiver;
                    ipRaisesRiver = heroRelativePosition == 0 ? Shared.SolverSizingsInfo.TurnTree.HeroOop.IpRaisesRiver : Shared.SolverSizingsInfo.TurnTree.HeroIp.IpRaisesRiver;

                    oopReraisesRiver = heroRelativePosition == 0 ? Shared.SolverSizingsInfo.TurnTree.HeroOop.OopReraisesRiver : Shared.SolverSizingsInfo.TurnTree.HeroIp.OopReraisesRiver;
                    ipReraisesRiver = heroRelativePosition == 0 ? Shared.SolverSizingsInfo.TurnTree.HeroOop.IpReraisesRiver : Shared.SolverSizingsInfo.TurnTree.HeroIp.IpReraisesRiver;
                }
                else if (gc.Round == 4)
                {
                    oopBetsRiver = heroRelativePosition == 0 ? Shared.SolverSizingsInfo.RiverTree.HeroOop.OopBetsRiver : Shared.SolverSizingsInfo.RiverTree.HeroIp.OopBetsRiver;
                    oopDonkBetsRiver = heroRelativePosition == 0 ? Shared.SolverSizingsInfo.RiverTree.HeroOop.OopDonkBetsRiver : Shared.SolverSizingsInfo.RiverTree.HeroIp.OopDonkBetsRiver;
                    ipBetsRiver = heroRelativePosition == 0 ? Shared.SolverSizingsInfo.RiverTree.HeroOop.IpBetsRiver : Shared.SolverSizingsInfo.RiverTree.HeroIp.IpBetsRiver;

                    oopRaisesRiver = heroRelativePosition == 0 ? Shared.SolverSizingsInfo.RiverTree.HeroOop.OopRaisesRiver : Shared.SolverSizingsInfo.RiverTree.HeroIp.OopRaisesRiver;
                    ipRaisesRiver = heroRelativePosition == 0 ? Shared.SolverSizingsInfo.RiverTree.HeroOop.IpRaisesRiver : Shared.SolverSizingsInfo.RiverTree.HeroIp.IpRaisesRiver;

                    oopReraisesRiver = heroRelativePosition == 0 ? Shared.SolverSizingsInfo.RiverTree.HeroOop.OopReraisesRiver : Shared.SolverSizingsInfo.RiverTree.HeroIp.OopReraisesRiver;
                    ipReraisesRiver = heroRelativePosition == 0 ? Shared.SolverSizingsInfo.RiverTree.HeroOop.IpReraisesRiver : Shared.SolverSizingsInfo.RiverTree.HeroIp.IpReraisesRiver;
                }

                int villainPosition = -1;

                for (int i = 0; i < gc.IsPlayerIn.Length; i++)
                {
                    if (gc.IsPlayerIn[i] != null && (bool)gc.IsPlayerIn[i] && i != gc.HeroPosition - 1)
                    {
                        villainPosition = i + 1;

                        break;
                    }
                }

                BettingAction[] prevRoundActions = gc.Actions.Where(a => a.Round == previousRoundTree.Round).ToArray();

                int prevRoundRaiserPosition = prevRoundActions.LastOrDefault(a => a.ActionType is BettingActionType.Bet or BettingActionType.Raise)?.Player ?? -1;

                if (prevRoundRaiserPosition != -1)
                {
                    if (heroRelativePosition == 0)
                        prevRoundRaiserPosition = prevRoundRaiserPosition == gc.HeroPosition ? 0 : 1;
                    else
                        prevRoundRaiserPosition = prevRoundRaiserPosition == gc.HeroPosition ? 1 : 0;
                }

                string board = $"{gc.FlopCard1} {gc.FlopCard2} {gc.FlopCard3} {gc.TurnCard ?? string.Empty} {gc.RiverCard ?? string.Empty}";

                int initRound = gc.Round;

                float heroPrevBets = (float)(gc.Actions.LastOrDefault(a => a.Round == initRound && a.Player == gc.HeroPosition)?.Amount ?? 0);
                float villainPrevBets = (float)(gc.Actions.LastOrDefault(a => a.Round == initRound && a.Player == villainPosition)?.Amount ?? 0);

                int effStackPostflop = (int)(Math.Min(gc.Stacks[gc.HeroPosition - 1].Value + heroPrevBets, gc.Stacks[villainPosition - 1].Value + villainPrevBets) * 10);

                ActionInfo[] ConvertActions(List<BettingAction> actions)
                {
                    ActionInfo[] convertedActions = new ActionInfo[actions.Count()];

                    for (int i = 0; i < actions.Count(); i++)
                    {
                        BettingAction action = actions[i];

                        SolverActionType solverActionType = action.ActionType switch
                        {
                            BettingActionType.Fold => SolverActionType.Fold,
                            BettingActionType.Check => SolverActionType.Check,
                            BettingActionType.Call => SolverActionType.Call,
                            BettingActionType.Bet => SolverActionType.Bet,
                            BettingActionType.Raise => SolverActionType.Raise,
                            _ => SolverActionType.NoAction
                        };

                        convertedActions[i] = new ActionInfo(solverActionType, (int)(action.Amount * 10), action.Round);
                    }

                    return convertedActions;
                }

                PostflopTreeManager postflopTreeManager = new PostflopTreeManager(prevRoundRaiserPosition)
                {
                    Actions = ConvertActions(gc.Actions),
                    Bets = new int[] { 0, 0, (int)gc.RoundPot * 10},
                    EffStackPreflop = (int)(Math.Min(gc.InitialStacks[gc.HeroPosition - 1].Value, gc.InitialStacks[villainPosition - 1].Value) * 10),
                    EffStackPostflop = effStackPostflop,
                    MinBet = 10,
                    CurrentPot = (int)(gc.RoundPot * 10),
                    InitRound = initRound,
                    AllInThreshold = (float)Shared.AllInThreshold / 100,
                    BoardMask = Hand.ParseHand(board),
                    OopBetsTurn = oopBetsTurn,
                    OopDonkBetsTurn = oopDonkBetsTurn,
                    IpBetsTurn = ipBetsTurn,
                    OopRaisesTurn = oopRaisesTurn,
                    IpRaisesTurn = ipRaisesTurn,
                    OopReraisesTurn = oopReraisesTurn,
                    IpReraisesTurn = ipReraisesTurn,
                    OopBetsRiver = oopBetsRiver,
                    OopDonkBetsRiver = oopDonkBetsRiver,
                    IpBetsRiver = ipBetsRiver,
                    OopRaisesRiver = oopRaisesRiver,
                    IpRaisesRiver = ipRaisesRiver,
                    OopReraisesRiver = oopReraisesRiver,
                    IpReraisesRiver = ipReraisesRiver
                };

                SolverTree solverTree = postflopTreeManager.GenerateTree(out string[] lines);

                string oopRange = null;
                string ipRange = null;

                if (currentRoundTree?.OopRange == null || currentRoundTree.IpRange == null)
                {
                    //Searching for entry ranges
                    BettingAction[] prevPrevRoundActions = gc.Actions.Where(a => a.Round < previousRoundTree.Round).ToArray();

                    float[] betsCurrent = new float[6];
                    float[] betsTotal = new float[6];

                    for (int i = 0; i < prevPrevRoundActions.Length; i++)
                    {
                        if (i > 0 && prevPrevRoundActions[i].Round > prevPrevRoundActions[i - 1].Round)
                            betsCurrent = new float[6];

                        betsTotal[prevPrevRoundActions[i].Player - 1] += (float)prevPrevRoundActions[i].Amount - betsCurrent[prevPrevRoundActions[i].Player - 1];

                        betsCurrent[prevPrevRoundActions[i].Player - 1] = (float)prevPrevRoundActions[i].Amount;
                    }

                    betsCurrent = new float[6];

                    SolverNode destNode = null;

                    SolverNode rootNode = previousRoundTree.RootNode;

                    SolverNode nextNode = rootNode;

                    for (int i = 0; i < prevRoundActions.Length; i++)
                    {
                        BettingAction action = prevRoundActions[i];

                        float currentPot = betsTotal.Sum();

                        if (action.ActionType == BettingActionType.Check)
                        {
                            SolverNode checkNode = nextNode.ChildNodes?.FirstOrDefault(n => n.ActionType == SolverActionType.Check);

                            if (checkNode == null)
                            {
                                nextNode = null;

                                break;
                            }

                            nextNode = checkNode;
                        }
                        else if (action.ActionType == BettingActionType.Call)
                        {
                            SolverNode callNode = nextNode.ChildNodes?.FirstOrDefault(n => n.ActionType == SolverActionType.Call);

                            if (callNode == null)
                            {
                                nextNode = null;

                                break;
                            }

                            nextNode = callNode;
                        }
                        else if (action.ActionType == BettingActionType.Bet)
                        {
                            if (nextNode.ChildNodes == null)
                            {
                                nextNode = null;

                                break;
                            }

                            SolverNode betNode = null;

                            SolverNode[] betNodes = nextNode.ChildNodes.Where(n => n.ActionType == SolverActionType.Bet).ToArray();

                            float prevAmountDelta = float.MaxValue;

                            for (int j = 0; j < betNodes.Length; j++)
                            {
                                float delta = (float)Math.Abs(action.Amount / currentPot - (float)betNodes[j].Amount / betNodes[j].CurrentPot);

                                if (delta < prevAmountDelta)
                                {
                                    prevAmountDelta = delta;

                                    if (j == betNodes.Length - 1)
                                        betNode = betNodes[j];

                                    continue;
                                }

                                betNode = betNodes[j - (Math.Abs(delta - prevAmountDelta) < 0.001 ? 0 : 1)];

                                break;
                            }

                            if (betNode == null)
                            {
                                nextNode = null;

                                break;
                            }

                            nextNode = betNode;
                        }
                        else if (action.ActionType == BettingActionType.Raise)
                        {
                            if (nextNode.ChildNodes == null)
                            {
                                nextNode = null;

                                break;
                            }

                            SolverNode raiseNode = null;

                            SolverNode[] raiseNodes = nextNode.ChildNodes.Where(n => n.ActionType == SolverActionType.Raise).ToArray();

                            float prevActionAmount = 0;

                            for (int j = i - 1; j >= 0; j--)
                            {
                                if (prevRoundActions[j].Player == action.Player)
                                {
                                    prevActionAmount = (float)prevRoundActions[j].Amount;

                                    break;
                                }
                            }

                            SolverNode prevPlayerNode = null;

                            SolverNode nextPlayerNode = raiseNodes.Length > 0 ? raiseNodes[0].ParentNode : null;

                            if (nextPlayerNode != null)
                            {
                                while (true)
                                {
                                    if (nextPlayerNode.NodeType == NodeType.Root)
                                        break;

                                    if (nextPlayerNode.Position == raiseNodes[0].Position)
                                    {
                                        prevPlayerNode = nextPlayerNode;

                                        break;
                                    }

                                    nextPlayerNode = nextPlayerNode.ParentNode;
                                }
                            }

                            float prevAmountDelta = float.MaxValue;

                            for (int j = 0; j < raiseNodes.Length; j++)
                            {
                                float delta = (float)Math.Abs((action.Amount - prevActionAmount) / currentPot - (float)(raiseNodes[j].Amount - (prevPlayerNode?.Amount ?? 0)) / raiseNodes[j].CurrentPot);

                                if (delta < prevAmountDelta)
                                {
                                    prevAmountDelta = delta;

                                    if (j == raiseNodes.Length - 1)
                                        raiseNode = raiseNodes[j];

                                    continue;
                                }

                                raiseNode = raiseNodes[j - (Math.Abs(delta - prevAmountDelta) < 0.001 ? 0 : 1)];

                                break;
                            }

                            if (raiseNode == null)
                            {
                                nextNode = null;

                                break;
                            }

                            nextNode = raiseNode;
                        }

                        betsTotal[action.Player - 1] += (float)action.Amount - betsCurrent[action.Player - 1];

                        betsCurrent[action.Player - 1] = (float)action.Amount;
                    }

                    destNode = nextNode;

                    if (destNode == null)
                    {
                        solverError = "Sequence of actions from previous round was not found";

                        solverRound = solverTree.Round;

                        return null;
                    }

                    SolverNode oopNode = destNode.Position == 0 ? destNode : destNode.ParentNode;
                    SolverNode ipNode = destNode.Position == 0 ? destNode.ParentNode : destNode;

                    float[] rangeOop = (oopNode.Ranges[0] ?? _solverManager.ShowRange(solverId, GtoPlayerPosition.OOP, oopNode.Id, SolverCommandTimeout)).Values;
                    float[] rangeIp = (ipNode.Ranges[1] ?? _solverManager.ShowRange(solverId, GtoPlayerPosition.IP, ipNode.Id, SolverCommandTimeout)).Values;

                    float rangeSumOop = rangeOop.Sum();
                    float rangeSumIp = rangeIp.Sum();

                    float initialEntryRangeSumOop = (rootNode.Ranges[0] ?? _solverManager.ShowRange(solverId, GtoPlayerPosition.OOP, rootNode.Id, SolverCommandTimeout)).Values.Sum();
                    float initialEntryRangeSumIp = (rootNode.Ranges[1] ?? _solverManager.ShowRange(solverId, GtoPlayerPosition.IP, rootNode.Id, SolverCommandTimeout)).Values.Sum();

                    string prevRoundActionsStr = string.Empty;

                    foreach (BettingAction postflopAction in prevRoundActions)
                    {
                        switch (postflopAction.ActionType)
                        {
                            case BettingActionType.Check:
                                prevRoundActionsStr += "x";
                                break;

                            case BettingActionType.Call:
                                prevRoundActionsStr += "c";
                                break;

                            case BettingActionType.Bet:
                                prevRoundActionsStr += "b";
                                break;

                            case BettingActionType.Raise:
                                prevRoundActionsStr += "r";
                                break;
                        }
                    }

                    float[] GetNodeRatios(string nodeId)
                    {
                        string[] actionParts = nodeId.Split(':');

                        List<float> ratios = new List<float>();

                        string newNodeId = string.Empty;

                        for (int i = 0; i < actionParts.Length; i++)
                        {
                            string actionPart = actionParts[i];

                            newNodeId += $"{(i == 0 ? string.Empty : ":")}{actionPart}";

                            if (Regex.IsMatch(actionPart, @"^b"))
                            {
                                if (!previousRoundTreeNodes.ContainsKey(newNodeId)) return null;

                                SolverNode node = previousRoundTreeNodes[newNodeId];

                                ratios.Add((float)node.Amount / node.CurrentPot);
                            }
                        }

                        return ratios.ToArray();
                    }

                    SolverNodeInfo[] GetMatchedNodes(bool inPosition)
                    {
                        string actionsPositionKey = $"{prevRoundActionsStr}_{(inPosition ? "ip" : "oop")}";

                        if (!Common.SupplementalRanges.ContainsKey(actionsPositionKey)) return new SolverNodeInfo[] { };

                        Dictionary<string, int> supplementalRanges = Common.SupplementalRanges[actionsPositionKey];

                        string matchedNodeId = (inPosition ? ipNode : oopNode).Id;

                        float[] matchedNodeIdRatios = GetNodeRatios(matchedNodeId);

                        if (matchedNodeIdRatios == null) return new SolverNodeInfo[] { };

                        List<SolverNodeInfo> solverNodes = new List<SolverNodeInfo>();

                        foreach (var rangeKv in supplementalRanges)
                        {
                            string nodesPattern = @"r:0";

                            string[] actionParts = rangeKv.Key.Split(':');

                            foreach (var actionPart in actionParts)
                            {
                                if (Regex.IsMatch(actionPart, @"(x)|(c)"))
                                    nodesPattern += @":c";
                                else if (Regex.IsMatch(actionPart, @"f"))
                                    nodesPattern += @":f";
                                else if (Regex.IsMatch(actionPart, @"(b)|(r)")) nodesPattern += @":b\d{1,4}";
                            }

                            foreach (var nodeKv in previousRoundTreeNodes)
                            {
                                if (Regex.IsMatch(nodeKv.Key, $"^{nodesPattern}$"))
                                {
                                    SolverNode primarilyMatchedNode = nodeKv.Value;

                                    float[] primarilyMatchedNodeRatios = GetNodeRatios(nodeKv.Key);

                                    if (primarilyMatchedNodeRatios == null) continue;

                                    bool isNodeOk = true;

                                    int betRaiseNodesCounter = 0;

                                    foreach (string actionPart in actionParts)
                                    {
                                        if (!Regex.IsMatch(actionPart, @"(b)|(r)")) continue;

                                        if (actionPart.Contains("^amt"))
                                        {
                                            if (Math.Abs(primarilyMatchedNodeRatios[betRaiseNodesCounter] - matchedNodeIdRatios[betRaiseNodesCounter]) < 0.001)
                                            {
                                                isNodeOk = false;

                                                break;
                                            }

                                            betRaiseNodesCounter++;
                                        }
                                        else if (actionPart.Contains("amt"))
                                        {
                                            if (Math.Abs(primarilyMatchedNodeRatios[betRaiseNodesCounter] - matchedNodeIdRatios[betRaiseNodesCounter]) > 0.001)
                                            {
                                                isNodeOk = false;

                                                break;
                                            }

                                            betRaiseNodesCounter++;
                                        }
                                    }

                                    if (isNodeOk) solverNodes.Add(new SolverNodeInfo(primarilyMatchedNode, rangeKv.Value));
                                }
                            }
                        }

                        return solverNodes.ToArray();
                    }

                    void SupplementRange(SolverNodeInfo[] nodeInfos, float[] range, float initEntrySum)
                    {
                        float initSum = range.Sum();

                        float destSum = initEntrySum * Shared.MinLineFrequency / 100;

                        float curSum = initSum;

                        int matchedNodesCounter;

                        for (int i = 1; i < nodeInfos.Length; i += matchedNodesCounter)
                        {
                            matchedNodesCounter = 1;

                            float[] nextRange = (nodeInfos[i].SolverNode.Ranges[nodeInfos[i].SolverNode.Position] ?? _solverManager.ShowRange(solverId,
                                                     nodeInfos[i].SolverNode.Position == 1 ? GtoPlayerPosition.IP : GtoPlayerPosition.OOP, nodeInfos[i].SolverNode.Id, SolverCommandTimeout)).Values.ToArray();

                            if (i < nodeInfos.Length - 1)
                            {
                                for (int j = i + 1; j < nodeInfos.Length; j++)
                                {
                                    if (nodeInfos[i].Priority == nodeInfos[j].Priority)
                                    {
                                        float[] tempRange = (nodeInfos[j].SolverNode.Ranges[nodeInfos[j].SolverNode.Position] ?? _solverManager.ShowRange(solverId,
                                                                 nodeInfos[j].SolverNode.Position == 1 ? GtoPlayerPosition.IP : GtoPlayerPosition.OOP, nodeInfos[j].SolverNode.Id, SolverCommandTimeout)).Values;

                                        for (int k = 0; k < nextRange.Length; k++) nextRange[k] += tempRange[k];

                                        matchedNodesCounter++;

                                        continue;
                                    }

                                    break;
                                }
                            }

                            float nextRangeSum = nextRange.Sum();

                            if (curSum + nextRangeSum < destSum)
                            {
                                for (int j = 0; j < range.Length; j++) range[j] += nextRange[j];

                                curSum += nextRangeSum;

                                continue;
                            }

                            int skips = nextRange.Count(r => r == 0);

                            if (skips == nextRange.Length) continue;

                            while (true)
                            {
                                float minValue = nextRange.Where(r => r > 0).Min();

                                for (int j = 0; j < range.Length; j++)
                                {
                                    if (nextRange[j] == 0) continue;

                                    curSum += minValue;

                                    range[j] += minValue;

                                    if (nextRange[j] > minValue)
                                        nextRange[j] -= minValue;
                                    else
                                    {
                                        nextRange[j] = 0;

                                        skips++;
                                    }
                                }

                                if (skips == nextRange.Length || curSum >= destSum) break;
                            }

                            break;
                        }
                    }

                    if (rangeSumOop * 100 / initialEntryRangeSumOop < Shared.MinLineFrequency)
                        SupplementRange(GetMatchedNodes(false), rangeOop, initialEntryRangeSumOop);

                    if (rangeSumIp * 100 / initialEntryRangeSumIp < Shared.MinLineFrequency)
                        SupplementRange(GetMatchedNodes(true), rangeIp, initialEntryRangeSumIp);

                    //Normalizing ranges
                    float ratioOop = 1 / rangeOop.Max();
                    float ratioIp = 1 / rangeIp.Max();

                    for (int i = 0; i < rangeOop.Length; i++)
                    {
                        rangeOop[i] *= ratioOop;

                        rangeIp[i] *= ratioIp;
                    }

                    string hc1 = gc.HoleCards[gc.HeroPosition - 1][0];
                    string hc2 = gc.HoleCards[gc.HeroPosition - 1][1];

                    ulong pocketMask = Hand.ParseHand($"{hc1}{hc2}");

                    ulong[] oopHands = Common.PioRangesMask.ToArray();
                    ulong[] ipHands = Common.PioRangesMask.ToArray();

                    ulong currentFlopMask = Hand.ParseHand($"{gc.FlopCard1} {gc.FlopCard2} {gc.FlopCard3}");

                    if (previousRoundTree.Round == 2)
                    {
                        for (int i = 0; i < oopHands.Length; i++)
                        {
                            oopHands[i] = Hand.ConvertHandFromGto(previousRoundTree.Board, currentFlopMask, oopHands[i]);
                            ipHands[i] = Hand.ConvertHandFromGto(previousRoundTree.Board, currentFlopMask, ipHands[i]);
                        }
                    }

                    ulong currentBoardMask = currentFlopMask | Hand.ParseHand(gc.TurnCard) | (gc.Round == 4 ? Hand.ParseHand(gc.RiverCard) : 0ul);

                    for (int i = 0; i < oopHands.Length; i++)
                    {
                        if ((oopHands[i] & currentBoardMask) != 0)
                            rangeOop[i] = 0;

                        if ((ipHands[i] & currentBoardMask) != 0)
                            rangeIp[i] = 0;
                    }

                    if (heroRelativePosition == 0)
                    {
                        int pocketIndex = Array.IndexOf(oopHands, pocketMask);

                        if (rangeOop[pocketIndex] < 1E-5)
                            rangeOop[pocketIndex] = rangeOop.Average();
                    }
                    else
                    {
                        int pocketIndex = Array.IndexOf(ipHands, pocketMask);

                        if (rangeIp[pocketIndex] < 1E-5)
                            rangeIp[pocketIndex] = rangeIp.Average();
                    }

                    if (rangeOop.Sum() < 1E-3)
                    {
                        solverError = "Oop range doesn't contain enough hands to solve";

                        solverRound = solverTree.Round;

                        return null;
                    }

                    if (rangeIp.Sum() < 1E-3)
                    {
                        solverError = "Ip range doesn't contain enough hands to solve";

                        solverRound = solverTree.Round;

                        return null;
                    }

                    string[] oopHandsStr = new string[oopHands.Length];
                    string[] ipHandsStr = new string[ipHands.Length];

                    int counter = 0;
                    foreach (ulong hand in oopHands)
                    {
                        int handIndex = Array.IndexOf(Common.PioRangesMask, hand);

                        oopHandsStr[handIndex] = rangeOop[counter].ToString("G9", CultureInfo.InvariantCulture);
                        ipHandsStr[handIndex] = rangeIp[counter++].ToString("G9", CultureInfo.InvariantCulture);
                    }

                    oopRange = string.Join(" ", oopHandsStr).TrimEnd(' ');
                    ipRange = string.Join(" ", ipHandsStr).TrimEnd(' ');
                }
                else
                {
                    oopRange = currentRoundTree.OopRange;
                    ipRange = currentRoundTree.IpRange;
                }

                solverTree.OopRange = oopRange;
                solverTree.IpRange = ipRange;

                float accuracy = (float)(gc.Round == 3 ? Shared.TurnSolverAccuracy : Shared.RiverSolverAccuracy) * solverTree.InitialPot / 100;

                _solverManager.SetThreads(solverId, 0, 0);

                _solverManager.SetRange(solverId, GtoPlayerPosition.OOP, oopRange, 0);

                _solverManager.SetRange(solverId, GtoPlayerPosition.IP, ipRange, 0);

                _solverManager.SetBoard(solverId, board, 0);

                _solverManager.SetEffectiveStack(solverId, solverTree.InitialStack, 0);

                _solverManager.SetIsomorphism(solverId, true, false, 0);

                _solverManager.SetPot(solverId, solverTree.InitialPot, 0);

                _solverManager.SetAccuracy(solverId, accuracy, SolverAccuracyType.Chips, 0);

                _solverManager.ClearLines(solverId, 0);

                foreach (string line in lines)
                    _solverManager.AddLine(solverId, line, 0);

                _solverManager.BuildTree(solverId, SolverCommandTimeout);

                _solverManager.SolveTreeWithTimeout(solverId, gc.Round == 3 ? Shared.TurnSolverDuration : Shared.RiverSolverDuration, false, SolverCommandExtTimeout);

                solverTree.Accuracy = _solverManager.GetAccuracy(solverId, SolverCommandTimeout) * 100 / solverTree.InitialPot;

                solverError = null;

                solverRound = solverTree.Round;

                return solverTree;
            }
            catch (Exception ex)
            {
                SolverExceptionThrown?.Invoke(ex);

                solverError = null;

                solverRound = -1;

                return null;
            }
        }

        #region Private classes

        private class PostflopEntryFacade
        {
            public int Id { get; init; }

            public int TableSize { get; init; }

            public float EffStack { get; init; }

            public float PotSize { get; init; }

            public BettingAction[] Actions { get; init; }

            public string RangeOop { get; init; }

            public string RangeIp { get; init; }
        }

        private class PostflopEntryEsComparer : IComparer<PostflopEntryFacade>
        {
            public int Compare(PostflopEntryFacade x, PostflopEntryFacade y)
            {
                if (x.EffStack < y.EffStack)
                    return -1;

                return x.EffStack > y.EffStack ? 1 : 0;
            }
        }

        private class PostflopEntryAmountComparer : IComparer<PostflopEntryFacade>
        {
            private readonly int _index;

            public PostflopEntryAmountComparer(int index)
            {
                _index = index;
            }

            public int Compare(PostflopEntryFacade x, PostflopEntryFacade y)
            {
                if (x.Actions[_index].Amount < y.Actions[_index].Amount)
                    return -1;

                if (x.Actions[_index].Amount > y.Actions[_index].Amount)
                    return 1;

                return 0;
            }
        }

        private class SolverNodeInfo
        {
            public SolverNode SolverNode { get; }

            public int Priority { get; }

            public SolverNodeInfo(SolverNode node, int priority)
            {
                SolverNode = node;

                Priority = priority;
            }
        }

        #endregion
    }
}
