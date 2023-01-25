using System;

public class NewTournamentEventArgs : EventArgs {
    public int NumberOfPlayers { get; private set; }

    public int StartingStack { get; private set; }
    public bool IncludeHumanPlayer { get; private set; }

    public int StartingSmallBlind { get; private set; }

    public NewTournamentEventArgs(int numberOfPlayers, int startingStack, int startingSmallBlind, bool includeHumanPlayer = false) {
        NumberOfPlayers = numberOfPlayers;
        StartingStack = startingStack;
        IncludeHumanPlayer = includeHumanPlayer;
        StartingSmallBlind = startingSmallBlind;
    }
}
