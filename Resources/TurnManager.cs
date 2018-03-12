using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class TurnManager : NetworkBehaviour {

    public Deck tileDeck;
    public ScoreManager scoreManager;
    public List<Player> players = new List<Player>();
    [SerializeField]
    public List<bool> playerReadyList = new List<bool>();
    public GameObject tileObject;
    public GameObject gameInfoCanvasObject;
    public Text turnText;
    public Text turnTimerText;
    public Text winnerText;
    public Text topPlayerUsernameText;
    public Text bottomPlayerUsernameText;
    public Text waitingForOpponentText;

    [SyncVar]
    private int turnNumber;
    [SyncVar]
    private float turnTime = 180;
    [SyncVar]
    public bool gameHasStarted = false;
    private bool deckReadyToShuffle = false;
    private bool deckReadyToDeal = false;
    private string turnTimeString;
    private int wallSize = 34;

    public Button LoadGameButton;
    public Button DealWallsButton;
    public Button StartMatchButton;
    public Button ResetGameButton;
    public Button ReadyGameButton;
    public Button UnreadyGameButton;

    private bool opponentIsReady = false;

    public GameObject UsernameInputField;
    public GameObject ToolTip;

    private void Start()
    {
        tileDeck.SetAllTileVisibility(true);
        UsernameInputField.SetActive(false);
        gameInfoCanvasObject.SetActive(true);
        if (!isServer)
        {
            LoadGameButton.gameObject.SetActive(false);
            DealWallsButton.gameObject.SetActive(false);
            StartMatchButton.gameObject.SetActive(false);
            ResetGameButton.gameObject.SetActive(false);
            ReadyGameButton.gameObject.SetActive(true);
            UsernameInputField.transform.position = new Vector3(640, 20);
        }
        else
        {
            //LoadGameButton.gameObject.SetActive(true);
            //DealWallsButton.gameObject.SetActive(true);
            StartMatchButton.gameObject.SetActive(false);
            //ResetGameButton.gameObject.SetActive(true);
            ReadyGameButton.gameObject.SetActive(false);
            UsernameInputField.transform.position = new Vector3(640, 690);
        }
        scoreManager.LoadTileTypes();
    }

    private void Update()
    {
        turnText.text = "Current Turn Number: " + turnNumber;
        //TestRandomHands(50);
        Timer();
        turnTimerText.text = "Time: " + turnTimeString;
        if (isServer)
        {
            if (deckReadyToShuffle && playerReadyList.All(c => c == true))
            {
                Debug.Log("All players are ready to shuffle.");
                tileDeck.ShuffleDeck();
                deckReadyToShuffle = false;
                deckReadyToDeal = true;
                for (int i = 0; i < playerReadyList.Count; i++)
                {
                    playerReadyList[i] = false;
                }

            }
            else if (deckReadyToDeal && playerReadyList.All(c => c == true))
            {
                Debug.Log("All players are ready to deal.");
                DealWalls();
                deckReadyToDeal = false;
            }
            else if (tileDeck.resetHasFinished && playerReadyList.All(c => c == true))
            {
                tileDeck.SetResetHasFinished(false);
                FinishResetMatch();
            }
            if (CheckIfAllPlayersAreReady())
            {
                StartMatchButton.interactable = true;
            }
            else
            {
                StartMatchButton.interactable = false;
            }
        }
    }

    //Keeps a 3 minute timer for turns. Currently does nothing if the timer runs out.
    private void Timer()
    {
        if (gameHasStarted)
        {
            turnTime -= Time.deltaTime;

            int minutes = Mathf.FloorToInt(turnTime / 60F);
            int seconds = Mathf.FloorToInt(turnTime - minutes * 60);

            turnTimeString = string.Format("{0:0}:{1:00}", minutes, seconds);
            if (turnTime < 0)
            {
                LoseByTimeout();
            }
        }
    }
    private void LoseByTimeout()
    {
        winnerText.gameObject.SetActive(true);
        winnerText.transform.position = new Vector3(640, 370);
        winnerText.fontSize = 50;
        winnerText.text = "Game Over! Player " + turnNumber % 2 + " loses by Time Out!";
        gameHasStarted = false;
        turnTime = 180;
    }
    //Spawns the tiles on all clients, then prepares the deck to be shuffled.
    private void SpawnTiles()
    {
        if (isServer)
        {
            foreach (Tile t in tileDeck.GetTiles())
            {
                NetworkServer.Spawn(t.gameObject);
                tileDeck.RpcAddTileToDeck(t.gameObject);
            }
            tileDeck.UpdateTilePositionValues();
            deckReadyToShuffle = true;
        }
    }
    public void SpreadTilesOut()
    {
        if (isServer)
        {
            float xPos = -5f;
            float yPos = 3f;
            foreach (Tile t in tileDeck.GetTiles())
            {
                t.transform.position = new Vector3(xPos, yPos);
                xPos += .5f;
                if (xPos >= 5f)
                {
                    xPos = -5f;
                    yPos--;
                }
            }
        }
    }
    //Deals the players' walls.
    public void DealWalls()
    {
        if (isServer)
        {
            //Keeps track of how many tiles have been used already.
            int iterator = 0;
            //X and Y position of starting hand.
            float x = -4;
            float y = 4;

            players[0].TargetSetPlayerDiscardPosition(players[0].connectionToClient, new Vector3(-4, 1.45f));
            players[0].TargetSetPlayerHandPosition(players[0].connectionToClient, new Vector3(-3, 2.25f));

            if (players.Count > 1)
            {
                players[1].TargetSetPlayerDiscardPosition(players[1].connectionToClient, new Vector3(-4, -1.15f));
                players[1].TargetSetPlayerHandPosition(players[1].connectionToClient, new Vector3(-3, -2));
            }

            tileDeck.SetAllTileVisibility(false);
            foreach (Player player in players)
            {
                player.TargetSetWall(player.connectionToClient, iterator, wallSize);
                player.TargetShowWall(player.connectionToClient);

                tileDeck.TargetUpdateSprites(player.connectionToClient, true, wallSize, iterator);
                tileDeck.Rpc_MoveTiles(x, y, wallSize, iterator);
                iterator += wallSize;
                y -= 7f;
            }
            tileDeck.RpcMoveWallTiles(x - 4.5f, 4.5f, (tileDeck.GetTiles().Count - iterator), iterator, 5);
            //int randomDoraInt = Random.Range(iterator, tileDeck.GetTiles().Count);
           // tileDeck.FlipDoraIndicator(randomDoraInt);
        }
    }
    //Loads the tiles, names them, and adds them to the tiledeck.
    public void LoadTiles()
    {
        waitingForOpponentText.gameObject.SetActive(false);
        if (isServer)
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    GameObject t = (GameObject)Instantiate(tileObject, new Vector3(0, 0), Quaternion.identity) as GameObject;
                    t.GetComponent<Tile>().SetTileInfo(j, "Dots");
                    t.name = "Dots " + j;
                    tileDeck.AddTileToDeck(t.GetComponent<Tile>());

                    t = (GameObject)Instantiate(tileObject, new Vector3(0, 0), Quaternion.identity) as GameObject;
                    t.GetComponent<Tile>().SetTileInfo(j + 10, "Bamboo");
                    t.name = "Bamboo " + (j - 10);
                    tileDeck.AddTileToDeck(t.GetComponent<Tile>());

                    t = (GameObject)Instantiate(tileObject, new Vector3(0, 0), Quaternion.identity) as GameObject;
                    t.GetComponent<Tile>().SetTileInfo(j + 20, "Characters");
                    t.name = "Characters " + (j - 20);
                    tileDeck.AddTileToDeck(t.GetComponent<Tile>());
                }

                GameObject ti = (GameObject)Instantiate(tileObject, new Vector3(0, 0), Quaternion.identity) as GameObject;
                ti.GetComponent<Tile>().SetTileInfo(30, "East Wind");
                ti.name = "East Wind";

                tileDeck.AddTileToDeck(ti.GetComponent<Tile>());
                ti = (GameObject)Instantiate(tileObject, new Vector3(0, 0), Quaternion.identity) as GameObject;
                ti.GetComponent<Tile>().SetTileInfo(31, "South Wind");
                ti.name = "South Wind";
                tileDeck.AddTileToDeck(ti.GetComponent<Tile>());
                ti = (GameObject)Instantiate(tileObject, new Vector3(0, 0), Quaternion.identity) as GameObject;
                ti.GetComponent<Tile>().SetTileInfo(32, "West Wind");
                ti.name = "West Wind";
                tileDeck.AddTileToDeck(ti.GetComponent<Tile>());
                ti = (GameObject)Instantiate(tileObject, new Vector3(0, 0), Quaternion.identity) as GameObject;
                ti.GetComponent<Tile>().SetTileInfo(33, "North Wind");
                ti.name = "North Wind";
                tileDeck.AddTileToDeck(ti.GetComponent<Tile>());

                ti = (GameObject)Instantiate(tileObject, new Vector3(0, 0), Quaternion.identity) as GameObject;
                ti.GetComponent<Tile>().SetTileInfo(40, "Red Dragon");
                ti.name = "Red Dragon";
                tileDeck.AddTileToDeck(ti.GetComponent<Tile>());
                ti = (GameObject)Instantiate(tileObject, new Vector3(0, 0), Quaternion.identity) as GameObject;
                ti.GetComponent<Tile>().SetTileInfo(41, "Green Dragon");
                ti.name = "Green Dragon";
                tileDeck.AddTileToDeck(ti.GetComponent<Tile>());
                ti = (GameObject)Instantiate(tileObject, new Vector3(0, 0), Quaternion.identity) as GameObject;
                ti.GetComponent<Tile>().SetTileInfo(42, "White Dragon");
                ti.name = "White Dragon";
                tileDeck.AddTileToDeck(ti.GetComponent<Tile>());
            }
            SpawnTiles();
        }
        ResetGameButton.enabled = true;
    }
    //Begins the match if there are any players.
    public void StartMatch()
    {
        if (players.Count > 0)
        {
            players[0].TargetSetPlayerTurn(players[0].connectionToClient, true);
            gameHasStarted = true;
            foreach (Player p in players)
            {
                p.TargetStartMatch(p.connectionToClient);
            }
        }
    }
    //Begins resetting the match. Clears the tileDeck and destroys the tiles. Empties players' walls, hands, and discards.
    public void BeginResetMatch()
    {
        if (isServer)
        {
            winnerText.gameObject.SetActive(false);
            Rpc_SyncHideGameWinText();
            gameHasStarted = false;

            foreach (Player p in players)
            {
                p.TargetSetPlayerTurn(players[1].connectionToClient, false);
                p.SetIsPlayerTurn(false);
                p.Rpc_BeginHandReset();
            }
        }
    }
    //Completes the reset by shuffling the tiles and dealing walls.
    public void FinishResetMatch()
    {
        if (isServer)
        {
            turnNumber = 0;
            gameHasStarted = false;
            LoadTiles();
        }
    }
    public void SetReadyForMatch()
    {
        foreach (Player p in players)
        {
            if (p.hasAuthority)
            {
                Debug.Log("Player has authority.");
                p.Cmd_SetReadyForMatch();
            }
        }
    }
    public void SetNotReadyForMatch()
    {
        foreach (Player p in players)
        {
            if (p.hasAuthority)
            {
                Debug.Log("Player has authority.");
                p.Cmd_SetNotReadyForMatch();
            }
        }
    }
    public bool CheckIfAllPlayersAreReady()
    {
        bool allPlayersAreReady = true;
        foreach (Player p in players)
        {
            if (p.hasAuthority)
            {
                if (!(p.GetHand().Count == 13))
                {
                    allPlayersAreReady = false;
                }
            }
            else
            {
                if (!p.GetIsReadyToPlay())
                {
                    allPlayersAreReady = false;
                }
            }
        }
        return allPlayersAreReady;
    }
    public void SetStartMatchButtonInteractability(bool b)
    {
        StartMatchButton.interactable = b;
        if (b)
        {
            StartMatchButton.transform.Find("OpponentIsReadyText").GetComponent<Text>().text = "Your opponent is ready.";
        }
        else
        {
            StartMatchButton.transform.Find("OpponentIsReadyText").GetComponent<Text>().text = "Your opponent isn't ready.";
        }
    }
    [Command]
    public void Cmd_SetStartMatchButtonInteractability(bool b)
    {
        StartMatchButton.interactable = b;
        if (b)
        {
            StartMatchButton.transform.Find("OpponentIsReadyText").GetComponent<Text>().text = "Your opponent is ready.";
        }
        else
        {
            StartMatchButton.transform.Find("OpponentIsReadyText").GetComponent<Text>().text = "Your opponent isn't ready.";
        }
    }
    //Changes player turns.
    public void AdvanceTurns(int discardValue)
    {
        
        if (!players[0].GetIsPlayerTurn())
        {
            CheckForWin(discardValue, 0);
            players[0].TargetSetPlayerTurn(players[0].connectionToClient, true);
            players[0].SetIsPlayerTurn(true);
            players[1].TargetSetPlayerTurn(players[1].connectionToClient, false);
            players[1].SetIsPlayerTurn(false);
            turnNumber++;
        }
        
        else
        {
            CheckForWin(discardValue, 1);
            players[0].TargetSetPlayerTurn(players[0].connectionToClient, false);
            players[0].SetIsPlayerTurn(false);
            players[1].TargetSetPlayerTurn(players[1].connectionToClient, true);
            players[1].SetIsPlayerTurn(true);
            turnNumber++;
        }
        turnTime = 180;
        if (players[0].GetDiscards().Count == 17 &&
                players[1].GetDiscards().Count == 17)
        {
            TieGame();
        }
        
    }
    //Adds new players to the player list. Called from NetworkMessenger. 
    public void UpdatePlayersList()
    {
        players = new List<Player>();
        int iterator = 0;
        foreach (GameObject g in GameObject.FindGameObjectsWithTag("Player"))
        {
            players.Add(g.GetComponent<Player>());
            g.GetComponent<Player>().SetPlayerCount(iterator);
            iterator++;
        }
        Rpc_SyncPlayerList();
        Debug.Log("Number of players: " + players.Count);
        if (players.Count == 2)
        {
            Rpc_SyncUsernameTexts();
            LoadTiles();

        }
        
    }
    [ClientRpc]
    public void Rpc_SyncPlayerList()
    {
        if (!isServer)
        {
            players = new List<Player>();
            int iterator = 0;
            foreach (GameObject g in GameObject.FindGameObjectsWithTag("Player"))
            {
                players.Add(g.GetComponent<Player>());
                g.GetComponent<Player>().SetPlayerCount(iterator);
                iterator++;
            }
            waitingForOpponentText.gameObject.SetActive(false);
        }  
    }

    public void CheckForWin(int discardTileVal, int p)
    {
        Debug.Log("Checking for win...");
        
        Player player = players[p];
        int[] tileIntArray = new int[50];
        foreach (Tile t in player.GetHand())
        {
            tileIntArray[t.GetValue()] += 1;
        }
        tileIntArray[discardTileVal] += 1;
        if (p == 0)
        {
            scoreManager.CheckForFuriten(discardTileVal, players[1].GetHand());
        }
        else
        {
            scoreManager.CheckForFuriten(discardTileVal, players[0].GetHand());
        }
        scoreManager.SetTileArray(tileIntArray);
        scoreManager.CheckForSpecialHands();
        scoreManager.CalculateScore(discardTileVal);
        
    }

    #region HandTesting
    /*public void TestHand(int[] tileArray)
    {
        TileArray = tileArray;

        globalSets = 0;
        globalPairs = 0;

        CheckValue(0);
        handFound = false;

        string s = "";
        int iterator = 1;
        foreach (int i in tileArray)
        {
            s += i + ", ";
            if (iterator % 9 == 0)
            {
                s += "\n";
            }
            iterator++;
        }
        Debug.Log(s);

    }
    public void TestRandomHand()
    {
        int[] tileArray = new int[50];
        for (int i = 0; i < 4; i++)
        {
            int pos = Random.Range(0, 34);
            if (Random.Range(0, 2) == 0 && tileArray[pos] < 2)
            {
                TestAddSet(tileArray, pos);
            }
            else
            {
                TestAddRun(tileArray, pos);
            }
        }

        TestAddPair(tileArray, Random.Range(0, 34));
        TestHand(tileArray);
    }
    public void TestAddSet(int[] tileArray, int pos)
    {
        if (tileArray[pos] < 2 && pos < 34)
        {
            tileArray[pos] += 3;
        }
        else
        {
            if (pos == 33)
            {
                pos = 1;
            }
            TestAddSet(tileArray, pos + 1);
        }

    }
    public void TestAddRun(int[] tileArray, int pos)
    {
        if ((pos >= 25) || (pos > 15 && pos < 18) || (pos > 6 && pos < 9))
        {
            pos = TestReturnValidRunInt();
        }
        tileArray[pos] += 1;
        tileArray[pos + 1] += 1;
        tileArray[pos + 2] += 1;
    }
    public int TestReturnValidRunInt()
    {
        HashSet<int> excludedNumbers = new HashSet<int>() { 7, 8, 16, 17 };
        List<int> range = Enumerable.Range(0, 24).Where(i => !excludedNumbers.Contains(i)).ToList();

        int index = Random.Range(0, 24 - excludedNumbers.Count);

        return range.ElementAt(index);
    }
    public void TestAddPair(int[] tileArray, int pos)
    {
        tileArray[pos] += 2;
    }
    public void TestRandomHands(int numOfTimes)
    {
        for (int i = 0; i < numOfTimes; i++)
        {
            TestRandomHand();
        }
    }*/
    #endregion
    //Iterates through the hand and finds the way the winning hand won, if it did.
    public void WinGame(int playerInt)
    {      
        winnerText.gameObject.SetActive(true);
        winnerText.transform.position = new Vector3(600, 360);
        winnerText.fontSize = 50;
        winnerText.text = "Game Over! " + players[playerInt].GetUsername() + " wins!";
        gameHasStarted = false;
        string user = players[playerInt].GetUsername();
        tileDeck.SetAllTileVisibility(true);
        Rpc_SyncWinGame(user + " wins!");
        if (isServer)
        {
            ResetGameButton.gameObject.SetActive(true);
        }    
    }
    [ClientRpc]
    public void Rpc_SyncWinGame(string username)
    {
        winnerText.gameObject.SetActive(true);
        winnerText.transform.position = new Vector3(600, 360);
        winnerText.fontSize = 50;
        winnerText.text = "Game Over! " + username;
        gameHasStarted = false;
    }
    [ClientRpc]
    public void Rpc_SyncHideGameWinText()
    {
        winnerText.gameObject.SetActive(false);
    }
    public void TieGame()
    {
        winnerText.gameObject.SetActive(true);
        winnerText.transform.position = new Vector3(600, 360);
        winnerText.fontSize = 50;
        winnerText.text = "Game Over! Tie Game!";
        gameHasStarted = false;
        tileDeck.SetAllTileVisibility(true);
        Rpc_SyncWinGame("Tie Game!");
        if (isServer)
        {
            ResetGameButton.gameObject.SetActive(true);
        }
    }
    
    public void AddPlayer(Player player)
    {
        Debug.Log("Player being added.");
        playerReadyList.Add(false);
        UpdatePlayersList();
    }
    public int GetPlayerInt()
    {
        return players.Count;
    }
    public void SetPlayerAsReady(int i)
    {
        Debug.Log("Player " + i + " is ready.");
        playerReadyList[i] = true;
    }
    public void SetPlayerAsUnready(int i)
    {
        Debug.Log("Player " + i + " is not ready.");
        playerReadyList[i] = false;
    }
    public Player GetPlayer(int i)
    {
        return players[i];
    }
    public void SetLocalPlayerUsername(string username)
    {
        foreach(Player p in players)
        {
            if (p.hasAuthority)
            {
                p.SetUsername(username);
                
            }           
        }
    }
    [ClientRpc]
    public void Rpc_SyncUsernameTexts()
    {
        UsernameInputField.gameObject.SetActive(true);
        players[0].SetUsernameText(topPlayerUsernameText);
        players[1].SetUsernameText(bottomPlayerUsernameText);
    }
}
