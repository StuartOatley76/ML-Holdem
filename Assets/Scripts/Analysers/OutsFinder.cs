using System;
using System.Collections;
using Unity.Collections;
using Unity.Jobs;
using Utils.ExtentionMethods;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using UnityEngine;

/// <summary>
/// Event args to take in information needed to calculate the number of outs
/// </summary>
public class FindOutsEventArgs : EventArgs {
    public Pocket Pocket { get; private set; }
    public Board Board { get; private set; }

    public Hand Hand { get; private set; }

    public FindOutsEventArgs(Pocket pocket, Board board, Hand hand) {
        Pocket = pocket;
        Board = board;
        Hand = hand;
    }
}

/// <summary>
/// Event args to pass out the number of outs
/// </summary>
public class OutsEventArgs : EventArgs {
    public int Outs { get; private set; }

    public OutsEventArgs(int outs) {
        Outs = outs;
    }
}

/// <summary>
/// Analyser to find the number of outs given a pocket and a board
/// </summary>
public class OutsFinder : Analyser<ulong, int, FindOutsEventArgs, OutsEventArgs> {

    protected override IEnumerator RunAnalysisJob(FindOutsEventArgs e) {

        ulong hashcode = CalculateHashCode(e);
        
        if (lookup.ContainsKey(hashcode)) {
            lookup.TryGetValue(hashcode, out int numberOfOuts);
            OutsEventArgs outsEA = new OutsEventArgs(numberOfOuts);
            AnalysisCompleted(outsEA);
            yield break;
        }

        NativeArray<Card> allTheCards = new NativeArray<Card>(e.Pocket.Cards.Length + e.Board.Cards.Length, Allocator.TempJob);
        NativeHashMap<int, int> allDuplicates = FindDuplicates(allTheCards);
        NativeHashMap<int, int> allSuits = FindSuits(allTheCards);
        FindOuts findOuts = new FindOuts {
            outs = new NativeHashSet<Card>(52, Allocator.TempJob),
            Pocket = new NativeArray<Card>(e.Pocket.Cards, Allocator.TempJob),
            Board = new NativeArray<Card>(e.Board.Cards, Allocator.TempJob),
            BestHand = e.Hand.handRank,
            IsALowStraight = e.Hand.AceLowStraight,
            allCards = allTheCards,
            //duplicates = allDuplicates,
            duplicatesEnumerator = allDuplicates.GetEnumerator(),
            //suits = allSuits,
            suitsEnumerator = allSuits.GetEnumerator()
           
        };

        JobHandle job = findOuts.Schedule();

        yield return new WaitUntil(()=>job.IsCompleted);

        job.Complete();

        OutsEventArgs outs = new OutsEventArgs(findOuts.NumberOfOuts);

        lookup.Add(hashcode, outs.Outs);

        findOuts.Pocket.Dispose();
        findOuts.Board.Dispose();
        findOuts.outs.Dispose();
        allDuplicates.Dispose();
        allSuits.Dispose();
        allTheCards.Dispose();

        AnalysisCompleted(outs);

    }

    private NativeHashMap<int, int> FindSuits(NativeArray<Card> cards) {
        NativeHashMap<int, int> cardSuits = new NativeHashMap<int, int>(cards.Length, Allocator.TempJob);
        for (int i = 0; i < cards.Length; i++) {
            int suit = (int)cards[i].Suit;
            if (!cardSuits.ContainsKey(suit)) {
                cardSuits.Add(suit, 1);
                continue;
            }
            cardSuits[suit]++;
        }
        return cardSuits;
    }

    /// <summary>
    /// Finds duplicate ranks within an array of cards
    /// </summary>
    /// <param name="cards">The array to check</param>
    /// <returns><int, int>Hashmap where the first int is the rank cast to an int, and the second is the quantity of that rank</returns>
    private NativeHashMap<int, int> FindDuplicates(NativeArray<Card> cards) {
        NativeHashMap<int, int> foundDuplicates = new NativeHashMap<int, int>(cards.Length / 2, Allocator.TempJob);
        for (int i = 1; i < cards.Length; i++) {
            if (cards[i - 1].Rank == cards[i].Rank) {
                foundDuplicates.TryAdd((int)cards[i].Rank, 1);
                foundDuplicates[(int)cards[i].Rank]++;
            }
        }
        NativeArray<int> singles = foundDuplicates.FindValue(1);
        foreach (int key in singles) {
            foundDuplicates.Remove(key);
        }
        return foundDuplicates;
    }

    /// <summary>
    /// Creates a hashcode for the cards in the pocket and cards on the board.
    /// Pocket and board cards need to be kept seperate as the position of a card changes whether another card is an out (eg, if an ace is in the pocket then another ace improves a hand more than it improves everyone elses,
    /// but if the existing ace is on the board then it doesn't)
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    private ulong CalculateHashCode(in FindOutsEventArgs e) {
        int boardsize = e.Board.Cards.Length;
        int numberOfBitsInCardHash = Card.NumberOfBitsInHashcode;
        ulong hashCode = 0;

        Card[] pocket = new Card[e.Pocket.Cards.Length];
        Card[] board = new Card[boardsize];

        Array.Copy(e.Pocket.Cards, pocket, e.Pocket.Cards.Length);
        Array.Copy(e.Board.Cards, board, boardsize);

        pocket.SortDescending();
        board.SortDescending();
        int bHashSize = 0;
        for (int i = board.Length - 1; i >= 0; i--) {
            hashCode |= (ulong)board[i].GetHashCode() << i * numberOfBitsInCardHash;
            bHashSize++;
        }

        for(int i = pocket.Length - 1; i >= 0; i--) {
            hashCode |= (ulong)pocket[i].GetHashCode() << i * numberOfBitsInCardHash + bHashSize * numberOfBitsInCardHash;
        }

        return hashCode;
    }

    /// <summary>
    /// Job to find all the outs. An out in poker is a card that is likely to improve the player's hand more than it improves their opponants hands and give the player a winning hand.
    /// For example, if we only have a high card, a card coming up that matches the rank of one already on the board technically improves our hand from high card to a pair, but it has also 
    /// improved the opponants hands by the same amount, so is not an out. If we have a 2 in our pocket, a 2 hitting the board is unlikely to help us win as there are better pairs the opponants could have.
    /// </summary>
    [BurstCompile]
    private struct FindOuts : IJob {

        /// <summary>
        /// Cards that are outs
        /// </summary>
        public NativeHashSet<Card> outs;

        public int NumberOfOuts { get { return outs.Count(); } }

        /// <summary>
        /// The pocket cards
        /// </summary>
        private NativeArray<Card> pocket;
        public NativeArray<Card> Pocket { get { return pocket; } set { pocket = value; } }

        /// <summary>
        /// The board cards
        /// </summary>
        private NativeArray<Card> board;
        public NativeArray<Card> Board { get { return board; } set { board = value; } }

        /// <summary>
        /// The type of hand currently held
        /// </summary>
        public HandRank BestHand { get; set; }

        /// <summary>
        /// Whether the hand is an ace low straight
        /// </summary>
        public bool IsALowStraight { get; set; }


        /// <summary>
        /// Stores how many of each suit are available. First int is Suit cast to int, second is quantity.
        /// Access through property, not field, to ensure it has been created.
        /// </summary>
        //public NativeHashMap<int, int> suits;

        public NativeHashMap<int, int>.Enumerator suitsEnumerator;

        /// <summary>
        /// Stores the duplicate ranks available. First int is Rank cast to int, second is the quantity of that rank
        /// Access through property, not field, to ensure it has been created.
        /// </summary>
        //public NativeHashMap<int, int> duplicates;

        public NativeHashMap<int, int>.Enumerator duplicatesEnumerator;
        /// <summary>
        /// Stores all the cards available (pocket and board) in descending order
        /// Access through property, not field, to ensure it has been created.
        /// </summary>
        public NativeArray<Card> allCards;

        //Sort the cards (not using extention method as it requires boxing which burst does not allow)
        private void SortDescending(NativeArray<Card> cards) {
            
            for (int i = 0; i < cards.Length; i++) {
                Card temp = cards[i];

                int j = i - 1;
                while (j >= 0 && cards[j].Rank < temp.Rank) {
                    cards[j + 1] = cards[j];
                    j--;
                }
                cards[j + 1] = temp;
            }
        }

        /// <summary>
        /// Finds the number of outs available, based on existing hand type
        /// </summary>
        public void Execute() {

            SortDescending(pocket);
            SortDescending(board);

            switch (BestHand) {

                case HandRank.HighCard:
                    HandleHighCard();
                    break;
                case HandRank.Pair:
                    if (FindBoardPairedRank(out _)) { //The pair we have is on the board, so we effectively only have a high card as everyone has that pair
                        HandleHighCard();
                        break;
                    }
                    HandlePair();
                    break;
                case HandRank.TwoPair:
                    HandleTwoPair();
                    break;

                case HandRank.ThreeOfAKind:
                    CheckForStraightDraws();
                    CheckForFlushDraws(out _);
                    CheckForFullHouseDraws();
                    CheckForQuadDraws();
                    break;

                case HandRank.Straight:
                    HandleStraight();
                    break;

                case HandRank.Flush:
                    CheckForFullHouseDraws();
                    CheckForQuadDraws();
                    Suit flushSuit = FindFlushSuit();
                    CheckForStraightDraws(true, flushSuit);
                    break;

                case HandRank.FullHouse:
                    CheckForQuadDraws();
                    break;

                default:
                    break;

            }

            //We've potentially added cards that are either in the pocket or on the board. This is cleaner than putting checks all over the place to prevent it but now we need to make sure they're taken out.
            foreach(Card card in allCards) {
                outs.Remove(card);
            }
        }

        /// <summary>
        /// Checks where the pairs are (board, pocket or split between the two) and checks for outs accordingly
        /// </summary>
        private void HandleTwoPair() {
            
            int numberOfBoardPairs = FindValueInMap(FindDuplicates(Board), 2).Length;
            if(numberOfBoardPairs >= 2) {
                HandleHighCard();
                return;
            }
            if(numberOfBoardPairs == 1) {
                HandlePair();
                return;
            }
            CheckForStraightDraws();
            CheckForFlushDraws(out _);
            CheckForFullHouseDraws();
        }

        /// <summary>
        /// Adds our pocket card ranks as outs. Unlike high card, we count any rank as an out here because two pair is less likely than a pair, so the ranks have lower importance as two pair vs two pair will happen a lot less often.
        /// Don't do this if the board is paired as that increases the chance of 2 pair vs 2 pair
        /// </summary>
        private void HandlePair() {
            AddAllRank(pocket[0].Rank);
            if (pocket[0].Rank != pocket[1].Rank) { // not pocket pair
                AddAllRank(pocket[1].Rank);
            }
            CheckForStraightDraws();
            CheckForFlushDraws(out _);
        }

        /// <summary>
        /// Adds any overcards, and checks for straight and flush draws
        /// </summary>
        private void HandleHighCard() {
            AddOverCards();
            CheckForStraightDraws();
            CheckForFlushDraws(out _);
        }

        /// <summary>
        /// Adds any overcards (cards in the pocket that are higher rank than the highest rank on the board) to the outs
        /// </summary>
        private void AddOverCards() {
            if(pocket[0].Rank > board[0].Rank) {
                AddAllRank(pocket[0].Rank);
            }
            if(pocket[1].Rank > board[0].Rank) {
                AddAllRank(pocket[1].Rank);
            }
        }

        /// <summary>
        /// Finds whether there is a pair on the board, passing out the rank of the pair
        /// </summary>
        /// <param name="pair">The rank of the pair</param>
        /// <returns>Whether there is a pair on the board</returns>
        private bool FindBoardPairedRank(out Rank pair) {
            for(int i = 1; i < board.Length; i++) {
                if(board[i-1].Rank == board[i].Rank) {
                    pair = board[i].Rank;
                    return true;
                }
            }
            pair = Rank.ace;
            return false;
        }

        /// <summary>
        /// Finds whether there are straight draws available and if so adds the outs. Finds the straight draws by using the fact that the cards are in descending order and looking at the range of ranks between x number of cards. Usually x is 4.
        /// The range is found by taking the lowest rank away from the highest rank. For example, if we have 4 cards (all different ranks) with a highest rank of 6 and a lowest rank of 3, we have a range of 3 and there will be an open ended straight draw 
        /// (where the 4 cards are 6, 5, 4, 3). If the range is 4 there is a gutshot draw (one rank is missing from the middle of the straight, eg 6, 5, 3, 2).
        /// The number of cards we look at is increased if we have duplicate ranks within the cards, for example, if the cards are 6, 5, 5, 4 we need to look at the next card before working out the range, if it's a 3 or a 2 we have draws.
        /// </summary>
        /// <param name="lookingForStraightFlush">Whether to just look for straight flushes</param>
        /// <param name="flushSuit">The suit of the straight flush</param>
        private void CheckForStraightDraws(bool lookingForStraightFlush = false, Suit flushSuit = Suit.clubs) {

            if(BestHand >= HandRank.Straight) {
                return;
            }

            for(int i = 0; i + 3 < allCards.Length; i++) {

                int numberOfAdditionalCardsToCheck = 3;
                bool dontContinue = false;
                duplicatesEnumerator.Reset();
                while (duplicatesEnumerator.MoveNext()) {
                    if(duplicatesEnumerator.Current.Key <= (int)allCards[i].Rank &&
                        duplicatesEnumerator.Current.Key >= (int)allCards[i + 3].Rank) {
                        if(i + numberOfAdditionalCardsToCheck + duplicatesEnumerator.Current.Value - 1 >= allCards.Length) {
                            dontContinue = true;
                            break;
                        }
                        numberOfAdditionalCardsToCheck += duplicatesEnumerator.Current.Value - 1;
                    }
                }
                //foreach(KeyValue<int, int> pair in duplicates) {
                //    if(pair.Key <= (int)allCards[i].Rank && pair.Key >= (int)allCards[i + 3].Rank) {
                //        if(i + numberOfAdditionalCardsToCheck + pair.Value - 1 >= allCards.Length) {
                //            dontContinue = true;
                //            break;
                //        }
                //        numberOfAdditionalCardsToCheck += pair.Value - 1;
                //    }
                //}
                if (dontContinue) {
                    break;
                }

                int range = (int)allCards[i].Rank - (int)allCards[i + numberOfAdditionalCardsToCheck].Rank;
                
                if(range == 3) { // open ended draw
                    //Add card above highest card
                    if(allCards[i].Rank != Rank.ace) {
                        if (!lookingForStraightFlush) {
                            AddAllRank(allCards[i].Rank + 1);
                        } else {
                            if (CheckForSameSuitWithinRange(i, numberOfAdditionalCardsToCheck)) {
                                outs.Add(new Card(flushSuit, allCards[i].Rank + 1));
                            }
                        }
                    }

                    //Add card below lowest rank
                    if(allCards[i + numberOfAdditionalCardsToCheck].Rank != Rank.two) {
                        if (!lookingForStraightFlush) {
                            AddAllRank(allCards[i + numberOfAdditionalCardsToCheck].Rank - 1);
                        } else {
                            if (CheckForSameSuitWithinRange(i, numberOfAdditionalCardsToCheck)) {
                                outs.Add(new Card(flushSuit, allCards[i + numberOfAdditionalCardsToCheck].Rank - 1));
                            }
                        }
                    } else { //Ace low 
                        if (!lookingForStraightFlush) {
                            AddAllRank(Rank.ace);
                        } else {
                            if (CheckForSameSuitWithinRange(i, numberOfAdditionalCardsToCheck)) {
                                outs.Add(new Card(flushSuit, Rank.ace));
                            }
                        }
                    }
                }

                if(range == 4) { // Gutshot draw (card missing from the middle of the straight)
                    for(int j = 1; j < numberOfAdditionalCardsToCheck; j++) {
                        if(allCards[i + j - 1].Rank != allCards[i + j].Rank && allCards[i + j - 1].Rank != allCards[i + j].Rank + 1){ //Find which card is missing
                            if (!lookingForStraightFlush) {
                                AddAllRank(allCards[i + j].Rank + 1);
                            } else {
                                if (CheckForSameSuitWithinRange(i, numberOfAdditionalCardsToCheck)) {
                                    outs.Add(new Card(flushSuit, allCards[i + j].Rank + 1));
                                }
                            }
                        }
                    }
                }
            }


            if (allCards[0].Rank == Rank.ace && allCards[allCards.Length - 1].Rank <= Rank.three) {
                CheckALowWithA(lookingForStraightFlush, flushSuit);
            }
        }

        /// <summary>
        /// Similar to check for straight draws, but looks for where an Ace (which has a rank value of 14) can be below a two and the player has the ace already
        /// </summary>
        /// <param name="lookingForStraightFlush">Whether to just look for straight flushes</param>
        /// <param name="flushSuit">The suit of the straight flush</param>
        private void CheckALowWithA(bool lookingForStraightFlush, Suit flushSuit) {
            //check for Ace low straight draws where we have the ace
            int numberOfAdditionalCardsToCheck = 2;
            duplicatesEnumerator.Reset();
            while (duplicatesEnumerator.MoveNext()) {
                if (duplicatesEnumerator.Current.Key <= 5) {
                    numberOfAdditionalCardsToCheck += duplicatesEnumerator.Current.Value - 1;
                }
            }

            //foreach (KeyValue<int, int> pair in duplicates) {
            //    if (pair.Key <= 5) {
            //        numberOfAdditionalCardsToCheck += pair.Value - 1;
            //    }
            //}
            if (allCards.Length - numberOfAdditionalCardsToCheck < 0 || (int)allCards[allCards.Length - numberOfAdditionalCardsToCheck].Rank >= 6) {
                return;
            }

            int range = (int)allCards[allCards.Length - numberOfAdditionalCardsToCheck].Rank - (int)allCards[allCards.Length - 1].Rank;

            if (range > 3 || range <= 1) {
                return;
            }
            Rank neededRank = Rank.ace;
            if (range == 2) { //Need either a 5 or a 2
                if (allCards[allCards.Length - 1].Rank == Rank.two) {
                    neededRank = Rank.five;
                } else {
                    neededRank = Rank.two;
                }
            }

            //Range = 3, need a 3 or a 4
            for (int i = 1; i < allCards.Length; i++) {
                if (allCards[i].Rank == Rank.four) {
                    neededRank = Rank.three;
                    break;
                }
                if (allCards[i].Rank == Rank.three) {
                    neededRank = Rank.four;
                    break;
                }
            }

            //Sanity, shouldn't happen
            if (neededRank == Rank.ace) {
                return;
            }

            if (!lookingForStraightFlush) {
                AddAllRank(neededRank);
                return;
            }

            if (CheckForSameSuitWithinRange(allCards.Length - numberOfAdditionalCardsToCheck, allCards.Length - 1)) {
                outs.Add(new Card(flushSuit, neededRank));
            }
            return;
        }

        /// <summary>
        /// Checks whether 4 cards within a range of allcards are the same suit for straight flushes
        /// </summary>
        /// <param name="i">Starting position in allcards</param>
        /// <param name="numberOfAdditionalCardsToCheck">How many more cards to check</param>
        /// <returns></returns>
        private bool CheckForSameSuitWithinRange(int i, int numberOfAdditionalCardsToCheck) {

            NativeHashMap<int, int> numberOfSuits = GetSuits(allCards.GetSubArray(i, numberOfAdditionalCardsToCheck));
            foreach(KeyValue<int, int> keyValue in numberOfSuits) {
                if(keyValue.Value >= 4) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks for outs where we already have a straight
        /// </summary>
        private void HandleStraight() {
            CheckForFullHouseDraws();
            if (CheckForFlushDraws(out Suit suit)) {
                CheckForStraightDraws(true, suit);
            }
        }

        /// <summary>
        /// Checks for outs that lead to a full house (only possible if the player already has two pairs or trips and those are not on the board)
        /// </summary>
        /// <returns></returns>
        private bool CheckForFullHouseDraws() {
            bool draws = false;
            if (ContainsTwoPairs(allCards, out NativeArray<Rank> ranks)) {
                draws = true;
                if (!ContainsTwoPairs(Board, out _)) {
                    AddAllRanks(ranks);
                }
            }

            if (FindTrips(allCards, out Rank rank)) {
                draws = true;
                if (!FindTrips(board, out _)) {
                    AddAllRank(rank);
                    for (int i = 0; i < board.Length; i++) {
                        AddAllRank(board[i].Rank);
                    }
                }

            }
            return draws;
        }

        /// <summary>
        /// Checks for outs that lead to quads (only possible if the player already has trips and the trips aren't on the board)
        /// </summary>
        /// <returns></returns>
        private bool CheckForQuadDraws() {
            if(FindTrips(board, out _)) {
                return true; //We have a quad draw, but no outs as if the draw hits everyone will have quads
            }
            NativeArray<int> trips = FindValueInMap(duplicatesEnumerator, 3);
            if (trips.Length == 0) {
                return false;
            }
            foreach(int rank in trips) {
                AddAllRank((Rank)rank);
            }
            return true;

        }

        /// <summary>
        /// Whether an array of cards contains two pairs
        /// </summary>
        /// <param name="cards">The array of cards</param>
        /// <param name="ranks">The ranks of the pairs</param>
        /// <returns>Whether 2 pairs exists</returns>
        private bool ContainsTwoPairs(NativeArray<Card> cards, out NativeArray<Rank> ranks) {
            NativeHashMap<int, int>.Enumerator dups;
            if(cards == allCards) {
                dups = duplicatesEnumerator;
            } else {
                dups = FindDuplicates(cards).GetEnumerator();
            }
            
            ranks = FindValueInMap(dups, 2).Reinterpret<Rank>();
            return (ranks.Length >= 2);
        }

        /// <summary>
        /// Finds trips (3 cards same rank) in the array, passing out the rank of the trips
        /// </summary>
        /// <param name="cards">Card array</param>
        /// <param name="rank">Rank of the trips</param>
        /// <returns>Whether the trips were found</returns>
        private bool FindTrips(NativeArray<Card> cards, out Rank rank) {
            rank = (Rank)(-1);
            NativeHashMap<int, int>.Enumerator dups;
            if(cards == allCards) {
                dups = duplicatesEnumerator;
            } else {
                dups = FindDuplicates(cards).GetEnumerator();
            }
            NativeArray<int> trips = FindValueInMap(dups, 3);
            if(trips.Length == 0) {
                return false;
            }
            rank = (Rank)trips[0];
            return true;

        }

        public NativeArray<int> FindValueInMap(NativeHashMap<int, int> map, int valueToSearchFor, Allocator allocator = Allocator.Temp) { 
            NativeHashMap<int, int>.Enumerator enumerator = map.GetEnumerator();
            return FindValueInMap(enumerator, valueToSearchFor, allocator);

        }

        public NativeArray<int> FindValueInMap(NativeHashMap<int, int>.Enumerator mapEnumerator, int valueToSearchFor, Allocator allocator = Allocator.Temp) {
            
            int i = 0;
            mapEnumerator.Reset();
            int size = 1;
            while (mapEnumerator.MoveNext()) {
                size++;
            }
            NativeArray<int> keys = new NativeArray<int>(size, allocator);
            while (mapEnumerator.MoveNext()) {
                if (mapEnumerator.Current.Value == valueToSearchFor) {
                    keys[i] = mapEnumerator.Current.Key;
                    i++;
                }
            }
            return keys;
        }


        /// <summary>
        /// Checks whether there are flush draws in AllCards, and if so adds the outs
        /// </summary>
        /// <param name="suit"></param>
        /// <returns></returns>
        private bool CheckForFlushDraws(out Suit suit) {
            bool draws = false;
            suit = (Suit)(-1);

            suitsEnumerator.Reset();
            while (suitsEnumerator.MoveNext()) {
                if(suitsEnumerator.Current.Value >= 4) {
                    suit = (Suit)suitsEnumerator.Current.Key;
                    draws = true;
                    if(suitsEnumerator.Current.Value == 4) {
                        AddAllSuit(suit);
                        break;
                    }
                }
            }
            //foreach(KeyValue<int,int> pair in suits) {
            //    if(pair.Value >= 4) {
            //        suit = (Suit)pair.Key;
            //        draws = true;
            //        if(pair.Value == 4) {
            //            AddAllSuit(suit);
            //        }
            //        break;
            //    }
            //}
            return draws;
        }

        /// <summary>
        /// Adds all of the given rank to the outs
        /// </summary>
        /// <param name="rank"></param>
        private void AddAllRank(Rank rank) {
            for(int i = 0; i < 4; i++) {
                outs.Add(new Card((Suit)i, rank));
            }
        }

        /// <summary>
        /// Adds all of the passed in ranks to the outs
        /// </summary>
        /// <param name="ranks"></param>
        private void AddAllRanks(NativeArray<Rank> ranks) {
            for(int i = 0; i < ranks.Length; i++) {
                AddAllRank(ranks[i]);
            }
        }

        /// <summary>
        /// Adds all of a suit above the given rank to the outs
        /// </summary>
        /// <param name="suit"></param>
        /// <param name="rank"></param>
        private void AddAllSuit(Suit suit, Rank rank = Rank.two) {
            for(int i = (int)rank; i < 15; i++) {
                outs.Add(new Card(suit, (Rank)i));
            }
        }

        /// <summary>
        /// Finds the suit of the flush. Returns -1 as a suit if not found
        /// </summary>
        /// <returns></returns>
        private Suit FindFlushSuit() {
            for(int i = 0; i < 4; i++) {
                suitsEnumerator.Reset();
                while (suitsEnumerator.MoveNext()) {
                    if(suitsEnumerator.Current.Value >= 5) {
                        return (Suit)suitsEnumerator.Current.Value;
                    }
                }
            }
            return (Suit)(-1);
        }

        /// <summary>
        /// Gets a hashmap containing the nmumber of each suit contained in the cards array
        /// </summary>
        /// <param name="cards"></param>
        /// <returns><int, int>Hashmap where the first int is the suit cast as an int, the second int is the quantity of that suit</returns>
        private NativeHashMap<int, int> GetSuits(NativeArray<Card> cards) {
            NativeHashMap<int, int> cardSuits = new NativeHashMap<int, int>(cards.Length, Allocator.Temp);
            for (int i = 0; i < cards.Length; i++) {
                int suit = (int)cards[i].Suit;
                if (!cardSuits.ContainsKey(suit)) {
                    cardSuits.Add(suit, 1);
                    continue;
                }
                cardSuits[suit]++;
            }
            return cardSuits;
        }

        /// <summary>
        /// Finds duplicate ranks within an array of cards
        /// </summary>
        /// <param name="cards">The array to check</param>
        /// <returns><int, int>Hashmap where the first int is the rank cast to an int, and the second is the quantity of that rank</returns>
        private NativeHashMap<int, int> FindDuplicates(NativeArray<Card> cards) {
            NativeHashMap<int, int> foundDuplicates = new NativeHashMap<int, int>(cards.Length / 2, Allocator.Temp);
            for (int i = 1; i < cards.Length; i++) {
                if (cards[i - 1].Rank == cards[i].Rank) {
                    foundDuplicates.TryAdd((int)cards[i].Rank, 1);
                    foundDuplicates[(int)cards[i].Rank]++;
                }
            }
            NativeArray<int> singles = foundDuplicates.FindValue(1);
            foreach (int key in singles) {
                foundDuplicates.Remove(key);
            }
            return foundDuplicates;
        }
    }
}
