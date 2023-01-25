using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Pot 
{

    private class Sidepot {
        public int BetAmount { get; private set; }
        private List<Player> PlayersPaidin { get; set; } = new List<Player>();
        private bool hasBeenPaid = false;
        public Sidepot(int betAmount) {
            BetAmount = betAmount;
        }

        public void AddPlayer(Player player) {
            if (!PlayersPaidin.Contains(player)) {
                PlayersPaidin.Add(player);
            }
        }

        public int GetPotTotal() {
            return BetAmount * PlayersPaidin.Count;
        }

        public bool PayPlayer(Player player) {
            if(hasBeenPaid || !PlayersPaidin.Contains(player)) {
                return false;
            }
            hasBeenPaid = true;
            return true;
        }

        public bool IsPlayerInSidepot(Player player) {
            return PlayersPaidin.Contains(player);
        }

    }

    private List<Player> playersInPot;
    private List<Player> StartingPlayersInPot;
    private Dictionary<Player, List<Bet>> betsMade = new Dictionary<Player, List<Bet>>();
    private int amountInPot = 0;
    public int AmountInPot { get { return amountInPot; } private set { amountInPot = value; 
        if(amountInPot < 0) {
                amountInPot = 0;
            }
        } 
    }
    private int nextPlayerToBet = 0;
    private int lastPlayerToRaise = 0;
    private int currentBetAmount = 0;
    private Player potDealer;
    private List<Sidepot> sidepots = new List<Sidepot>();
    private EventHandler<ScoreRequestEventArgs> requestScoresEvent;
    public List<KeyValuePair<Player, int>> Payouts { get; private set; } = new List<KeyValuePair<Player, int>>();
    public ScoreRequestEventArgs scoreRequestEventArgs { get; private set; }
    public bool requestingScores = false;
    private bool Debugged;

    public void StartNewPot(List<Player> players, List<Bet> initialBets, Player dealer, int bigBlindAmount) {
        betsMade.Clear();
        sidepots.Clear();
        Payouts.Clear();
        AmountInPot = 0;
        if (requestScoresEvent != null) {
            foreach (EventHandler<ScoreRequestEventArgs> del in requestScoresEvent.GetInvocationList()) {
                requestScoresEvent -= del;
            }
        }
        potDealer = dealer;
        nextPlayerToBet = players.FindIndex(player => player == potDealer) + 3;
        nextPlayerToBet %= players.Count();
        lastPlayerToRaise = players.FindIndex(player => player == potDealer) + 2;
        lastPlayerToRaise %= players.Count();
        playersInPot = players;
        StartingPlayersInPot = players;
        currentBetAmount = bigBlindAmount;
        AmountInPot = initialBets.Sum(bet => bet.BetAmount);
        if(AmountInPot < 0) {
            Debug.LogError("Pot at start < 0");
        }
        foreach(Bet bet in initialBets) {
            List<Bet> bets = new List<Bet> {
                bet
            };
            betsMade.Add(bet.Player, bets);
        }
        foreach (Player player in players) {
            requestScoresEvent += player.ConnectToPot();
            if (!betsMade.ContainsKey(player)) {
                betsMade.Add(player, new List<Bet>());
            }
        }
    }

    public Bet GetNextBet(out List<Bet> bets) {
        bets = GetLatestBets();
        int betAmount = GetNextMinimumBetAmount();
        if (lastPlayerToRaise == nextPlayerToBet) {
            PrepareNextBettingRound();
            CheckForOverPayment();
            return null;
        }
        return new Bet(playersInPot[nextPlayerToBet], betAmount);
    }

    private void PrepareNextBettingRound() {
        Player firstPlayer = potDealer;
        int firstPlayerPosition = StartingPlayersInPot.FindIndex(player => player == firstPlayer);
        do {
            firstPlayerPosition++;
            firstPlayerPosition %= StartingPlayersInPot.Count;
            firstPlayer = StartingPlayersInPot[firstPlayerPosition];
        } while (!playersInPot.Contains(firstPlayer));
        nextPlayerToBet = playersInPot.FindIndex(player => player == firstPlayer);
        lastPlayerToRaise = nextPlayerToBet - 1;
        if(lastPlayerToRaise < 0) {
            lastPlayerToRaise = playersInPot.Count - 1;
        }
    }

    private void CheckForOverPayment() {
        if(playersInPot.Count < 2 || amountInPot == 0) {
            return;
        }
        int playersNotAllIn = 0;
        Player notAllIn = null;
        foreach (Player player in playersInPot) {
            if (!player.IsAllIn) {
                notAllIn = player;
                playersNotAllIn++;
            }
        }
        if(playersNotAllIn > 1 || notAllIn == null) {
            return;
        }

        List<KeyValuePair<Player, int>> playersPaidAmounts = new List<KeyValuePair<Player, int>>();
        foreach(KeyValuePair<Player, List<Bet>> pair in betsMade) {
            playersPaidAmounts.Add(new KeyValuePair<Player, int>(pair.Key, pair.Value.Sum(bet => bet.BetAmount)));
        }
        playersPaidAmounts.OrderByDescending(value => value.Value);
        if(playersPaidAmounts[0].Key == notAllIn && playersPaidAmounts[0].Value > playersPaidAmounts[1].Value) {
            int repay = playersPaidAmounts[0].Value - playersPaidAmounts[1].Value;
            repay = (repay > amountInPot) ? amountInPot : repay;
            playersPaidAmounts[0].Key.GiveWinnings(repay);
            AmountInPot -= repay;
        }
    }

    private int GetNextMinimumBetAmount() {
        int amountPaid = 0;
        int playercounter = 0;
        do {
            nextPlayerToBet %= playersInPot.Count;
            if (betsMade.TryGetValue(playersInPot[nextPlayerToBet], out List<Bet> previousBets)) {
                if (previousBets.Count > 0 && previousBets[previousBets.Count - 1].IsAllIn) {
                    nextPlayerToBet++;
                    playercounter++;
                } else {
                    break;
                }
            }
        } while (playercounter < playersInPot.Count);
        nextPlayerToBet %= playersInPot.Count;
        if (betsMade.TryGetValue(playersInPot[nextPlayerToBet], out List<Bet> bets)) {
            amountPaid = bets.Sum(bet => bet.BetAmount);
        }
        return currentBetAmount - amountPaid;
    }

    private List<Bet> GetLatestBets() {
        List<Bet> bets = new List<Bet>();
        foreach(KeyValuePair<Player, List<Bet>> pair in betsMade) {
            if (pair.Value.Count > 0) {
                bets.Add(pair.Value[pair.Value.Count - 1]);
            }
        }
        return bets;
    }

    public void PlaceBet(Bet bet) {
        if (betsMade.TryGetValue(bet.Player, out List<Bet> bets)) {
            bets.Add(bet);
        } else {
            return;
        }
        if (bet.Action == PokerAction.Fold) {
            if (playersInPot.Count > 1) {
                playersInPot.Remove(bet.Player);
                requestScoresEvent -= bet.Player.ConnectToPot();
            }
            return;
        }
        AmountInPot += bet.BetAmount;
        foreach (Sidepot sidepot in sidepots) {
            if (!sidepot.IsPlayerInSidepot(bet.Player)) {
                int amountPaid = 0;
                if (betsMade.TryGetValue(bet.Player, out List<Bet> allBets)) {
                    amountPaid = allBets.Sum(bet => bet.BetAmount);
                }
                foreach(Sidepot side in sidepots) {
                    if(side == sidepot) {
                        continue;
                    }
                    if (side.IsPlayerInSidepot(bet.Player)) {
                        amountPaid -= side.BetAmount;
                    }
                }
                if (amountPaid >= sidepot.BetAmount) {
                    sidepot.AddPlayer(bet.Player);
                }
            }
        }

        if (bet.BetAmount < bet.MinAmount) {
            CreateSidePot(bet.Player);
        }

        if(bet.BetAmount > bet.MinAmount) {
            currentBetAmount += bet.BetAmount - bet.MinAmount;
            lastPlayerToRaise = nextPlayerToBet;
        }
        nextPlayerToBet++;
        nextPlayerToBet %= playersInPot.Count;

    }

    private void CreateSidePot(Player player) {
        int betAmount = 0;
        if(betsMade.TryGetValue(player, out List<Bet> bets)) {
            betAmount = bets.Sum(bet => bet.BetAmount);
        } else {
            return;
        }
        foreach(Sidepot sp in sidepots) {
            if (sp.IsPlayerInSidepot(player)) {
                betAmount -= sp.BetAmount;
            }
        }
        Sidepot sidepot = new Sidepot(betAmount);
        sidepot.AddPlayer(player);
        foreach (Player p in playersInPot) {
            int totalBet = 0;
            if (betsMade.TryGetValue(p, out List<Bet> allBets)) {
                totalBet = allBets.Sum(bet => bet.BetAmount);
            }
            foreach(Sidepot side in sidepots) {
                if (side.IsPlayerInSidepot(p)) {
                    totalBet -= side.BetAmount;
                }
            }
            if(totalBet > betAmount) {
                sidepot.AddPlayer(p);
            }
        }
        if(sidepots.Sum(sp => sp.GetPotTotal()) + sidepot.GetPotTotal() > amountInPot) {
            return;
        }
        sidepots.Add(sidepot);
        sidepots.OrderBy(pot => pot.GetPotTotal());
    }

    public IEnumerator Payout() {
        scoreRequestEventArgs = new ScoreRequestEventArgs(playersInPot);
        requestingScores = true;
        requestScoresEvent?.Invoke(this, scoreRequestEventArgs);
        //Debug.LogWarning("Pot requesting scores");
        yield return new WaitUntil(() => scoreRequestEventArgs.AllScoresAssigned());
        //Debug.LogWarning("Pot recieved scores");
        requestingScores = false;
        Debugged = false;
        List<KeyValuePair<Player, int>> scores = scoreRequestEventArgs.GetScoresDescending();
        for(int i = 0; i < scores.Count; i++) {
            if(betsMade.TryGetValue(scores[i].Key, out List<Bet> bets)){
                if(bets.Sum(bet => bet.BetAmount) >= currentBetAmount) {
                    List<Player> winners = new List<Player>() { scores[i].Key };
                    int score = scores[i].Value;
                    int j = i + 1;
                    while (j < scores.Count && scores[j].Value == scores[i].Value) {
                        winners.Add(scores[j].Key);
                        j++;
                    }
                    foreach (Player player in winners) {
                        Payouts.Add(new KeyValuePair<Player, int>(scores[i].Key, AmountInPot / winners.Count));
                    }
                    AmountInPot = 0;
                    yield break;
                }
                for(int j = 0; j < sidepots.Count; j++) {
                    if (sidepots[j].PayPlayer(scores[i].Key)) {
                        Payouts.Add(new KeyValuePair<Player, int>(scores[i].Key, sidepots[j].GetPotTotal()));
                        AmountInPot -= sidepots[j].GetPotTotal();
                    }
                }
            }
        }
    }

    internal void DebugState() {
        Debug.LogWarning("Pot amount = " + amountInPot);
        Debug.LogWarning("requesting scores = " + requestingScores);
        Debug.LogWarning("Players in pot - ");
        foreach(Player player in playersInPot) {
            Debug.LogWarning("Player " + player.PlayerID);
        }
    }

    internal void DebugScores() {
        if (!Debugged) {
            Debug.LogWarning("Pot scores");
            scoreRequestEventArgs.DebugScores();
            Debugged = true;
        }
    }

    public void Payout(Player player) {
        player.GiveWinnings(AmountInPot);
        AmountInPot = 0;
    }
}
