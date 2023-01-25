using System;
using System.Collections.Generic;

/// <summary>
/// Event args to handle new hands
/// </summary>
public class NewHandEventArgs : EventArgs
{
    public Player BigBlindPlayer { get; private set; } //Player in the big blind
    public Player SmallBlindPlayer { get; private set; } //Player in the small blind
    public int BlindsPayed { get; private set; } = 0; //How much has been payed into the blinds/ante
    public int SmallBlindAmount { get; private set; } //Cost of the small blind
    public int anteAmount { get; private set; } //Cost of the ante

    public List<Player> PlayersInHand { get; private set; }
    public List<Bet> InitialBets { get; private set; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="bb">Player in the big blind position</param>
    /// <param name="sb">Player in the small blind position</param>
    /// <param name="sbAmount">Cost of the small blind</param>
    /// <param name="ante">Cost of the ante if any</param>
    public NewHandEventArgs(List<Player> players, Player bb, Player sb, int sbAmount, int ante = 0) {
        PlayersInHand = players;
        BigBlindPlayer = bb;
        SmallBlindPlayer = sb;
        SmallBlindAmount = sbAmount;
        anteAmount = ante;
        InitialBets = new List<Bet>();
    }

    /// <summary>
    /// Adds to the amount payed for the blinds/ante
    /// </summary>
    /// <param name="blindAmount">Amount payed</param>
    public void AddBlind(Bet potEntry) {
        InitialBets.Add((potEntry));
    }
}
