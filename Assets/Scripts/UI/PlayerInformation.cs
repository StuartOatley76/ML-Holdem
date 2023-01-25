using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerInformation : MonoBehaviour {
    [SerializeField] private GameObject informationHolder;
    [SerializeField] private TextMeshProUGUI playerID;
    [SerializeField] private TextMeshProUGUI playerType;
    [SerializeField] private TextMeshProUGUI playerChips;
    [SerializeField] private TextMeshProUGUI playerPosition;
    private bool hasBeenUsedThisFrame = false;


    public void ShowInformation(Player player) {
        playerID.text = player.PlayerID.ToString();
        Type type = player.GetType();
        if (type == typeof(HumanPlayer)) {
            playerType.text = "Human Player";
        } else if (type == typeof(OutsPlayer)) {
            playerType.text = "ML Outs Player";
        } else if (type == typeof(ScoredPlayer)) {
            playerType.text = "ML Scored player";
        } else if (type == typeof(Player)) {
            playerType.text = "ML Basic Player";
        }
        playerChips.text = player.Stack.ToString();
        if (PlayerManager.Instance != null) {
            playerPosition.text = PlayerManager.Instance.GetPlayerRank(player).ToString() + " / "
                + PlayerManager.Instance.NumberOfPlayersLeftIn.ToString();
        }
        hasBeenUsedThisFrame = true;
        informationHolder.SetActive(true);
    }

    private void LateUpdate() {
        if (!hasBeenUsedThisFrame) {
            playerID.text = string.Empty;
            playerType.text = string.Empty;
            playerChips.text = string.Empty;
            playerPosition.text = string.Empty;
            informationHolder.SetActive(false);
        }
        hasBeenUsedThisFrame = false;
    }
}
