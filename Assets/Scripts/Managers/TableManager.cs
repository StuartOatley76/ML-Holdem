using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;


[RequireComponent(typeof(Factory<Table>))]
public class TableManager : UnitySingleton<TableManager> {
    private const int MAX_NUMBER_OF_PLAYERS_AT_TABLE = 6;
    public int MaxNumberOfPlayersAtTable { get { return MAX_NUMBER_OF_PLAYERS_AT_TABLE; } }


    private const int MIN_IDEAL_NUMBER_OF_PLAYERS = 4;
    public int MinIdealNumberOfPlayersAtTable { get { return MIN_IDEAL_NUMBER_OF_PLAYERS; } }
    private int numberOfTables = 0;     //Number of tables in the tournament

    private List<Table> activeTables = new List<Table>();   //The tables that are still active in the tournament
    private Dictionary<Player, Table> playerPositions = new Dictionary<Player, Table>();    //Stores which table each player is at
    //private Dictionary<Table, int> numberOfPlayersAtTables = new Dictionary<Table, int>();  //Stores how many players are active at each table

    private int numberOfColumns;    //Number of columns of tables in the play area
    private int numberOfRows = 0;   //Number of rows of tables in the play area
    [SerializeField] private float spaceBetweenTables = 1f; //space between each table

    public EventHandler<TableAreaEventArgs> SetTableArea;
    public EventHandler<BlindIncreaseEventArgs> SetStartingBlinds;

    public bool IsHumanPlayerPlaying {
        get {
            if(PlayerManager.instance.HumanPlayer == null) {
                return false;
            }
            if (!playerPositions.ContainsKey(PlayerManager.instance.HumanPlayer)) {
                return false;
            }
            return true;
        }
    }
    protected override void Awake() {
        base.Awake();
        StartCoroutine(WaitBeforeConnectingEvents());
        
    }

    private IEnumerator WaitBeforeConnectingEvents() {
        yield return new WaitUntil(() => Tournament.instance != null 
            && PlayerManager.instance != null);
        Tournament.Instance.NewTournamentEvent += CreateTables;
        PlayerManager.Instance.playersCreatedEvent += PlacePlayersAtTables;
    }

    private void RemovePlayer(object o, EventArgs e) {
        if(!(o is Player)) {
            return;
        }
        Player player = (Player)o;
        if(playerPositions.TryGetValue(player, out Table table)) {
            table.RemovePlayer(player);
            playerPositions.Remove(player);
            player.OutOfTournament -= RemovePlayer;
        }
    }
    private void CreateTables(object sender, NewTournamentEventArgs e) {
        activeTables = new List<Table>();
        numberOfTables = e.NumberOfPlayers / MAX_NUMBER_OF_PLAYERS_AT_TABLE;
        if (e.NumberOfPlayers % MAX_NUMBER_OF_PLAYERS_AT_TABLE != 0) {
            numberOfTables++;
        }

        for (int i = 0; i < numberOfTables; i++) {
            GameObject tableGO = Factory<Table>.Instance.GetInstance().gameObject;
            activeTables.Add(tableGO.GetComponent<Table>());
        }
        PositionTables();
        SetStartingBlinds?.Invoke(this, new BlindIncreaseEventArgs(e.StartingSmallBlind));
    }

    public void Rebalance() {
        TableCamera.CancelCamera();
        List<Player> players = new List<Player>();
        Table humamPlayerTable = Table.HumanPlayerTable;
        foreach(Table table in activeTables) {
            if(humamPlayerTable != null && table == humamPlayerTable) {
                continue;
            }
            players.AddRange(table.RemoveAllPlayers());
        }
        int playersAtHumanTable = 0;
        if(humamPlayerTable != null) {
            playersAtHumanTable = playerPositions.Where(t => t.Value == humamPlayerTable).Select(t => t.Key).Count();
            activeTables.Remove(humamPlayerTable);
        }

        numberOfTables = players.Count + playersAtHumanTable / MAX_NUMBER_OF_PLAYERS_AT_TABLE;
        if (players.Count % MAX_NUMBER_OF_PLAYERS_AT_TABLE != 0) {
            numberOfTables++;
        }
        int numToRemove = activeTables.Count - numberOfTables;
        for (int i = 0; i < numToRemove; i++) {
            Table table = activeTables[activeTables.Count - 1];
            activeTables.Remove(table);
            Destroy(table.gameObject);
        }
        if (humamPlayerTable != null) {
            activeTables.Add(humamPlayerTable);
        }
        PositionTables();
        playerPositions.Clear();
        PlaceplayersAtTables(players);
    }


    /// <summary>
    /// positions tables in the game area
    /// </summary>
    private void PositionTables() {
    numberOfRows = 0;
    float sqroot = Mathf.Sqrt(numberOfTables);
    numberOfColumns = Mathf.RoundToInt(sqroot);

    for (int i = 0, x = 0; i < activeTables.Count; i++) {
        activeTables[i].transform.position = new Vector3(x * spaceBetweenTables, 0, numberOfRows * spaceBetweenTables);
        if (x < numberOfColumns) {
            x++;
            continue;
        }
        numberOfRows++;
        x = 0;
    }

    SetTableArea?.Invoke(this, new TableAreaEventArgs(numberOfColumns, numberOfRows, spaceBetweenTables));
    }

    /// <summary>
    /// Adds players to tables
    /// </summary>
    private void PlacePlayersAtTables(object sender, PlayerCreationEventArgs e) {
        PlaceplayersAtTables(e.Players);
    }

    private void PlaceplayersAtTables(List<Player> players) {
        for (int i = 0; i < players.Count; i++) {
            if (!activeTables[i % activeTables.Count].AddPlayer(players[i])) {
                continue;
            }
            playerPositions.Add(players[i], activeTables[i % activeTables.Count]);
            players[i].OutOfTournament -= RemovePlayer;
            players[i].OutOfTournament += RemovePlayer;
        }
    }
}
