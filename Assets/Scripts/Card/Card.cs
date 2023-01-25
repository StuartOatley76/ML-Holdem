using Newtonsoft.Json;
using System;

/// <summary>
/// Enum for card suit with binary value for hashcode
/// </summary>
public enum Suit {
    hearts = 0b00,
    spades = 0b01,
    diamonds = 0b10,
    clubs = 0b11
}

/// <summary>
/// Enum for card rank with binary value for hashcode and scoring
/// </summary>
public enum Rank {
    two = 0b0010,
    three = 0b0011,
    four = 0b0100,
    five = 0b0101,
    six = 0b0110,
    seven = 0b0111,
    eight = 0b1000,
    nine = 0b1001,
    ten = 0b1010,
    jack = 0b1011,
    queen = 0b1100,
    king = 0b1101,
    ace = 0b1110

}

/// <summary>
/// Struct to represent a playing card
/// </summary>
//12 bytes (2 x Enum = 8, 1 x int = 4,)
public struct Card : IComparable, IEquatable<Card>
{
    private const uint SUIT_MASK = 0b000011; //bit mask to retrieve the suit from the hashcode

    private const int NUMBER_OF_BITS_IN_HASHCODE = 6; //Length of the hashcode
    public static int NumberOfBitsInHashcode { get { return NUMBER_OF_BITS_IN_HASHCODE; } }

    private Suit suit; //The suit of the card
    public Suit Suit { get { return suit; } }

    private Rank rank; //The rank of the card
    public Rank Rank { get { return rank; } }

    [JsonProperty]
    private int hashCode; //The card's unique hashcode created from the rank value and suit value, with the rank value most significant

    public Card(Suit cardSuit, Rank cardRank) {
        suit = cardSuit;
        rank = cardRank;
        hashCode = (int)cardSuit | (int)rank << 2;  //rank is the most significant bits so that we can use the hashcode to order cards by rank
    }

    /// <summary>
    /// copy constructor
    /// </summary>
    /// <param name="cardToCopy"></param>
    public Card(Card cardToCopy) {
        suit = cardToCopy.suit;
        rank = cardToCopy.rank;
        hashCode = cardToCopy.hashCode;
    }

    public static Card ConvertFromHash(in uint cardCode) {
        return new Card((Suit)(cardCode & SUIT_MASK), (Rank)(cardCode >> 2));
    }

    /// <summary>
    /// Is this a valid card? Cards created with the default constructor (eg when an array of cards is created) are not valid
    /// </summary>
    /// <returns></returns>
    public bool IsValid() {
        //The default constructor will set the hashcode to 0, so we can test for that
        return (hashCode == 0) ? false : true;
    }

    /// <summary>
    /// Provides the name of the card as a string
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return Enum.GetName(typeof(Rank), rank) + " of " + Enum.GetName(typeof(Suit), suit);
    }

    /// <summary>
    /// override of Object.GetHashCode()
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode() {
        return hashCode;
    }

    /// <summary>
    /// Checks whether this card is the same suit as the supplied card
    /// </summary>
    /// <param name="card">The card to compare the suit to</param>
    /// <returns>Whether the cards are the same suit</returns>
    public bool IsSameSuitAs(Card card) {
        return suit == card.suit;
    }

    /// <summary>
    /// Override of Object.Equals
    /// </summary>
    /// <param name="obj"></param>
    /// <returns>Whether the cards are the same suit and rank</returns>
    public override bool Equals(object obj) {
        if (obj == null || !GetType().Equals(obj.GetType())) {
            return false;
        }
        Card toCompare = (Card)obj;

        return toCompare.rank == rank && toCompare.suit == suit;

    }

    /// <summary>
    /// Implementation of CompareTo for IComparable
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public int CompareTo(object obj) {
        if(obj == null || !GetType().Equals(obj.GetType())) {
            return 1;
        }
        Card toCompare = (Card)obj;
        return hashCode - toCompare.hashCode;
    }


    /// <summary>
    /// Implementation of Equals for Iequatable
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(Card other) {
        return other.rank == rank && other.suit == suit;
    }

    /// <summary>
    /// Implementation of greater than operator for IComparable
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool operator >(Card a, Card b) {
        return a.hashCode > b.hashCode;
    }

    /// <summary>
    /// Implementation of less than operator for IComparable
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool operator <(Card a, Card b) {
        return a.hashCode < b.hashCode;
    }

}
