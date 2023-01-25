using System;
public class TournamentFinishedEventArgs : EventArgs {
    public Player Winner { get; private set; }

    public TournamentFinishedEventArgs(Player winner) {
        Winner = winner;
    }
}