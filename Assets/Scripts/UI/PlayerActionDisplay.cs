using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;
using TMPro;
using System;
using Utils.HelperFunctions;

public class PlayerActionDisplay : UnitySingleton<PlayerActionDisplay>
{

    private Queue<TextMeshProUGUI> displayTexts = new Queue<TextMeshProUGUI>();
    private Dictionary<Player, TextMeshProUGUI> playerDisplays = new Dictionary<Player, TextMeshProUGUI>();
    private Dictionary<Player, (int, GameObject)> playerTables = new Dictionary<Player, (int, GameObject)>();
    [SerializeField] private GameObject displayTextPrefab;
    private bool valid = true;
    private float waitAfterHandEnded = 5f;
    private Coroutine reset;

    private void Start() {
        for(int i = 0; i < TableManager.instance.MaxNumberOfPlayersAtTable; i++) {
            GameObject displayGO = Instantiate(displayTextPrefab, Vector3.zero, Quaternion.identity, transform);
            TextMeshProUGUI text = displayGO.GetComponent<TextMeshProUGUI>();
            if(text != null) {
                displayTexts.Enqueue(text);
            } else {
                Debug.LogError("No text mesh found on display prefab");
                valid = false;
                return;
            }
        }
        CameraTracker.instance.MainCameraActive += Reset;

    }

    public EventHandler<Bet> Connect(Player player, int table, GameObject position) {
        if(playerTables.TryGetValue(player, out _)) {
            playerTables.Remove(player);
            if(playerDisplays.TryGetValue(player, out TextMeshProUGUI text)) {
                playerDisplays.Remove(player);
                displayTexts.Enqueue(text);
                text.enabled = false;
            }
        }
        GameObject textPos = Helpers.FindChildWithTag(position, "ActionTextPosition");
        if (textPos != null) {
            playerTables.Add(player, (table, textPos));
        } else {
            Debug.LogError("Text position not found");
        }
        return ActionMade;
    }

    private void ActionMade(object sender, Bet e) {
        if(!(sender is Player || !valid)) {
            return;
        }
        Player player = (Player)sender;
        if (playerTables.TryGetValue(player, out (int, GameObject) table)) {
            if(table.Item1 != TableCamera.ActiveTable) {
                return;
            }
            TextMeshProUGUI text;
            if (!playerDisplays.TryGetValue(player, out text)) {
                text = displayTexts.Dequeue();
                text.enabled = true;
                playerDisplays.Add(player, text);
            }
            text.text = Enum.GetName(typeof(PokerAction), e.Action);
            if(e.Action != PokerAction.Fold) {
                text.text += " " + e.BetAmount;
            }
            text.transform.position = CameraTracker.instance.CurrentCamera.WorldToScreenPoint(table.Item2.transform.position);
        }
    }

    public EventHandler ConnectReset() {
        return Reset;
    }
    private void Reset(object sender, EventArgs e) {
        if (reset != null) {
            StopCoroutine(reset);
        }
        reset = StartCoroutine(DelayedReset(sender));
    }

    private IEnumerator DelayedReset(object sender) {
        if (sender is Table) {
            Table table = (Table)sender;
            if (table.TableID != TableCamera.ActiveTable) {
                yield break;
            }
            yield return new WaitForSeconds(waitAfterHandEnded);
        }
        
        List<Player> players = new List<Player>();
        foreach (KeyValuePair<Player, TextMeshProUGUI> entry in playerDisplays) {
            displayTexts.Enqueue(entry.Value);
            entry.Value.text = "";
            entry.Value.enabled = false;
            players.Add(entry.Key);
        }
        foreach (Player player in players) {
            playerDisplays.Remove(player);
        }
    }
}
