using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils.HelperFunctions;
using Utils;
/// <summary>
/// Class to handle poker table
/// </summary>
[RequireComponent(typeof(RoundOfPoker), typeof(BoardUI))]
public class Table : MonoBehaviour
{
    private static int counter = 0;
    public static Table HumanPlayerTable { get; private set; }
    private int handCount = 0;
    private List<Player> players; //List of the players at the table
    private List<GameObject> playerPositions; //Gameobject of the player position
    private List<string> playerPositionTags;  //List of tags for player positions
    private Dictionary<GameObject, Player> playersAtPositions; //
    private static Queue<Table> waitingTables = new Queue<Table>();
    private int SmallBlind { get; set; } //Cost of the small blind
    
    private RoundOfPoker game;
    private BoardUI boardUI;
    private EventHandler<NewHandEventArgs> newHandEvent;
    private EventHandler handEndedEvent;
    public int NumberOfPlayersAtTable { get { return players.Count; } }

    private const int framesToWaitAfterCreation = 2;

    public const int MAX_NUMBER_OF_TABLES_RUNNING_AT_SAME_TIME = 10;
    private static List<Table> activeTables = new List<Table>();
    private static bool waitForRebalance = false;
    public int TableID { get; private set; }
    private static bool started = false;
    private static bool gettingTables;
    private static Coroutine startingTables;
    private void Awake() {
        counter++;
        gameObject.name = "Table " + counter;
        TableID = counter;
        players = new List<Player>();
        playerPositions = new List<GameObject>();
        playerPositionTags = new List<string>(new string[] {
        "Player1Pos", "Player2Pos", "Player3Pos", "Player4Pos", "Player5Pos", "Player6Pos"
        });
        playersAtPositions = new Dictionary<GameObject, Player>();
        StartCoroutine(WaitBeforeConnectingEvents());
        waitingTables.Enqueue(this);
    }

    private IEnumerator WaitBeforeConnectingEvents() {
        if (TableManager.Instance == null) {
            yield return new WaitUntil(() => TableManager.Instance != null);
        }
        TableManager.Instance.SetStartingBlinds += IncreaseBlinds;
        if (Tournament.Instance == null) {
            yield return new WaitUntil(() => Tournament.Instance != null);
        }
        Tournament.Instance.BlindIncrease += IncreaseBlinds;
        if(PlayerActionDisplay.Instance == null) {
            yield return new WaitUntil(() => PlayerActionDisplay.Instance != null);
        }
        if(PlayerManager.Instance == null) {
            yield return new WaitUntil(() => PlayerManager.Instance != null);
        }
        PlayerManager.Instance.TournamentFinished += Clear;
        handEndedEvent += PlayerActionDisplay.Instance.ConnectReset(); 
    }

    private void IncreaseBlinds(object sender, BlindIncreaseEventArgs e) {
        SmallBlind = e.SmallBlind;
    }

    /// <summary>
    /// Finds the player positions
    /// </summary>
    void Start()
    {
        if (Tournament.Instance.HasHumanPlayer) {
            List<GameObject> holeCardHolders = Helpers.FindChildrenWithTag(gameObject, "Hole Card Holder");
            foreach (GameObject holder in holeCardHolders) {
                holder.transform.Rotate(new Vector3(0, 0, 1), 180);
            }
        }
        for (int i = 0; i < playerPositionTags.Count; i++) {
            playerPositions.Add(Helpers.FindChildWithTag(gameObject, playerPositionTags[i]));
        }
        game = GetComponent<RoundOfPoker>();
        boardUI = GetComponent<BoardUI>();
        if(this == HumanPlayerTable) {
            Debug.LogError("Connecting to round");
        }
        newHandEvent += game.ConnectToTable(EndHand, boardUI.GetCardsAddedListener(), this);
        if (!started) {
            started = true;
            StartCoroutine(WaitForGameStart());
        }
    }

    private IEnumerator WaitForGameStart() {
        int i = 0;
        while(i < framesToWaitAfterCreation) {
            i++;
            yield return null;
        }
        if(startingTables != null) {
            yield break;
        }
        if (startingTables == null) {
            Debug.LogError("Starting tables from Wait for game start");
            startingTables = StartCoroutine(StartTables());
        }
    }


    private static IEnumerator StartTables() {
        if(HumanPlayerTable != null) {
            Debug.LogError("Starting human table");
            HumanPlayerTable.NewHand();
        }
        if (activeTables.Count < MAX_NUMBER_OF_TABLES_RUNNING_AT_SAME_TIME) {
            for (int j = activeTables.Count; j <= MAX_NUMBER_OF_TABLES_RUNNING_AT_SAME_TIME; j++) {
                if (waitingTables.Count > 0) {
                    Table table = GetNextTable();
                    if(table == null) {
                        yield break;
                    }
                    table.NewHand();
                    yield return null;
                }
            }
        }
        startingTables = null;
    }

    private static Table GetNextTable() {
        if (gettingTables) {
            return null;
        }
        gettingTables = true;
        Table table = null;
        int count = waitingTables.Count;
        for(int i = 0; i < count && table == null; i++) { 
            table = waitingTables.Dequeue();
        }
        gettingTables = false;
        return table;
    }
    private void EndHand(object o, EventArgs e) {
        activeTables.Remove(this);
        handEndedEvent?.Invoke(this, EventArgs.Empty);
        waitingTables.Enqueue(this);
        CheckForRebalance();
        if (waitForRebalance) {
            return;
        }
        if(HumanPlayerTable != null && HumanPlayerTable == this) {
            if (startingTables == null) {
              Debug.LogError("Starting tables from EndHand");
              startingTables = StartCoroutine(StartTables());
            }
        }
        Table table = null;
        while (table == null) {
            if (waitingTables.Count == 0) {
                return;
            }
            table = waitingTables.Dequeue();
        }
        table.NewHand();
    }

    private void CheckForRebalance() {
        if (waitForRebalance) {
            return;
        }
        if(waitingTables.Count + activeTables.Count == 1) {
            return;
        }

        if (players.Count < TableManager.Instance.MinIdealNumberOfPlayersAtTable) {
            StartCoroutine(WaitForRebalance());
        }
    }

    private IEnumerator WaitForRebalance() {
        waitForRebalance = true;
        while (activeTables.Count > 0) {
            yield return null;
        }
        //yield return new WaitUntil(() => numberOfTablesCurrentlyRunning == 0);
        yield return new WaitForSeconds(5);
        TableManager.Instance.Rebalance();
        waitForRebalance = false;
        started = false;
        if (startingTables == null) {
            Debug.LogError("Startiong tables from WaitForRebalance");
            startingTables = StartCoroutine(StartTables());
        }
    }

    /// <summary>
    /// Adds a player to the table
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public bool AddPlayer(Player player) {
        if(players.Count >= TableManager.Instance.MaxNumberOfPlayersAtTable) {
            return false;
        }
        GameObject position = GetFreePosition();
        if(position == null) {
            return false;
        }
        players.Add(player);
        playersAtPositions.Add(position, player);
        player.transform.SetParent(position.transform, false);
        Chips chips = position.GetComponentInChildren<Chips>();
        if(chips != null) {
            chips.Player = player;
        }
        foreach(Renderer renderer in position.GetComponentsInChildren<Renderer>()) {
            renderer.enabled = true;
        }
        if(player.PlayerID == PlayerManager.Instance.HumanPlayerID) {
            HumanPlayerTable = this;
            game.debug = true;
            waitingTables = new Queue<Table>(waitingTables.Where(t => t != this));
        }
        player.PlaceAtTable(position, TableID);
        return true;

    }

    /// <summary>
    /// Finds a free place at the table
    /// </summary>
    /// <returns>gameobject for position at table</returns>
    private GameObject GetFreePosition() {
        for(int i = 0; i < playerPositions.Count; i++) {
            if (!playersAtPositions.ContainsKey(playerPositions[i])) {
                return playerPositions[i];
            }
        }
        return null;
    }

    /// <summary>
    /// removes a player from the table
    /// </summary>
    /// <param name="player">The player to remove</param>
    public void RemovePlayer(Player player) {
        KeyValuePair<GameObject, Player> entry;
        try {
            entry = playersAtPositions.First(go => go.Value == player);
        } catch {
            return;
        }
        Chips chips = entry.Key.GetComponent<Chips>();
        if(chips != null) {
            chips.Player = null;
        }
        foreach(Renderer renderer in entry.Key.GetComponentsInChildren<Renderer>()) {
            renderer.enabled = false;
        }
        if(HumanPlayerTable == this && player.PlayerID == PlayerManager.Instance.HumanPlayerID) {
            HumanPlayerTable = null;
        }
        playersAtPositions.Remove(entry.Key);
        players.Remove(player);
        player.ClearPosition();
    }

    /// <summary>
    /// Starts a new hand
    /// </summary>
    private void NewHand() {
        if(activeTables.Count > MAX_NUMBER_OF_TABLES_RUNNING_AT_SAME_TIME || waitForRebalance) {
            waitingTables.Enqueue(this);
            return;
        }
        if (players.Count < 2) {
            waitingTables.Enqueue(this);
            Debug.LogError("Starting tables from new hand");
            startingTables = StartCoroutine(StartTables());
            return;
        }
        if(HumanPlayerTable != null && HumanPlayerTable != this && handCount >= HumanPlayerTable.handCount) {
            for(int i = 0; i < waitingTables.Count; i++) {
                Table nextTable = waitingTables.Dequeue();
                if(nextTable.handCount < HumanPlayerTable.handCount) {
                    nextTable.NewHand();
                    return;
                }
                waitingTables.Enqueue(nextTable);
            }
            return;
        }
        handCount++;
        activeTables.Add(this);
        Player newBB = players[0];
        players.RemoveAt(0);
        players.Add(newBB);
        NewHandEventArgs newHand = new NewHandEventArgs(players, players[players.Count - 1], players[players.Count - 2], SmallBlind);
        if(this == HumanPlayerTable) {
            Debug.LogError("Invoking New Hand Event");
            Debug.LogError("Connected to " + newHandEvent?.GetInvocationList().Count());
        }
        newHandEvent?.Invoke(this, newHand);
    }


    public List<Player> RemoveAllPlayers() {
        List<Player> playersToRemove = new List<Player>(players);
        foreach(Player player in playersToRemove) {
            RemovePlayer(player);
        }
        handCount = 0;
        return playersToRemove;
    }



    private void OnDestroy() {
        if (Tournament.Instance) {
            Tournament.Instance.BlindIncrease -= IncreaseBlinds;
        }
        if (PlayerManager.Instance) {
            PlayerManager.Instance.TournamentFinished -= Clear;
        }
    }

    private void Clear(object sender, TournamentFinishedEventArgs e) {
        counter = 0;
        HumanPlayerTable = null;
        waitingTables.Clear();
        activeTables.Clear();
        waitForRebalance = false;
        started = false;
        Destroy(gameObject);
    }
}
