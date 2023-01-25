using System;
using System.Collections.Generic;

public class PlayerCreationEventArgs : EventArgs {
    public List<Player> Players { get; private set; }
    public PlayerCreationEventArgs(List<Player> players) {
        Players = players;
    }
}
