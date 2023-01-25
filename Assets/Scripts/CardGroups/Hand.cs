using System;
using Unity.Collections;
using Utils.ExtentionMethods;


//Enum to represent hand rank. Given binary value to be used in scoring hands
public enum HandRank {
    HighCard = 0b0000,
    Pair = 0b0001,
    TwoPair = 0b0010,
    ThreeOfAKind = 0b0011,
    Straight = 0b0100,
    Flush = 0b0101,
    FullHouse = 0b0110,
    FourOfAKind = 0b0111,
    StraightFlush = 0b1000
}

//Struct to represent a 5 card poker hand. Implements Icomparable by comparing hand scores
public struct Hand : IComparable
{

    public const int NUMBER_OF_CARDS_IN_A_HAND = 5; //Number of cards that makes up a hand


    private FixedList64Bytes<Card> cards; //The cards in the hand
    public FixedList64Bytes<Card> Cards {
        get { return cards; }
        set { cards = value; }
    }
    public int Score { get; set; } //The score for the hand

    public int HashCode { get; private set; } //Unique hashcode for the hand. Consists of the hashcodes of each card when hand is ordered, with each card hashcode bitshifted so that higher cards are more significant

    public HandRank handRank { get; set; } // The hand rank of the hand

    public bool AceLowStraight { get; set; } // Whether the hand is an ace low straight

    /// <summary>
    /// Constructor for the hand
    /// </summary>
    /// <param name="cardsForHand">The cards in the hand</param>
    public Hand(params Card[] cardsForHand) {
        cards = new FixedList64Bytes<Card>();
        for(int i = 0; i < NUMBER_OF_CARDS_IN_A_HAND && i < cardsForHand.Length; i++) {
            cards.Add(cardsForHand[i]);
        }
        cards.SortDescending();
        HashCode = 0;
        for(int i = 0, j = cards.Length - 1; i < cards.Length; ++i, j = cards.Length - 1 - i) {
            HashCode += cards[i].GetHashCode() << Card.NumberOfBitsInHashcode * j;
        }
        handRank = HandRank.HighCard;
        AceLowStraight = false;
        Score = 0;
    }

    /// <summary>
    /// Override of Object.GetHashCode()
    /// </summary>
    /// <returns>The hand's hashcode</returns>
    public override int GetHashCode() {
        return HashCode;
    }

    /// <summary>
    /// Override of Object.Equals()
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj) {
        if (obj == null || !GetType().Equals(obj.GetType())) {
            return false;
        }

        Hand toCompare = (Hand)obj;

        for(int i = 0; i < NUMBER_OF_CARDS_IN_A_HAND; i++) {
            if (!cards[i].Equals(toCompare.cards[i])) {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Implementation of CompareTo for IComparable
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public int CompareTo(object obj) {
        if(obj == null || !(obj is Hand)) {
            return 1;
        }

        Hand toCompare = (Hand)obj;

        return Score - toCompare.Score;
    }

    public override string ToString() {
        return "Hand rank = " + Enum.GetName(typeof(HandRank), (int)handRank) + "\n" + base.ToString();
        
    }
}
