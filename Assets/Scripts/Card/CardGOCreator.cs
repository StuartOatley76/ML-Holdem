using System.Text;
using UnityEngine;

/// <summary>
/// Creates a card ganeobject from a Card
/// </summary>
public static class CardGOCreator
{
    private const string basePath = "PlayingCards/Resources/Prefab/BackColor_Black/";
    private const string cardPrefix = "Black_PlayingCards_";
    private const string cardSuffix = "_00";

    /// <summary>
    /// Calculates the path to the card prefab and instantiates it
    /// </summary>
    /// <param name="card"></param>
    /// <returns></returns>
    public static GameObject InstantiateCardGO(Card card) {
        StringBuilder path = new StringBuilder(basePath);
        path.Append(cardPrefix);
        GetCardString(path, card);
        path.Append(cardSuffix);
        GameObject go = GameObject.Instantiate(Resources.Load(path.ToString(), typeof(GameObject))) as GameObject;
        go.SetActive(false);
        return go;
    }

    /// <summary>
    /// Adds the Card string section to the path
    /// </summary>
    /// <param name="path">The path stringbuilder</param>
    /// <param name="card">the card</param>
    private static void GetCardString(StringBuilder path, Card card) {
        switch (card.Suit) {
            case Suit.clubs:
                path.Append("Club");
                break;
            case Suit.diamonds:
                path.Append("Diamond");
                break;
            case Suit.hearts:
                path.Append("Heart");
                break;
            case Suit.spades:
                path.Append("Spade");
                break;
            default:
                path.Append("Club");
                break;
        }

        if(card.Rank == Rank.ace) {
            path.Append("01");
            return;
        }

        path.Append(((int)card.Rank).ToString("D2"));
        return;
    }
}
