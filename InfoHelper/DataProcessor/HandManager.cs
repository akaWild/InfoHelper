using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HoldemHand;
using InfoHelper.StatsEntities;
using InfoHelper.Utils;
using InfoHelper.ViewModel.DataEntities;

namespace InfoHelper.DataProcessor
{
    public static class HandManager
    {
        private static readonly int Hearts = 2;
        private static readonly int Diamonds = 1;
        private static readonly int Clubs = 0;
        private static readonly int Spades = 3;

        private static readonly int SpadeOffset = 13 * Spades;
        private static readonly int ClubOffset = 13 * Clubs;
        private static readonly int DiamondOffset = 13 * Diamonds;
        private static readonly int HeartOffset = 13 * Hearts;

        public static double CalculateHandEquity(ulong pocketMask, ulong boardMask)
        {
            long wins = 0, ties = 0, count = 0;

            foreach (ulong oppMask in Hand.Hands(0UL, boardMask | pocketMask, 2))
            {
                foreach (ulong bm in Hand.Hands(boardMask, pocketMask | oppMask, 5))
                {
                    uint pocketHandVal = Hand.Evaluate(pocketMask | bm, 7);
                    uint oppHandVal = Hand.Evaluate(oppMask | bm, 7);

                    if (pocketHandVal > oppHandVal)
                        wins++;
                    else if (pocketHandVal == oppHandVal)
                        ties++;

                    count++;
                }
            }

            return (wins + (ties / 2.0)) * 100.0 / count;
        }

        public static HandType GetMadeHandType(ulong pocketMask, ulong boardMask)
        {
            bool isMadeHand = IsMadeHand(pocketMask, boardMask), isDrawHand = IsDrawHand(pocketMask, boardMask);

            return (isMadeHand, isDrawHand) switch
            {
                (true, false) => HandType.MadeHand,
                (false, true) => HandType.DrawHand,
                (true, true) => HandType.ComboHand,
                (false, false) => HandType.MadeHand
            };
        }

        private static bool IsMadeHand(ulong pocketMask, ulong boardMask)
        {
            int nCards = Hand.BitCount(boardMask);

            return nCards switch
            {
                3 => IsMadeHandFlop(pocketMask, boardMask),
                4 => IsMadeHandTurn(pocketMask, boardMask),
                _ => IsMadeHandRiver(pocketMask, boardMask)
            };
        }

        private static bool IsDrawHand(ulong pocketMask, ulong boardMask)
        {
            int nCards = Hand.BitCount(boardMask);

            if (nCards == 5)
                return false;

            bool isFd = Hand.IsFlushDraw(pocketMask, boardMask, 0ul);
            bool isSd = Hand.IsOpenEndedStraightDraw(pocketMask, boardMask, 0ul);
            bool isGs = Hand.IsGutShotStraightDraw(pocketMask, boardMask, 0ul);

            return isFd || isSd || isGs;
        }

        private static bool IsMadeHandFlop(ulong pocketMask, ulong boardMask)
        {
            Hand.HandTypes handType = Hand.EvaluateType(boardMask | pocketMask);

            uint scBoard = (uint)((boardMask >> (ClubOffset)) & 0x1fffUL);
            uint sdBoard = (uint)((boardMask >> (DiamondOffset)) & 0x1fffUL);
            uint shBoard = (uint)((boardMask >> (HeartOffset)) & 0x1fffUL);
            uint ssBoard = (uint)((boardMask >> (SpadeOffset)) & 0x1fffUL);

            uint ranksBoard = scBoard | sdBoard | shBoard | ssBoard;
            uint nRanksBoard = (uint)Hand.BitCount(ranksBoard);
            uint nDupsBoard = 3 - nRanksBoard;

            //Все три карты флопа одинакового достоинства
            if (nDupsBoard == 2)
                return handType is Hand.HandTypes.FourOfAKind or Hand.HandTypes.FullHouse;

            switch (handType)
            {
                case Hand.HandTypes.StraightFlush:
                case Hand.HandTypes.FourOfAKind:
                case Hand.HandTypes.FullHouse:
                case Hand.HandTypes.Flush:
                case Hand.HandTypes.Straight:
                case Hand.HandTypes.Trips:
                case Hand.HandTypes.TwoPair:
                    return true;
                case Hand.HandTypes.Pair:
                    return nDupsBoard == 0;
                default:
                    return false;
            }
        }

        private static bool IsMadeHandTurn(ulong pocketMask, ulong boardMask)
        {
            Hand.HandTypes handType = Hand.EvaluateType(boardMask | pocketMask);

            uint scBoard = (uint)((boardMask >> (ClubOffset)) & 0x1fffUL);
            uint sdBoard = (uint)((boardMask >> (DiamondOffset)) & 0x1fffUL);
            uint shBoard = (uint)((boardMask >> (HeartOffset)) & 0x1fffUL);
            uint ssBoard = (uint)((boardMask >> (SpadeOffset)) & 0x1fffUL);

            uint ranksBoard = scBoard | sdBoard | shBoard | ssBoard;
            uint nRanksBoard = (uint)Hand.BitCount(ranksBoard);
            uint nDupsBoard = 4 - nRanksBoard;

            uint[] suitsBoard = new uint[] { scBoard, sdBoard, shBoard, ssBoard };

            uint scHero = (uint)((pocketMask >> (ClubOffset)) & 0x1fffUL);
            uint sdHero = (uint)((pocketMask >> (DiamondOffset)) & 0x1fffUL);
            uint shHero = (uint)((pocketMask >> (HeartOffset)) & 0x1fffUL);
            uint ssHero = (uint)((pocketMask >> (SpadeOffset)) & 0x1fffUL);

            uint ranksHero = scHero | sdHero | shHero | ssHero;
            uint nRanksHero = (uint)Hand.BitCount(ranksHero);

            //Все четыре карты тёрна одинакового достоинства
            if (nDupsBoard == 3)
                return false;

            int[] dupsCountBoard = new int[13] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            for (int i = 0; i <= 12; i++)
            {
                int shift = 1 << i;

                foreach (uint suit in suitsBoard)
                {
                    if ((shift & suit) != 0)
                        dupsCountBoard[i]++;
                }
            }

            //3 карты терна одинаковые
            if (dupsCountBoard.Contains(3))
            {
                switch (handType)
                {
                    case Hand.HandTypes.FourOfAKind:
                    case Hand.HandTypes.FullHouse:
                        return true;
                    default:
                        return false;
                }
            }

            //Дважды спаренная доска
            if (dupsCountBoard.Count(d => d == 2) == 2)
            {
                int[] boardNumbers = SplitBits(ranksBoard);

                switch (handType)
                {
                    case Hand.HandTypes.FourOfAKind:
                    case Hand.HandTypes.FullHouse:
                        return true;
                }

                if (nRanksHero == 1)
                    return ranksHero > boardNumbers[0];

                return false;
            }

            switch (handType)
            {
                case Hand.HandTypes.StraightFlush:
                case Hand.HandTypes.FourOfAKind:
                case Hand.HandTypes.FullHouse:
                case Hand.HandTypes.Flush:
                case Hand.HandTypes.Straight:
                case Hand.HandTypes.Trips:
                case Hand.HandTypes.TwoPair:
                    return true;
                case Hand.HandTypes.Pair:
                    return nDupsBoard == 0;
                default:
                    return false;
            }
        }

        private static bool IsMadeHandRiver(ulong pocketMask, ulong boardMask)
        {
            Hand.HandTypes handType = Hand.EvaluateType(boardMask | pocketMask);

            uint scBoard = (uint)((boardMask >> (ClubOffset)) & 0x1fffUL);
            uint sdBoard = (uint)((boardMask >> (DiamondOffset)) & 0x1fffUL);
            uint shBoard = (uint)((boardMask >> (HeartOffset)) & 0x1fffUL);
            uint ssBoard = (uint)((boardMask >> (SpadeOffset)) & 0x1fffUL);

            uint ranksBoard = scBoard | sdBoard | shBoard | ssBoard;

            uint[] suitsBoard = new uint[] { scBoard, sdBoard, shBoard, ssBoard };

            uint scHero = (uint)((pocketMask >> (ClubOffset)) & 0x1fffUL);
            uint sdHero = (uint)((pocketMask >> (DiamondOffset)) & 0x1fffUL);
            uint shHero = (uint)((pocketMask >> (HeartOffset)) & 0x1fffUL);
            uint ssHero = (uint)((pocketMask >> (SpadeOffset)) & 0x1fffUL);

            uint ranksHero = scHero | sdHero | shHero | ssHero;

            int[] dupsCountBoard = new int[13] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            for (int i = 0; i <= 12; i++)
            {
                int shift = 1 << i;

                foreach (uint suit in suitsBoard)
                {
                    if ((shift & suit) != 0)
                        dupsCountBoard[i]++;
                }
            }

            int[] boardNumbers = SplitBits(ranksBoard);

            //4 карты на ривере одинаковые
            if (dupsCountBoard.Contains(4))
                return false;

            if (dupsCountBoard.Contains(3))
            {
                int tripleCard = 1 << Array.IndexOf(dupsCountBoard, 3);

                //Доска имеет вид XXXYY
                if (dupsCountBoard.Contains(2))
                {
                    int doubleCard = 1 << Array.IndexOf(dupsCountBoard, 2);

                    if (handType == Hand.HandTypes.FourOfAKind)
                        return true;

                    //XXX > YY
                    if (doubleCard > tripleCard)
                    {
                        if (((ranksHero & doubleCard) != 0) || (Hand.BitCount(ranksHero) == 1 && ranksHero > doubleCard))
                            return true;

                        return false;
                    }

                    return (Hand.BitCount(ranksHero) == 1 && ranksHero > tripleCard) || (Hand.BitCount(ranksHero) == 1 && ranksHero > doubleCard);
                }

                switch (handType)
                {
                    case Hand.HandTypes.StraightFlush:
                    case Hand.HandTypes.FourOfAKind:
                    case Hand.HandTypes.FullHouse:
                    case Hand.HandTypes.Flush:
                    case Hand.HandTypes.Straight:
                        return true;
                    default:
                        return false;
                }
            }

            //Дважды спаренная доска
            if (dupsCountBoard.Count(d => d == 2) == 2)
            {
                int singleCard = 1 << Array.IndexOf(dupsCountBoard, 1);

                switch (handType)
                {
                    case Hand.HandTypes.StraightFlush:
                    case Hand.HandTypes.FourOfAKind:
                    case Hand.HandTypes.FullHouse:
                    case Hand.HandTypes.Flush:
                    case Hand.HandTypes.Straight:
                        return true;
                }

                if (singleCard == boardNumbers[2])
                {
                    if (Hand.BitCount(ranksHero) == 1)
                        return ranksHero > boardNumbers[0];

                    return (ranksHero & singleCard) != 0;
                }

                if (singleCard == boardNumbers[1])
                {
                    if (Hand.BitCount(ranksHero) == 1)
                        return ranksHero > boardNumbers[0];

                    return (ranksHero & singleCard) != 0;
                }

                if (Hand.BitCount(ranksHero) == 1)
                    return ranksHero > boardNumbers[1];

                return false;
            }

            //монотонная доска
            if (suitsBoard.Any(s => Hand.BitCount(s) == 5))
            {
                if (handType == Hand.HandTypes.StraightFlush)
                    return true;

                if (Hand.BitCount(scBoard) == 5)
                    return scHero >= boardNumbers[0];

                if (Hand.BitCount(ssBoard) == 5)
                    return ssHero >= boardNumbers[0];

                if (Hand.BitCount(shBoard) == 5)
                    return shHero >= boardNumbers[0];

                return sdHero >= boardNumbers[0];
            }

            //Спаренная доска
            if (dupsCountBoard.Count(d => d == 2) == 1)
            {
                switch (handType)
                {
                    case Hand.HandTypes.StraightFlush:
                    case Hand.HandTypes.FourOfAKind:
                    case Hand.HandTypes.FullHouse:
                    case Hand.HandTypes.Flush:
                    case Hand.HandTypes.Straight:
                    case Hand.HandTypes.Trips:
                    case Hand.HandTypes.TwoPair:
                        return true;
                    default:
                        return false;
                }
            }

            switch (handType)
            {
                case Hand.HandTypes.StraightFlush:
                case Hand.HandTypes.Flush:
                case Hand.HandTypes.Straight:
                case Hand.HandTypes.Trips:
                case Hand.HandTypes.TwoPair:
                case Hand.HandTypes.Pair:
                    return true;
                default:
                    return false;
            }
        }

        private static int[] SplitBits(uint mask)
        {
            List<int> numbers = new List<int>();

            for (int i = 0; i < 32; i++)
            {
                int number = 1 << i;

                if ((number & mask) != 0)
                    numbers.Add(number);
            }

            return numbers.ToArray();
        }
    }
}
