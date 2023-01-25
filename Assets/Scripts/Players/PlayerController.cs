using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;


public class PlayerController : Agent {

    protected Bet bet;
    protected Card[] board;
    protected Card[] pocket;
    protected int stack;
    protected int numberOfPlayersAtTable;
    protected RoundStage stage;
    private int bigBlind;
    protected List<Bet> bets;
    protected int pot;
    protected Player player;
    protected Pocket winningPocket;
    private int currentPrizeInChips = 0;
    protected EventHandler<Bet> ActionEvent;

    protected static int observationSize = 40;
    private void Awake() {
        player = GetComponent<Player>();
    }
    public virtual EventHandler<ActionRequestEventArgs> SetListener(EventHandler<Bet> listener) {

        if (ActionEvent != null) {
            foreach (EventHandler<Bet> d in ActionEvent.GetInvocationList()) {
                ActionEvent -= d;     //No other player should have access to the controller
            }
        }
        ActionEvent += listener;
        return RequestPlayerAction;
    }

    /// <summary>
    /// Adds observations about the state (41 observations)
    /// </summary>
    /// <param name="sensor"></param>
    public sealed override void CollectObservations(VectorSensor sensor) {
        sensor.Reset();
        SetTournamentInformation(sensor); 
        SetGameStateInformation(sensor); 
        CollectAdditionalObservations(sensor);
        SetWinningPocketInformation(sensor);
    }

    protected virtual void SetWinningPocketInformation(VectorSensor sensor) {
        Card[] cards = new Card[Pocket.NumberOfCards];
        if (winningPocket != null) {
            cards = winningPocket.Cards;
        }
        AddCardObservations(cards, Pocket.NumberOfCards, sensor);
    }

    private void SetGameStateInformation(VectorSensor sensor) {
        AddCardObservations(pocket, Pocket.NumberOfCards, sensor);
        AddCardObservations(board, Board.MaxCards, sensor);
        sensor.AddObservation(pot);
        sensor.AddObservation(stack);
        sensor.AddObservation(numberOfPlayersAtTable);
        if (bet != null) {
            sensor.AddObservation(bet.MinAmount);
        } else {
            sensor.AddObservation(-1);
        }
        sensor.AddObservation((int)stage);
        AddBetsInformation(sensor);
    }

    //18
    private void AddBetsInformation(VectorSensor sensor) {
        if(bets == null) {
            return;
        }
        foreach (Bet bet in bets) {
            sensor.AddObservation((int)bet.Action);
            sensor.AddObservation(bet.BetAmount);
            sensor.AddObservation(bet.Player.Stack);
        }
        if (bets.Count < TableManager.Instance.MaxNumberOfPlayersAtTable) {
            for (int i = bets.Count; i < 6; i++) {
                sensor.AddObservation(-1);
                sensor.AddObservation(-1);
                sensor.AddObservation(-1);
            }
        }
    }

    private void AddCardObservations(Card[] cards, int maxNumberOfCards, VectorSensor sensor) {
        if(cards == null) {
            return;
        }
        for (int i = 0; i < cards.Length; i++) {
            sensor.AddObservation((int)cards[i].Rank);
            sensor.AddObservation((int)cards[i].Suit);
        }
        for (int i = cards.Length; i < maxNumberOfCards; i++) {
            sensor.AddObservation(-1);
            sensor.AddObservation(-1);
        }
    }

    /// <summary>
    /// Add additional observations (maximum of 10)
    /// </summary>
    /// <param name="sensor"></param>
    protected virtual void CollectAdditionalObservations(VectorSensor sensor) {
    }

    //4
    private void SetTournamentInformation(VectorSensor sensor) {
        if(PlayerManager.Instance == null || Tournament.Instance == null) {
            return;
        }
        sensor.AddObservation(PlayerManager.Instance.NumberOfPlayersLeftIn);
        sensor.AddObservation(PlayerManager.Instance.GetPlayerRank(player));
        sensor.AddObservation(Tournament.Instance.GetNextPrizePosition());
        sensor.AddObservation(Tournament.Instance.GetNextPrizeAmountInChips() - currentPrizeInChips);
    }

    protected virtual void RequestPlayerAction(object o, ActionRequestEventArgs e) {
        AddBaseInformation(e);
        SetAdditionalInformation(e);
        RequestDecision();
        Academy.Instance.EnvironmentStep();
    }

    protected void AddBaseInformation(ActionRequestEventArgs e) {
        if (e != null) {
            bet = e.Bet;
            if(e.Board == null || e.Board.Cards == null) {
                board = new Card[Board.MaxCards];
            }
            board = e.Board.Cards;
            if (e.Pocket == null || e.Pocket.Cards == null) {
                pocket = new Card[Pocket.NumberOfCards];
            } else {
                pocket = e.Pocket.Cards;
            }
            stack = e.Stack;
            numberOfPlayersAtTable = e.NumberOfPlayersAtTable;
            bets = e.Bets;
            pot = e.PotAmount;
            stage = e.Stage;
            bigBlind = e.BigBlindAmount;
            return;
        }
        bet = null;
        board = new Card[Board.MaxCards];
        pocket = new Card[Pocket.NumberOfCards];
        stack = -1;
        numberOfPlayersAtTable = -1;
        bets = new List<Bet>();
        pot = -1;
        stage = RoundStage.Showdown;
    }

    protected virtual void SetAdditionalInformation(ActionRequestEventArgs e) {
        
    }

    public override void OnActionReceived(float[] vectorAction) {
        bet.Action = (PokerAction)((int)vectorAction[0]);
        vectorAction[1] = Mathf.Abs(vectorAction[1]);
        if (bet.Action != PokerAction.Fold) {
            if(bet.Action == PokerAction.Raise) {
                bet.BetAmount = (int)vectorAction[1] * player.Stack / 100;
                if(bet.BetAmount < bigBlind) {
                    bet.BetAmount = bigBlind;
                }
                bet.BetAmount += bet.MinAmount;
            } else {
                bet.BetAmount = bet.MinAmount;
            }
            if (bet.BetAmount > stack) {
                    bet.IsAllIn = true;
                    bet.BetAmount = player.Stack;
            }
        }
        ActionEvent?.Invoke(this, bet);
    }

    public void GiveRewardOrPunishment(int amount, ActionRequestEventArgs actionArgs) {
        if(Tournament.Instance.GetCurrentPrizeAmountInChips() > currentPrizeInChips) {
            amount += Tournament.Instance.GetCurrentPrizeAmountInChips() - currentPrizeInChips;
            currentPrizeInChips = Tournament.Instance.GetCurrentPrizeAmountInChips();
        }
        AddBaseInformation(actionArgs);
        if(actionArgs is IWinningHandEventArgs) {
            AddWinningHandInformation((IWinningHandEventArgs)actionArgs);
        }
        AddReward(amount);
        EndEpisode();
    }

    protected virtual void AddWinningHandInformation(IWinningHandEventArgs actionArgs) {
        winningPocket = actionArgs.WinningPocket;
    }
}
