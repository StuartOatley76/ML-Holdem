using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils.HelperFunctions;
public class CardArrayEventArgs : EventArgs {
    public Card[] Cards { get; set; }
}

[RequireComponent(typeof(PlayerController), typeof(HandFinder), typeof(HandScorer))]
public class Player : MonoBehaviour
{
    public enum PlayerState {
        EnteredRequest,
        RequestedAction,
        ReceivedAction,
        RequestedScore,
        AlreadyHadScore,
        RequestedHandAnalysis,
        ReceivedAnalysis,
        ReceivedOuts,
        CouldntGiveScore
    }

    private static int playerCounter = 0;

    public int PlayerID { get; private set; }
    private PlayerController controller;
    private HandFinder handFinder;
    private HandScorer handScorer;
    protected List<Hand> hands;
    protected Pocket pocket;
    protected Board board;
    public int Stack { get; private set; }

    private int stackAtStartOfHand;
    public bool IsAllIn { get; private set; } = false;

    private EventHandler<HandsEventArgs> RequestAnalysis;
    protected EventHandler<CardsEventArgs> RequestHands;
    protected EventHandler<ActionRequestEventArgs> RequestControllerAction;
    public EventHandler<Bet> ActionMade;
    private GameObject playerPosition;
    private Transform[] cardPositions = new Transform[2];
    private static string holeCard1Tag = "HoleCard1";
    private static string holeCard2Tag = "HoleCard2";

    public EventHandler OutOfTournament;
    private ScoreRequestEventArgs scoreRequestEventArgs;

    protected ActionRequestEventArgs actionArgs;
    private static float playerActionDelay = 10f;
    protected bool gettingScore;
    protected PlayerState state;
    private bool isConnected = false;
    protected int lastStageStarted;
    protected int lastStageCompleted;
    protected int Score {
        get {
            if (hands != null && hands.Count != 0) {
                hands = hands.OrderByDescending(h => h.Score).ToList();
                return hands[0].Score;
            }
            return -1;
        }
    }

    private void Awake() {
        playerCounter++;
        PlayerID = playerCounter;
    }

    protected virtual void Start()
    {
        Stack = Tournament.Instance.StartingStack;
        pocket = new Pocket();
        handScorer = GetComponent<HandScorer>();
        controller = GetComponent<PlayerController>();
        handFinder = GetComponent<HandFinder>();
        RequestHands += handFinder.SetListener(ReceiveHands);
        RequestAnalysis += handScorer.SetListener(ReceiveAnalysedHands);
        RequestControllerAction += controller.SetListener(RecieveAction);
    }

    public void ConnectToRound(out EventHandler<EventArgs> onRoundFinish, 
        out EventHandler<NewHandEventArgs> onNewHand) {
        onRoundFinish = OnEndHand;
        onNewHand = NewHand;
        isConnected = true;
    }
    public void DisconnectFromRound( out EventHandler<EventArgs> onRoundFinish,

        out EventHandler<NewHandEventArgs> onNewHand) {
        onRoundFinish = OnEndHand;
        onNewHand = NewHand;
        isConnected = false;
    }

    public EventHandler<ScoreRequestEventArgs> ConnectToPot() {
        return RequestScore;
    }

    private void RequestScore(object sender, ScoreRequestEventArgs e) {
        state = PlayerState.RequestedScore;
        gettingScore = true;
        if (!e.PlayerNeedsScore(this)) {
            Debug.LogError("Player -" + PlayerID + "doesn't need score???");
            return;
        }
        if(Score != -1) {
            e.AddScore(this, Score);
            state = PlayerState.AlreadyHadScore;
            gettingScore = false;
            return;
        }
        if(pocket == null || board == null || pocket.CountCards() + board.CountCards() < Hand.NUMBER_OF_CARDS_IN_A_HAND) {
            e.AddScore(this, 0);
            state = PlayerState.CouldntGiveScore;
            gettingScore = false;
            return;
        }
        
        scoreRequestEventArgs = e;
        StartCoroutine(GetScore());
    }

    private IEnumerator GetScore() {
        //Debug.LogWarning("Player " + PlayerID + " getting score");
        
        RequestHands?.Invoke(this, new CardsEventArgs(pocket, board));
        yield return new WaitUntil(() => Score != -1);
        //Debug.LogWarning("Player " + PlayerID + " recieved score of " + Score);
        gettingScore = false;
        state = PlayerState.AlreadyHadScore;
        scoreRequestEventArgs.AddScore(this, Score);
    }

    private void ReceiveHands(object o, HandsEventArgs h) {
        //if (gettingScore) {
        //    Debug.LogWarning("Player " + PlayerID + " recieved hands");
        //}
        if(h.Hands == null) {
            Debug.LogError("Hands = null");
        }
        if(h.Hands.Length == 0) {
            Debug.LogError("Hands empty");
        }
        //if (gettingScore) {

        //    foreach (Hand hand in h.Hands) {
        //        foreach (Card card in hand.Cards) {
        //            Debug.LogWarning(card.ToString());
        //        }
        //        Debug.LogWarning(Enum.GetName(typeof(HandRank), hand.handRank));
        //        Debug.LogWarning("Ace low = " + hand.AceLowStraight);
        //    }
        //}
        state = PlayerState.RequestedHandAnalysis;
        RequestAnalysis?.Invoke(this, h);
    }

    public virtual void RequestAction(ActionRequestEventArgs actionRequest, int roundCounter) {
        lastStageStarted = roundCounter;
        if(pocket.Cards.Length != 2) {
            Debug.LogError("Action requested with bad pocket");
        }
        state = PlayerState.EnteredRequest;
        AddActionArgsInfo(actionRequest);
        StartCoroutine(GetAction());
    }

    protected IEnumerator GetAction() {
        if (Tournament.Instance.GoSlow() && 
            PlayerID != PlayerManager.Instance.HumanPlayerID) {
            yield return new WaitForSeconds(playerActionDelay);
        }
        state = PlayerState.RequestedAction;
        RequestControllerAction(this, actionArgs);
    }

    protected void AddActionArgsInfo(ActionRequestEventArgs actionRequest) {
        actionArgs = actionRequest;
        actionArgs.Pocket = pocket;
        actionArgs.Stack = Stack;
        board = actionArgs.Board;
    }

    private void RecieveAction(object o, Bet bet) {
        state = PlayerState.ReceivedAction;
        Stack -= bet.BetAmount;
        if (bet.IsAllIn) {
            IsAllIn = true;
        }
        ActionMade?.Invoke(this, bet);
        lastStageCompleted = lastStageStarted;
    }

    protected virtual void NewHand(object obj, NewHandEventArgs e) {
        stackAtStartOfHand = Stack;
        pocket.Reset();
        foreach (Transform transform in cardPositions) {
            if (transform.childCount > 0) {
                Transform card = transform.GetChild(0);
                card.SetParent(null);
                card.gameObject.SetActive(false);
            }
        }
        HandleBlinds(e);

    }

    private void HandleBlinds(NewHandEventArgs e) {
        if (e.anteAmount > 0) {
            e.AddBlind(PayBlinds(e.anteAmount));
        }
        if (e.BigBlindPlayer == this) {
            e.AddBlind(PayBlinds(e.SmallBlindAmount * 2));
        }
        if (e.SmallBlindPlayer == this) {
            e.AddBlind(PayBlinds(e.SmallBlindAmount));
        }
    }

    public void PlaceAtTable(GameObject position, int tableID) {
        playerPosition = position;
        ActionMade += PlayerActionDisplay.Instance.Connect(this, tableID, position);
        cardPositions[0] = Helpers.FindChildWithTag(playerPosition, holeCard1Tag).transform;
        cardPositions[1] = Helpers.FindChildWithTag(playerPosition, holeCard2Tag).transform;
    }

    public void DeclareWinner() {
        
    }

    private Bet PayBlinds(int blind) {
        if(Stack > blind) {
            Stack -= blind;
            Bet blindBet = new Bet(this, blind, PokerAction.CheckOrCall);
            blindBet.BetAmount = blind;
            return blindBet;
        }

        IsAllIn = true;
        Bet allInBlind = new Bet(this, blind, PokerAction.CheckOrCall);
        allInBlind.BetAmount = Stack;
        allInBlind.IsAllIn = true;
        Stack = 0;
        return allInBlind;

    }

    public void GivePocketCard(Card card, GameObject cardGO) {
        int cardPos = pocket.AddCard(card);
        if(cardPos == -1) {
            return;
        }
        if (cardPositions[cardPos] != null) {
            cardGO.transform.SetParent(cardPositions[cardPos], false);
            cardGO.transform.localPosition = Vector3.zero;
            if (cardPositions[cardPos].parent.parent.gameObject.name == "Player1Pos" ||
                cardPositions[cardPos].parent.parent.gameObject.name == "Player4Pos") {
                cardGO.transform.Rotate(new Vector3(0, 1, 0), 90);
            }
        }
    }

    public void ReturnCardsToDeck(Deck deck) {
        foreach(Card card in pocket.Cards) {
            deck.ReturnCardToDeck(card);
        }
    }

    public void WinTourney() {
        Debug.LogError("Player Won");
        OutOfTourney();
    }

    private void OutOfTourney() {;
        int winnings = Tournament.Instance.GetCurrentPrizeAmountInChips();
        controller.GiveRewardOrPunishment(winnings, actionArgs);
        OutOfTournament?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void ReceiveAnalysedHands(object obj, HandsEventArgs h) {
        //if (gettingScore) {
        //    Debug.LogWarning("Player " + PlayerID + " Received analysed hands");
        //}
        state = PlayerState.ReceivedAnalysis;
        hands = new List<Hand>(h.Hands);
        hands = hands.OrderByDescending(hand => hand.Score).ToList();
        //if (hands == null || hands.Count == 0) {
        //    Debug.LogError("Hands null or empty");
        //}
        //if (gettingScore) {
        //    foreach (Hand hand in hands) {
        //        foreach (Card card in hand.Cards) {
        //            Debug.LogWarning(card.ToString());
        //        }
        //        Debug.LogWarning(Enum.GetName(typeof(HandRank), hand.handRank));
        //        Debug.LogWarning("Ace low = " + hand.AceLowStraight);
        //        Debug.LogWarning("Score = " + Score);
        //    }
        //}
    }

    public void ClearPosition() {
        if (playerPosition != null) {
            Chips chips = playerPosition.GetComponentInChildren<Chips>();
            if (chips != null) {
                chips.Player = null;
            }
        }
        ActionMade = null;
    }

    public void GiveWinnings (int winnings) {
        Stack += winnings;
        if (IsAllIn && Stack > 0) {
            IsAllIn = false;
        }
    }

    private void OnEndHand(object o, EventArgs e) {
        controller.GiveRewardOrPunishment(Stack - stackAtStartOfHand, actionArgs);
        if(Stack <= 0) {
            OutOfTourney();
        }
    }

    internal void DebugState() {
        Debug.LogWarning(Enum.GetName(typeof(PlayerState), state));
        Debug.LogWarning("ConnectedState = " + isConnected);
        Debug.LogWarning("pocket = " + ((pocket == null) ? "null" : pocket.ToString()));
        Debug.LogWarning("last stage started = " + lastStageStarted);
        Debug.LogWarning("last stage completed = " + lastStageCompleted);
        if (hands == null) {
            Debug.LogWarning("Hands = null");
        } else if (hands.Count == 0) {
            Debug.LogWarning("Hands empty");
        } else {
            Debug.LogWarning("Score =" + Score);
        }
    }
}
