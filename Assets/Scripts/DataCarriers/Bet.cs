using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bet {
    public Player Player { get; private set; }
    public int MinAmount { get; private set; }

    public PokerAction Action { get; set; }
    public int BetAmount { get; set; } = 0;
    public bool IsAllIn { get; set; } = false;
    private Bet() { }
    public Bet(Player player, int minAmount, PokerAction action = PokerAction.Fold) {
        Player = player;
        MinAmount = minAmount;
        BetAmount = 0;
        Action = action;
    }

}
