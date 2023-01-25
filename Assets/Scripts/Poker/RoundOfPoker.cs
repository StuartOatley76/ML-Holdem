using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public enum PokerAction {
    Fold = 0,
    CheckOrCall = 1,
    Raise = 2
}

public enum RoundStage {
    PreFlop = 0,
    Flop,
    Turn,
    River,
    Showdown,
}

public class RoundOfPoker : MonoBehaviour
{

    private List<Player> playersAtStartOfRound;
    private List<Player> playersInCurrentHand;
    private Deck deck;
    private RoundStage stage;
    private Pot pot;
    private int currentPlayer;
    private int bigBlindAmount;
    private int actionsLeft;
    private bool waitingForAction;
    private int dealer;
    private Board board;
    private EventHandler<NewHandEventArgs> OnNewHand; //Event triggered when a new hand is started
    public EventHandler<ShowdownEventArgs> OnShowdown;
    public EventHandler<EventArgs> OnRoundFinish;
    public EventHandler<CardsAddedToBoardEventArgs> OnCardsAddedToBoard;
    private ActionRequestEventArgs latestActionRequestArgs;
    private static int numberOfTablesDelayed = 0;
    private static int framesToWait = 0;
    public int PotValue { get { return pot.AmountInPot; } }
    private int debugTime;
    private int lastPlayerActionprocessedFrom;
    private int stageCounter = 0;
    public bool debug = false;
    private List<Table> tables = new List<Table>();
    private void Awake() {
        deck = new Deck();
        pot = new Pot();
        board = new Board();
    }
    //private void StartNewRound(object o, NewHandEventArgs newHand) {
    //    if (tables.Contains(Table.HumanPlayerTable) && !debug) {
    //        Debug.LogError("Wrong table!!!!");
    //        Debug.LogError("Connected to " + tables.Count);
    //    }
    //    if (debug) {
    //        Debug.Log("Entering start new round");
    //    }
    //    StartCoroutine(DelayedStartNewRound(newHand));
    //}

    //private IEnumerator DelayedStartNewRound(NewHandEventArgs newHand) {
    //    if (debug) {
    //        Debug.Log("Entering Delayed");
    //    }
    //    if (numberOfTablesDelayed < Table.MAX_NUMBER_OF_TABLES_RUNNING_AT_SAME_TIME) {
    //        int i = 0;
    //        while (i < framesToWait) {
    //            i++;
    //            yield return null;
    //        }
    //        framesToWait++;
    //        framesToWait %= Table.MAX_NUMBER_OF_TABLES_RUNNING_AT_SAME_TIME;
    //        numberOfTablesDelayed++;
    //    }
    //    if (debug) {
    //        Debug.Log("Exiting delayed");
    //    }
    //    StartRound(newHand);
    //}

    private void StartRound(object o, NewHandEventArgs newHand) {
        if (debug) {
            Debug.LogError("Starting round");
        }
        DisconnectPlayers();
        if (newHand == null || newHand.InitialBets == null || newHand.PlayersInHand == null || newHand.PlayersInHand.Count == 0) {
            OnRoundFinish?.Invoke(this, EventArgs.Empty);
            return;
        }
        playersInCurrentHand = new List<Player>(newHand.PlayersInHand);
        playersAtStartOfRound = new List<Player>(newHand.PlayersInHand);
        ConnectPlayers();
        OnNewHand?.Invoke(this, newHand);
        dealer = 0;
        if (playersInCurrentHand.Count > 3) {
            dealer = playersInCurrentHand.Count - 3;
        }
        pot.StartNewPot(playersInCurrentHand, newHand.InitialBets, playersInCurrentHand[dealer], newHand.SmallBlindAmount * 2);
        if (newHand.PlayersInHand.Count == 1) {
            FinishRound(playersInCurrentHand[0]);
        }
        bigBlindAmount = newHand.SmallBlindAmount * 2;
        stage = RoundStage.PreFlop;
        deck.Shuffle();
        dealer = 0;
        if (playersInCurrentHand.Count > 3) {
            dealer = playersInCurrentHand.Count - 3;
        }
        currentPlayer = 0;
        board.Reset();
        NextStage();
    }

    public EventHandler<NewHandEventArgs> ConnectToTable(EventHandler<EventArgs> handEnd,
        EventHandler<CardsAddedToBoardEventArgs> cardsAdded, Table table) {
        tables.Add(table);
        OnRoundFinish += handEnd;
        OnCardsAddedToBoard += cardsAdded;
        return StartRound;
    }

    private bool EndStage() {
        if (debug) {
            Debug.Log("Entering end stage");
        }
        if (playersInCurrentHand.Count == 1) {
            if (debug) {
                Debug.Log("Only one player left in, Finishing round");
            }
            FinishRound(playersInCurrentHand[0]);
            return false;
        }
        if(stage == RoundStage.River) {
            if (debug) {
                Debug.Log("Starting showdown");
            }
            StartCoroutine(Showdown());
            return false;
        }
        currentPlayer = dealer + 1;
        currentPlayer %= playersInCurrentHand.Count;
        if (debug) {
            Debug.Log("currentPlayer = " + currentPlayer + ". Increaing stage");

        }
        stage++;
        return true;
    }

    private void ConnectPlayers() {

        foreach(Player player in playersAtStartOfRound) {
            if (debug) {
                Debug.Log("Connecting player " + player.PlayerID);
            }
            player.ActionMade += ProcessAction;
            player.ConnectToRound(out EventHandler<EventArgs> roundFinish, out EventHandler<NewHandEventArgs> newHand);
            OnRoundFinish += roundFinish;
            OnNewHand += newHand;
        }
    }

    private void DisconnectPlayers() {
        if(playersAtStartOfRound == null) {
            return;
        }
        foreach(Player player in playersAtStartOfRound) {
            if (debug) {
                Debug.Log("Disconnecting player " + player.PlayerID);
            }
            player.ActionMade -= ProcessAction;
            player.DisconnectFromRound(out EventHandler<EventArgs> roundFinish, out EventHandler<NewHandEventArgs> newHand);
            OnRoundFinish -= roundFinish;
            OnNewHand -= newHand;
        }
    }
    private void ProcessAction(object sender, Bet bet) {
        if (debug) {
            Debug.Log("Player " + bet.Player.PlayerID + " made bet");
            Debug.Log(Enum.GetName(typeof(PokerAction), bet.Action));
            Debug.Log("min amount = " + bet.MinAmount);
            Debug.Log("bet amount = " + bet.BetAmount);
            Debug.Log("Is all in = " + bet.IsAllIn);
            }
        waitingForAction = false;
        lastPlayerActionprocessedFrom = bet.Player.PlayerID;
        pot.PlaceBet(bet);
        switch (bet.Action) {
            case PokerAction.Fold:
                if (playersInCurrentHand.Count > 1) {
                    if (debug) {
                        Debug.Log("Removing player " + bet.Player);
                    }
                    playersInCurrentHand.Remove(bet.Player);
                    bet.Player.ActionMade -= ProcessAction;
                }
                if(currentPlayer >= playersInCurrentHand.Count) {
                    currentPlayer = 0;
                }
                actionsLeft--;
                if (debug) {
                    Debug.Log("current player = " + playersInCurrentHand[currentPlayer].PlayerID);
                    Debug.Log("Actions left = " + actionsLeft);
                }
                return;
            case PokerAction.CheckOrCall:
                actionsLeft--;
                if (debug) {
                    Debug.Log("actions left = " + actionsLeft);
                }
                break;
            case PokerAction.Raise:
                actionsLeft = playersInCurrentHand.Count - 1;
                if (debug) {
                    Debug.Log("Actions left = " + actionsLeft);
                }
                break;
            default:
                break;
        }
        currentPlayer++;
        currentPlayer %= playersInCurrentHand.Count;
        if (debug) {
            Debug.Log("current player = " + playersInCurrentHand[currentPlayer].PlayerID);
        } 

    }

    /// <summary>
    /// Completes the hand without a showdown
    /// </summary>
    /// <param name="winner"></param>
    private void FinishRound(Player winner) {
        if (debug) {
            Debug.Log("Finishing round, winner = " + winner.PlayerID);
        }
        pot.Payout(winner);
        EndRound();
    }

    /// <summary>
    /// 
    /// </summary>
    private IEnumerator Showdown() {

        yield return pot.Payout();
        ShowdownEventArgs showdownEventArgs = new ShowdownEventArgs(playersInCurrentHand);
        OnShowdown?.Invoke(this, showdownEventArgs);
        foreach (KeyValuePair<Player, int> payout in pot.Payouts) {
            payout.Key.GiveWinnings(payout.Value);
        }

        EndRound();
    }

    private void EndRound() {
        if (debug) {
            Debug.Log("Ending Round");
        }
        debugTime = 0;
        foreach (Player player in playersAtStartOfRound) {
            player.ReturnCardsToDeck(deck);
        }
        foreach(Card card in board.Cards) {
            deck.ReturnCardToDeck(card);
        }

        OnRoundFinish?.Invoke(this, EventArgs.Empty);
    }

    private IEnumerator PlayStage() {
        if (debug) {
            Debug.Log("Starting stage");
        }
        if (playersInCurrentHand.Count == 1) {
            if (debug) {
                Debug.Log("Only one player left");
            }
            FinishRound(playersInCurrentHand[0]);
        }
        actionsLeft = playersInCurrentHand.Count;

        if(stage == RoundStage.PreFlop) {
            actionsLeft -= 1;
        }
        if (debug) {
            Debug.Log(actionsLeft + " actions left");
        }
        while (actionsLeft > 0) {
            if (debug) {
                Debug.Log("Starting next action loop");
            }
            int playersNotAllIn = playersInCurrentHand.Count(p => p.IsAllIn == false);
            if (debug) {
                Debug.Log(playersNotAllIn + " players not all in");
            }
            if(playersNotAllIn <= 1) {
                actionsLeft = 0;
                if (debug) {
                    Debug.Log("Skipping due to all in");
                }
                break;
            }
            waitingForAction = true;
            while (playersInCurrentHand[currentPlayer].IsAllIn) {
                if (debug) {
                    Debug.Log("Changing player due to all in");
                }
                actionsLeft--;
                if(actionsLeft <= 0) {
                    break;
                }
                currentPlayer++;
                currentPlayer %= playersInCurrentHand.Count;
            }
            if (actionsLeft > 0) {
                Bet nextBet = pot.GetNextBet(out List<Bet> bets);
                if(nextBet == null) {
                    if (debug) {
                        Debug.Log("next bet null, skipping");
                    }
                    actionsLeft = 0;
                    continue;
                }
                if (debug) {
                    Debug.Log("Next bet - ");
                    Debug.Log("Min amount = " + nextBet.MinAmount);
                }
                if (playersInCurrentHand.Count <= 1) {
                    if (debug) {
                        Debug.Log("Only one player left in, skipping");
                    }
                    actionsLeft = 0;
                    break;
                }
                latestActionRequestArgs = new ActionRequestEventArgs(
                    nextBet, board, bets, pot.AmountInPot, bigBlindAmount, stage);
                if (debug) {
                    Debug.Log("Requesting action from " + playersInCurrentHand[currentPlayer].PlayerID);
                }
                playersInCurrentHand[currentPlayer].RequestAction(latestActionRequestArgs, stageCounter);
                yield return new WaitWhile(() => waitingForAction);
                if (playersInCurrentHand.Count == 1) {
                    if (debug) {
                        Debug.Log("Only one player left in after wait for action, finishing");
                    }
                    actionsLeft = 0;
                    FinishRound(playersInCurrentHand[0]);
                    yield break;
                }
            }
        }
        if (debug) {
            Debug.Log("moving to end stage");
        }
        if (EndStage()){
            NextStage();
        }
    }

    private void NextStage() {
        stageCounter++;
        if (debug) {
            Debug.Log("Starting stage " + Enum.GetName(typeof(RoundStage), stage));
        }
        switch(stage) {
            case RoundStage.PreFlop:
                DealHoleCards();
                break;
            case RoundStage.Flop:
                AddCardsToBoard(3);
                break;
            case RoundStage.Turn:
                AddCardsToBoard(1);
                break;
            case RoundStage.River:
                AddCardsToBoard(1);
                break;
            case RoundStage.Showdown:
                StartCoroutine(Showdown());
                return;
            default:
                return;
        }
        StartCoroutine(PlayStage());
    }

    private void DealHoleCards() {
        for (int i = 0; i < Pocket.NumberOfCards; i++) {
            foreach (Player player in playersInCurrentHand) {
                Card? card = deck.GetNextCard();
                if (debug) {
                    Debug.Log("Dealing " + card.ToString() + " to " + player.PlayerID);
                }
                if(card == null) {
                    Debug.LogError("No cards in deck!");
                    continue;
                }
                player.GivePocketCard((Card)card, deck.GetCardGO((Card)card));
            }
        }
    }

    private void AddCardsToBoard(int numberOfCards) {
        Card[] cards = new Card[numberOfCards];
        for (int i = 0; i < numberOfCards; i++) {
            Card? card = deck.GetNextCard();
            if (debug) {
                Debug.Log("Dealing " + card.ToString() + " to board");
            }
            if(card == null) {
                continue;
            }
            cards[i] = (Card)card;
        }
        board.AddCards(cards);
        OnCardsAddedToBoard?.Invoke(this, new CardsAddedToBoardEventArgs(cards, deck));
    }

    private void DebugState() {
        debugTime++;
        if (!pot.requestingScores) {
            Debug.LogWarning("Round going for " + debugTime);
            Debug.LogWarning("Round stage - " + Enum.GetName(typeof(RoundStage), stage));
            Debug.LogWarning("Actions left " + actionsLeft);
            Debug.LogWarning("Waiting for action = " + waitingForAction);
            Debug.LogWarning("Last player to make action = " + lastPlayerActionprocessedFrom);
            Debug.LogWarning("Stage counter = " + stageCounter);
            if (waitingForAction) {
                Debug.LogWarning("Waiting for player " + playersInCurrentHand[currentPlayer].PlayerID);
                playersInCurrentHand[currentPlayer].DebugState();
            }

            Debug.LogWarning("Players in hand - ");
            foreach (Player player in playersInCurrentHand) {
                Debug.LogWarning("Player " + player.PlayerID);
                if (!waitingForAction) {
                    player.DebugState();
                }
            }
            Debug.LogWarning("Pot state - ");
            pot.DebugState();
            Debug.LogWarning("Board = ");
            Debug.LogWarning(board.ToString());
        } else {
            pot.DebugScores();
            Debug.LogWarning("Board = ");
            Debug.LogWarning(board.ToString());
            Debug.Break();
        }
        //if(debugTime > 100) {
        //    Debug.Break();
        //}
    }
}

public class CardsAddedToBoardEventArgs {
    public Card[] Cards { get; private set; }
    public GameObject[] CardGOs { get; private set; }

    public CardsAddedToBoardEventArgs(Card[] cards, Deck deck) {
        Cards = cards;
        CardGOs = new GameObject[cards.Length];
        for(int i = 0; i < cards.Length; i++) {
            CardGOs[i] = deck.GetCardGO(cards[i]);
        }
    }
}

public class ShowdownEventArgs {
    public List<Player> PlayersInShowdown { get; private set; }
    public Dictionary<Player, Pocket> PlayersPockets { get; private set; }

    public ShowdownEventArgs(List<Player> players) {
        PlayersInShowdown = players;
        PlayersPockets = new Dictionary<Player, Pocket>();
    }

    public void AddPocket(Player player, Pocket pocket) {
        if (PlayersInShowdown.Contains(player)) {
            PlayersPockets.Add(player, pocket);
        }
    }
}