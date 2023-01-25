using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

public class PlayerManager : UnitySingleton<PlayerManager>
{
    [SerializeField] private GameObject humanPlayerPrefab;
    private List<Factory<Player>> factories = new List<Factory<Player>>();
    private List<Player> players = new List<Player>();

    public EventHandler<PlayerCreationEventArgs> playersCreatedEvent;
    public EventHandler<TournamentFinishedEventArgs> TournamentFinished;
    public HumanPlayer HumanPlayer { get; private set; }
    public int HumanPlayerID {
        get { 
            if(HumanPlayer == null) {
                return -1;
            }
            return HumanPlayer.PlayerID;
        } 
    }
    protected override void Awake() {
        base.Awake();
        factories.AddRange(GetComponents<PlayerFactory>());
        StartCoroutine(WaitBeforeConnectingEvents());
    }

    private IEnumerator WaitBeforeConnectingEvents() {
        yield return new WaitUntil(() => Tournament.instance != null);
        Tournament.instance.NewTournamentEvent += CreatePlayers;
    }

    public int NumberOfPlayersLeftIn { get { return players.Count; } }

    private void CreatePlayers(object sender, NewTournamentEventArgs e) {
        if(factories == null || factories.Count == 0) {
            return;
        }
        if (Tournament.instance.HasHumanPlayer) {
            HumanPlayer = Instantiate(humanPlayerPrefab, Vector3.zero, Quaternion.identity).GetComponent<HumanPlayer>();
            players.Add(HumanPlayer);
        }
        for(int i = 0; i < e.NumberOfPlayers; i++) {
            int factoryToUse = UnityEngine.Random.Range(0, factories.Count);
            Player player = factories[factoryToUse].GetInstance();
            players.Add(player);
            player.OutOfTournament += RemovePlayer;

        }
        StartCoroutine(WaitBeforeTriggeringEvent(new PlayerCreationEventArgs(players)));
        
    }

    private IEnumerator WaitBeforeTriggeringEvent(PlayerCreationEventArgs playerCreationEventArgs) {
        yield return new WaitUntil(() => playersCreatedEvent != null);
        playersCreatedEvent?.Invoke(this, playerCreationEventArgs);
    }

    private void RemovePlayer(object sender, EventArgs e) {
        if(!(sender is Player) || players.Count == 1) {
            return;
        }
        players.Remove((Player)sender);
        if(players.Count == 1) {
            players[0].WinTourney();
            TournamentFinished.Invoke(this, new TournamentFinishedEventArgs(players[0]));
            players.RemoveAt(0);
        }
    }

    public int GetPlayerRank(Player player) {
        players.OrderByDescending(playerPos => playerPos.Stack);
        return players.FindIndex(p => p == player);
    }

    
}
