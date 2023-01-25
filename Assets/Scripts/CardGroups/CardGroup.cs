using System.Linq;
using System;

/// <summary>
/// Abstract class to represent a collection of cards
/// </summary>
public abstract class CardGroup 
{
    protected Card[] cards; //The cards in the group
    public int MaxNumberOfCards { get { return cards.Length; } }
    /// <summary>
    /// returns a copy of the array of cards, checking that the cards are valid
    /// </summary>
    public virtual Card[] Cards { get {
            return cards.Where(c => c.IsValid() != false).ToArray();
        }
    }
    protected int currentCard = 0;
    public CardGroup(int numberOfCards) {
        cards = new Card[numberOfCards];
    }

    public virtual int CountCards() {
        return cards.Count(c => c.IsValid() != false);
    }

    public override string ToString() {
        string toReturn =  "Cards - ";
        for (int i = 0; i < Cards.Length; i++) {
            toReturn += Cards[i].ToString();
            if (i < Cards.Length - 1) {
                toReturn += ", ";
            }
        }
        return toReturn;
    }

}
