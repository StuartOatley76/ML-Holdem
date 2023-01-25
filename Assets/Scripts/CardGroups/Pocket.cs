using System;

/// <summary>
/// Card group to represent pocket cards
/// </summary>
public class Pocket : CardGroup
{
    private const int NUMBER_OF_CARDS = 2;  //Number of pocket cards
    public static int NumberOfCards { get { return NUMBER_OF_CARDS; } }
    public Card Card1 { get; private set; } //First pocket card
    public Card Card2 { get; private set; } //Second Pocket card

    /// <summary>
    /// Constructor
    /// </summary>
    public Pocket() : base(NUMBER_OF_CARDS) { }

    /// <summary>
    /// Resets the pocket cards
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="e"></param>
    public void Reset() {
        Card1 = new Card();
        Card2 = new Card();
        cards = new Card[NUMBER_OF_CARDS];
    }

    /// <summary>
    /// Adds a card to the pocket cards
    /// </summary>
    /// <param name="card">The card to add</param>
    /// <returns>Position in the card array of the card, or -1 if the array is full</returns>
    public int AddCard(Card card) {
        if(Card1.IsValid() != false && Card2.IsValid() != false) {
            return -1;
        }
        if(Card1.IsValid() == false) {
            Card1 = card;
            cards[0] = card;
            return 0;
        }
        Card2 = card;
        cards[1] = card;
        return 1;
    }

}
