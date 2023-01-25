using System;
using System.Linq;
using Utils.HelperFunctions;
using Utils.ExtentionMethods;
using Unity.Jobs;
using Unity.Collections;
using System.Collections;
using Unity.Burst;
using UnityEngine;

/// <summary>
/// Eventargs to hold an array of cards sorted in descending order
/// </summary>
public class CardsEventArgs : EventArgs {
    public Card[] cards { get; private set; }

    public CardsEventArgs(Pocket pocket, Board board) {
        cards = pocket.Cards.Concat(board.Cards).ToArray();

    }

    /// <summary>
    /// Constructor. Merges the arrays of cards provided and sorts the cards
    /// </summary>
    /// <param name="cardGroups"></param>
    public CardsEventArgs(params CardGroup[] cardGroups) {
        int count = cardGroups.Sum(c => c.CountCards());
        cards = new Card[count];
        for(int i = 0; i < cardGroups.Count(); i++) {
            cards = Helpers.MergeArrays(cards, cardGroups[i].Cards);
        }
        cards.SortDescending();
    }
}

/// <summary>
/// Analyser which finds all available 5 card poker hands from an array of cards
/// </summary>
public class HandFinder : Analyser<ulong, Hand[], CardsEventArgs, HandsEventArgs> {

    private const int numberOfCardsInAHand = 5;
    protected override IEnumerator RunAnalysisJob(CardsEventArgs e) {

        ulong hashCode = GetHashCode(e.cards);
        HandsEventArgs toReturn = new HandsEventArgs();

        if (lookup.ContainsKey(hashCode)) {
            lookup.TryGetValue(hashCode, out Hand[] found);
            toReturn.Hands = found;
            AnalysisCompleted(toReturn);
            yield break;
        }



        NativeArray<Card> cards = new NativeArray<Card>(e.cards, Allocator.TempJob);
        cards.SortDescending();
        //Combinations combinationFinder = new Combinations(cards);
        HandFindingJob job = new HandFindingJob {
            Cards = cards,
            Combinations = new NativeList<FixedList64Bytes<Card>>(Allocator.TempJob),
            CardsAInHand = Hand.NUMBER_OF_CARDS_IN_A_HAND
        };

        JobHandle jobHandle = job.Schedule();

        yield return new WaitUntil(() => jobHandle.IsCompleted);

        jobHandle.Complete();

        NativeList<FixedList64Bytes<Card>> combinations = job.Combinations;
        //NativeList<FixedList64<Card>> combinations = combinationFinder.CardCombinations;
        Hand[] hands = new Hand[combinations.Length];
        for (int i = 0; i < combinations.Length; i++) {
            hands[i] = new Hand(combinations[i].ToArray());
        }
        cards.Dispose();
        combinations.Dispose();

        toReturn = new HandsEventArgs { Hands = hands };

        lookup.Add(hashCode, hands);
        AnalysisCompleted(toReturn);
    }

    /// <summary>
    /// creates a unique hash code for the array of cards based on each card's hashcode
    /// </summary>
    /// <param name="availableCards"></param>
    /// <returns></returns>
    private static ulong GetHashCode(in Card[] availableCards) {
        int numberOfBits = Card.NumberOfBitsInHashcode;
        ulong hashCode = 0;
        for (int i = availableCards.Length - 1; i >= 0; i--) {
            hashCode |= (ulong)availableCards[i].GetHashCode() << i * numberOfBits;
        }
        return hashCode;
    }

    /// <summary>
    /// Job that finds all 5 card combinations of the crdas in the cards nativearray 
    /// </summary>
    [BurstCompile]
    private struct HandFindingJob : IJob {

        private NativeArray<Card> cards; //The available cards
        public NativeArray<Card> Cards { 
            get { return cards; } 
            set { cards = value; } 
        }

        private int cardsInAHand;
        public int CardsAInHand { set { cardsInAHand = value; } }
        private NativeList<FixedList64Bytes<Card>> cardCombinations; //List 
        public NativeList<FixedList64Bytes<Card>> Combinations { 
            get { return cardCombinations; }
            set { cardCombinations = value; }
        }

        public void Execute() {
            GetCombinations(new FixedList64Bytes<Card>());
        }


        private void GetCombinations(FixedList64Bytes<Card> cardsInCombo, int start = 0, int index = 0) {
            if (index == cardsInAHand) {
                cardCombinations.Add(cardsInCombo);
                return;
            }

            for (int i = start; i < cards.Length; i++) {
                cardsInCombo.Insert(index, cards[i]);
                GetCombinations(cardsInCombo, i + 1, index + 1);
                cardsInCombo.RemoveAt(index);
                //while (cards.Length < i + 1 && cards[i].Rank == cards[i + 1].Rank) {
                //    i++;
                //}
            }
        }
    }


    private class Combinations {
        private NativeArray<Card> cards; //The available cards
        public NativeArray<Card> Cards {
            get { return cards; }
        }

        private int CardsInAHand = 5;

        private NativeList<FixedList64Bytes<Card>> cardCombinations; 
        public NativeList<FixedList64Bytes<Card>> CardCombinations {
            get { return cardCombinations; }
        }

        public Combinations(NativeArray<Card> cardsToSort) {
            cards = cardsToSort;
            cardCombinations = new NativeList<FixedList64Bytes<Card>>(Allocator.TempJob);
            FixedList64Bytes<Card> combination = new FixedList64Bytes<Card>();
            GetCombinations(combination);

        }

        private void GetCombinations(FixedList64Bytes<Card> cardsInCombo, int start = 0, int index = 0) {
            if (index == CardsInAHand) {
                cardCombinations.Add(cardsInCombo);
                return;
            }

            for (int i = start; i < cards.Length; i++) {
                cardsInCombo.Insert(index, cards[i]);
                GetCombinations(cardsInCombo, i + 1, index + 1);
                cardsInCombo.RemoveAt(index);
                //while (cards.Length < i + 1 && cards[i].Rank == cards[i + 1].Rank) {
                //    i++;
                //}
            }
        }
    }
}
