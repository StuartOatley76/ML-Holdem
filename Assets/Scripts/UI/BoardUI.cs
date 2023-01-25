using System;
using UnityEngine;
using Utils.HelperFunctions;
public class BoardUI : MonoBehaviour
{
    private GameObject flopCard1;
    private GameObject flopCard2;
    private GameObject flopCard3;
    private GameObject turnCard;
    private GameObject riverCard;

    private GameObject[] cardPositions;
    private int currentnumberOfCards = 0;

    private void Awake() {
        flopCard1 = Helpers.FindChildWithTag(gameObject, "FlopCard1");
        flopCard2 = Helpers.FindChildWithTag(gameObject, "FlopCard2");
        flopCard3 = Helpers.FindChildWithTag(gameObject, "FlopCard3"); 
        turnCard = Helpers.FindChildWithTag(gameObject, "TurnCard");
        riverCard = Helpers.FindChildWithTag(gameObject, "RiverCard");
        cardPositions = new GameObject[] { flopCard1, flopCard2, flopCard3, turnCard, riverCard };
    }

    public EventHandler<CardsAddedToBoardEventArgs> GetCardsAddedListener() {
        return AddCards;
    }

    private void AddCards(object o, CardsAddedToBoardEventArgs e) {
        CheckForRemovedCards();
        if(currentnumberOfCards >= cardPositions.Length) {
            return;
        }
        for(int i = 0; i < e.CardGOs.Length; i++) {
            AddCard(e.CardGOs[i]);
        }
    }

    private void AddCard(GameObject card) {
        GameObject space = FindFirstEmptySpace();
        if(space == null) {
            return;
        }
        card.transform.SetParent(space.transform, false);
    }

    private GameObject FindFirstEmptySpace() {
        for(int i = 0; i < cardPositions.Length; i++) {
            if (cardPositions[i].transform.childCount == 0) {
                return cardPositions[i];
            }
        }
        return null;
    }

    private void CheckForRemovedCards() {
        for(int i = 0; i <= currentnumberOfCards; i++) {
            if(cardPositions[i].transform.childCount == 0) {
                currentnumberOfCards--;
            }
        }
    }
}
