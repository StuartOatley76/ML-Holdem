using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Utils.HelperFunctions;
using Utils.ExtentionMethods;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

/// <summary>
/// Analyser that takes in an array of hands and calculates the score for each hand, returning them in order of score, from highest to lowest
/// </summary>
public class HandScorer : Analyser<uint, Hand, HandsEventArgs, HandsEventArgs>
{
    protected override IEnumerator RunAnalysisJob(HandsEventArgs e) {

        SplitArrayIntoFoundAndScore(e.Hands, out Hand[] foundHands, out Hand[] handsToScore);

        if (handsToScore.Length == 0) {
            foundHands.SortDescending();
            HandsEventArgs handsArgs = new HandsEventArgs {
                Hands = foundHands
            };
            AnalysisCompleted(handsArgs);
            yield break;
        }

        NativeArray<Hand> toEvaluate = new NativeArray<Hand>(handsToScore, Allocator.TempJob);
        ScoreHands evaluate = new ScoreHands { Hands = toEvaluate,
            CardsAInHand = Hand.NUMBER_OF_CARDS_IN_A_HAND};
        JobHandle job = evaluate.Schedule(toEvaluate.Length, 16);

        yield return new WaitUntil(()=>job.IsCompleted);

        job.Complete();

        Hand[] evaluatedHands = evaluate.Hands.ToArray();

        AddToDictionary(evaluatedHands);

        Hand[] All = Helpers.MergeArrays(evaluatedHands, foundHands);
        All.SortDescending();

        HandsEventArgs evaluatedHandsArgs = new HandsEventArgs {
            Hands = All
        };
        AnalysisCompleted(evaluatedHandsArgs);
        toEvaluate.Dispose();
    }

    /// <summary>
    /// Adds the newly evaluated hands to the dictionary
    /// </summary>
    /// <param name="evaluatedHands"></param>
    private void AddToDictionary(Hand[] evaluatedHands) {
        for(int i = 0; i < evaluatedHands.Length; i++) {
            lookup.Add((uint)evaluatedHands[i].HashCode, evaluatedHands[i]);
        }
    }

    /// <summary>
    /// Searches the dictionary for each hand in the array and puts into the appropriate array based on whether or not it's found.
    /// </summary>
    /// <param name="hands"></param>
    /// <param name="foundHands"></param>
    /// <param name="handsToEvaluate"></param>
    private void SplitArrayIntoFoundAndScore(Hand[] hands, out Hand[] foundHands, out Hand[] handsToEvaluate) {

        List<Hand> found = new List<Hand>();
        List<Hand> notFound = new List<Hand>();
        foreach(Hand hand in hands) {
            if (lookup.TryGetValue((uint)hand.HashCode, out Hand foundHand)) {
                found.Add(foundHand);
                continue;
            }
            notFound.Add(hand);
        }
        foundHands = found.ToArray();
        handsToEvaluate = notFound.ToArray();
    }

    /// <summary>
    /// Parallel job that calculates each hand's score
    /// </summary>
    [BurstCompile]
    private struct ScoreHands : IJobParallelFor {

        private int cardsInAHand;
        public int CardsAInHand { set { cardsInAHand = value; } }

        private NativeArray<Hand> hands;
        public NativeArray<Hand> Hands { get { return hands; }  set { hands = value; } }

        // Yes, this function is horribly long and could be tidied into multiple functions. However, Burst doesn't 
        // support calling functions unless they're inlined, and C# has no way of guaranteeing functions are inlined,
        // and as this needs to be done multiple times for each player it needs to be as fast as possible.
        public void Execute(int index) {
            
            Hand hand = hands[index];
            FixedList64Bytes<Card> cards = hand.Cards;

            Card temp;

            //Sort the cards (not using extention method as it requires boxing which burst does not allow)
            for (int i = 0; i < cards.Length; i++) {
                temp = cards[i];

                int j = i - 1;
                while (j >= 0 && cards[j].Rank < temp.Rank) {
                    cards[j + 1] = cards[j];
                    j--;
                }
                cards[j + 1] = temp;
            }

            //Find duplicate ranks
            NativeHashMap<int, int> duplicates = new NativeHashMap<int, int>(cards.Length, Allocator.Temp);
            foreach(Card card in cards) {
                if (!duplicates.ContainsKey((int)card.Rank)) {
                    duplicates.Add((int)card.Rank, 0);
                }
                duplicates[(int)card.Rank]++;
            }

            bool foundDuplicates = false;
            //Set handtype based on duplicates
            foreach(KeyValue<int, int> pair in duplicates) {

                if(pair.Value == 1) {
                    continue;
                }

                foundDuplicates = true;

                if(pair.Value == 2) {
                    switch (hand.handRank) {
                        case HandRank.HighCard:
                            hand.handRank = HandRank.Pair;
                            break;
                        case HandRank.Pair:
                            hand.handRank = HandRank.TwoPair;
                            break;
                        case HandRank.ThreeOfAKind:
                            hand.handRank = HandRank.FullHouse;
                            break;
                        default:
                            break;
                    }
                    continue;
                }

                if(pair.Value == 3) {
                    if(hand.handRank == HandRank.HighCard) {
                        hand.handRank = HandRank.ThreeOfAKind;
                        continue;
                    }
                    hand.handRank = HandRank.FullHouse;
                }

                if(pair.Value == 4) {
                    hand.handRank = HandRank.FourOfAKind;
                }
            }

            //Check for flush and straight - both impossible if we have any duplicate ranks
            if (!foundDuplicates) {

                //check for flush
                Suit firstSuit = cards[0].Suit;
                bool secondSuitFound = false;

                for (int i = 1; i < cards.Length; i++) {
                    if (cards[i].Suit != firstSuit) {
                        secondSuitFound = true;
                        break;
                    }
                }
                hand.handRank = secondSuitFound ? hand.handRank :
                    (hand.Cards.Length == cardsInAHand) ? HandRank.Flush : hand.handRank;


                if (hand.Cards.Length == cardsInAHand) {
                    //Check for straight. As the code can't reach this bit if we have any duplicate ranks and the cards are ordered by rank, we just need to check the difference in rank between the first and last card with an additional check
                    //for ace low straight
                    bool aceLowStraight = false;
                    bool haveStraight = false;
                    if (cards[0].Rank == cards[cards.Length - 1].Rank + cards.Length - 1) {
                        haveStraight = true;
                    } else if ((uint)cards[1].Rank == cards.Length && cards[0].Rank == Rank.ace) {
                        haveStraight = true;
                        aceLowStraight = true;
                    }

                    if (haveStraight) {
                        if (hand.handRank == HandRank.Flush) {
                            hand.handRank = HandRank.StraightFlush;
                        } else {
                            hand.handRank = HandRank.Straight;
                        }
                        hand.AceLowStraight = aceLowStraight;
                    }
                }
            }

            //Score hands. To score a hand we give it a bit code where the most significant 4 bits represent the type of hand, the next 4 represent the first card in the array working down to the last card. We need to take account of the fact that in an ace low straight, the ace
            //is in the wrong place in the array, and Rank.Ace gives the wrong value.

            //If we have duplicates we need to order the cards before scoring so that the cards in the duplicates are more significant than those not in them, so a pair is aabcd where aa are the paired cards and bcd are still in order, two pair is aabbc where a > b, 
            //trips is aaabc where b>c and a full house is aaabb 

            if (foundDuplicates) {
                switch (hand.handRank) {

                    case HandRank.FullHouse:
                        duplicates.TryGetValue((int)cards[0].Rank, out int numberOffirst);
                        if(numberOffirst == 2) {
                            Card tempCardOne = cards[0];
                            Card tempCardTwo = cards[1];
                            for(int i = 0; i < cards.Length - 3; i++) {
                                cards[i] = cards[i + 2];
                            }
                            cards[cards.Length - 2] = tempCardOne;
                            cards[cards.Length - 1] = tempCardTwo;
                        }
                        break;

                    case HandRank.ThreeOfAKind:
                        int firstPosition = 0;
                        for (int i = 0; i < cards.Length; i++) {
                            duplicates.TryGetValue((int)cards[i].Rank, out int numberOfCurrent);
                            if (numberOfCurrent == 3) {
                                break;
                            }
                            firstPosition++;
                        }
                        if(firstPosition > 2) {
                            break;
                        }

                        int numberToMove = 3 + firstPosition - 1;
                        for(int i = 0; i < firstPosition; i++) {
                            Card tempCardTrips = cards[0];
                            for(int j = 1; j < numberToMove; j++) {
                                cards[j - 1] = cards[j];
                            }
                            cards[numberToMove] = tempCardTrips;
                        }
                        break;

                    case HandRank.TwoPair:

                        int tempCardPosition = 4;

                        for (int i = 0; i < cards.Length; i++) {
                            duplicates.TryGetValue((int)cards[i].Rank, out int quantity);
                            if (quantity == 1) {
                                tempCardPosition = i;
                                break;
                            }
                        }

                        Card tempCard = cards[tempCardPosition];
                        for(int i = tempCardPosition; i < cards.Length - 1; i++) {
                            cards[i] = cards[i + 1];
                        }
                        cards[cards.Length - 1] = tempCard;
                        break;

                    case HandRank.Pair:
                        int firstCardPos = 0;
                        for(int i = 0; i < cards.Length; i++) {
                            duplicates.TryGetValue((int)cards[0].Rank, out int count);
                            if(count == 2) {
                                firstCardPos = i;
                                break;
                            }
                        }

                        if (firstCardPos > 0) {
                            Card tempCardOne = cards[firstCardPos];
                            Card tempCardTwo = cards[firstCardPos + 1];

                            for (int i = firstCardPos - 1; i > 0; i--) {
                                cards[i + 2] = cards[i];
                            }

                            cards[0] = tempCardOne;
                            cards[1] = tempCardTwo;
                        }
                        break;

                    default:
                        break;
                }
            }

            int bitshift = 4;   //Whilst this would be nicer as a member variable (ideally a const) we can't use const as const is static and we can't assess statics from within a job (Yes, accessing a const would be thread safe as it's readonly... tell that to Unity)
                                //and we can't initialise non static members of structs in declarations, so it makes most sense to put it here.

            if (!hand.AceLowStraight) {
                 hand.Score = (int)hand.handRank << (cards.Length * bitshift);
                for(int i = cards.Length - 1; i >= 0; i--) {
                    hand.Score |= (int)cards[i].Rank << (i * bitshift);
                }
            } else {
                for(int i = cards.Length - 2; i >= 0; i--) {
                    hand.Score |= (int)cards[i].Rank << ((i + 1) * bitshift);
                }
                hand.Score += 1;
            }

            duplicates.Dispose();
            hand.Cards = cards;
            hands[index] = hand;
        }
    }
}
