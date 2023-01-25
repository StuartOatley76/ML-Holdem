using System.Linq;

public class Board : CardGroup
{
    private const int MAX_NUMBER_OF_CARDS = 5;
    private int numberOfCards = 0;

    public static int MaxCards { get { return MAX_NUMBER_OF_CARDS; } }
    public Board() : base(MAX_NUMBER_OF_CARDS) { }

    public bool AddCards(params Card[] cardsToAdd) {
        if(cardsToAdd == null || numberOfCards + cardsToAdd.Length > MAX_NUMBER_OF_CARDS || cardsToAdd.Count(c => c.IsValid() == false) > 0) {
            return false;
        }
        cardsToAdd.CopyTo(cards, numberOfCards);
        numberOfCards += cardsToAdd.Length;
        return true;
    }

    public void Reset() {
        cards = new Card[MAX_NUMBER_OF_CARDS];
        numberOfCards = 0;
    }

    public override bool Equals(object obj) {
        if (obj == null || !GetType().Equals(obj.GetType())) {
            return false;
        }

        Board toCompare = (Board)obj;

        if(numberOfCards != toCompare.numberOfCards) {
            return false;
        }

        for(int i = 0; i < numberOfCards; i++) {
            if (!cards[i].Equals(toCompare.cards[i])) {
                return false;
            }
        }
        return true;
    }

    public override int GetHashCode() {
        int hashcode = 0;
        for(int i = 0; i < cards.Length; i++) {
            hashcode |= cards[i].GetHashCode() << (cards.Length - 1 - i);
        }
        return hashcode;
    }
}
