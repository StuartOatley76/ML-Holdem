using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class ScoreRequestEventArgs : EventArgs
{
    private Dictionary<Player, int> playerScores;

    public ScoreRequestEventArgs(List<Player> players) {

        playerScores = new Dictionary<Player, int>();
        foreach(Player player in players) {
            if (!playerScores.ContainsKey(player)) {
                playerScores.Add(player, -1);
            }
        }
    }

    public bool PlayerNeedsScore(Player player) {
        return playerScores.ContainsKey(player);
    }
    public bool AddScore(Player player, int score) {
        if (playerScores.ContainsKey(player)) {
            playerScores[player] = score;
            return true;
        }
        return false;
    }

    public bool AllScoresAssigned() {
        foreach(KeyValuePair<Player, int> pair in playerScores) {
            if(pair.Value == -1) {
                return false;
            }
        }
        return true;
    }

    public List<KeyValuePair<Player, int>> GetScoresDescending() {
        return playerScores.OrderByDescending(score => score.Value).ToList();
    }

    public void DebugScores() {
        foreach(KeyValuePair<Player, int> pair in playerScores) {
            Debug.LogWarning("Player " + pair.Key.PlayerID + " has score of " + pair.Value);
            if(pair.Value == -1) {
                pair.Key.DebugState();
            }
        }
    }
}
