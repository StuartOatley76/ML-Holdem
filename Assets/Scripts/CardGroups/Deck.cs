using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// class to represent a deck of cards
/// </summary>
public class Deck : CardGroup {

    public const int NUMBER_OF_CARDS = 52; //Number of cards in a deck
    private int nextCard; // int to record the position of the next card
    private static Vector3 defaultPosition;
    private static Quaternion defaultRotation;
    private static float xScale;
    private static float yScale;
    private static float zScale;
    private Dictionary<Card, GameObject> cardGOs; //Dictionary to hold the card gameobjects

    /// <summary>
    /// Constructor that creates each card in the deck
    /// </summary>
    public Deck() : base(NUMBER_OF_CARDS) {
        cardGOs = new Dictionary<Card, GameObject>();
        int position = 0;
        foreach (Suit suit in Enum.GetValues(typeof(Suit)).Cast<Suit>()) {
            foreach (Rank rank in Enum.GetValues(typeof(Rank)).Cast<Rank>()) {
                cards[position] = new Card(suit, rank);
                cardGOs.Add(cards[position], CardGOCreator.InstantiateCardGO(cards[position]));
                position++;
            }
        }
        defaultPosition = cardGOs[cards[0]].transform.position;
        defaultRotation = cardGOs[cards[0]].transform.rotation;
        xScale = cardGOs[cards[0]].transform.localScale.x;
        yScale = cardGOs[cards[0]].transform.localScale.y;
        zScale = cardGOs[cards[0]].transform.localScale.z;
    }

    /// <summary>
    /// returns the next card in the deck
    /// </summary>
    /// <returns></returns>
    public Card? GetNextCard() {
        if (nextCard < NUMBER_OF_CARDS) {
            return cards[nextCard++];
        }
        return null;
    }

    /// <summary>
    /// returns the card's gameobject
    /// </summary>
    /// <param name="card"></param>
    /// <returns></returns>
    public GameObject GetCardGO(Card card) {
        if (cardGOs.TryGetValue(card, out GameObject go)) {
            go.SetActive(true);
            return go;
        }
        go = CardGOCreator.InstantiateCardGO(card);
        cardGOs.Add(card, go);
        return go;

    }

    /// <summary>
    /// returns the number of unused cards left in the deck
    /// </summary>
    /// <returns></returns>
    public int CardsLeftInDeck() {
        return NUMBER_OF_CARDS - nextCard;
    }

    public void ReturnCardToDeck(Card card) {
        if(cardGOs.TryGetValue(card, out GameObject go)) {
            go.transform.SetParent(null);
            go.transform.position = defaultPosition;
            go.transform.rotation = defaultRotation;
            go.transform.localScale = new Vector3(xScale, yScale, zScale);
            go.SetActive(false);
        }
    }

    /// <summary>
    /// Shuffles the deck. Uses fisher yates shuffle
    /// </summary>
    public void Shuffle() {

        for (int cardPos = 0; cardPos < NUMBER_OF_CARDS; cardPos++) {
            int swapPos = UnityEngine.Random.Range(0, cardPos);
            Card tempCard = cards[swapPos];
            cards[swapPos] = cards[cardPos];
            cards[cardPos] = tempCard;
        }
        nextCard = 0;
    }
}
