using System;
using UnityEngine;

/// <summary>
/// EventArgs for when a player is placed at a table;
/// </summary>
public class PlayerTableEventArgs : EventArgs {
    public Player Player { get; private set; }
    public GameObject PlayerPosition { get; private set; }

    public PlayerTableEventArgs(Player player, GameObject playerPosition) {
        Player = player;
        PlayerPosition = playerPosition;
    }
}
