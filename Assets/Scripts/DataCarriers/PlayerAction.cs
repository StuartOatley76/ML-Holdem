using System;

public class PlayerAction : EventArgs {
    public PokerAction Action { get; private set; }
    public int AmountPaid { get; private set; }

    public int RaiseAmount { get; private set; }

    public PlayerAction(PokerAction action, int amountPaid, int raiseAmount = 0) {
        Action = action;
        AmountPaid = amountPaid;
        RaiseAmount = raiseAmount;
    }
}