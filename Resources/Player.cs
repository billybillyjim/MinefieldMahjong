using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Player : NetworkBehaviour {

    [SerializeField]
    private List<Tile> playerWall = new List<Tile>();
    [SerializeField]
    private List<Tile> playerHand = new List<Tile>();
    [SerializeField]
    private List<Tile> playerDiscards = new List<Tile>();
    private Vector3 playerHandPosition;
    private Vector3 playerDiscardPosition;
    public int score;
    public int cash;
    public int wager;
    [SerializeField]
    private int playerInt = 0;
    public Deck tileDeck;
    public TurnManager manager;
    private bool gameHasStarted = false;
    private bool isTurn = false;
    private bool deckLocalPlayerIsSet = false;
    private bool isConnected = false;
    private bool isSortingHand = false;
    private bool resetHasFinished = false;
    private bool isReadyToPlay = false;

    public Button readyForMatchButton;
    public Text usernameText;

    [SyncVar]
    public string username;

    private void Update()
    {
        if (deckLocalPlayerIsSet == false && tileDeck.localPlayer == null && hasAuthority)
        {
            Debug.Log("Local Player set.");
            tileDeck.SetLocalPlayer(this);
            deckLocalPlayerIsSet = true;
        }
        if (!isConnected && manager != null && hasAuthority)
        {
            Cmd_AddPlayerToManager();
            isConnected = true;
        }
        if (resetHasFinished)
        {
            resetHasFinished = false;
            SetReady(true);
        }

    }
    [Command]
    public void Cmd_AddPlayerToManager()
    {
        manager.AddPlayer(this);
    }
    private void Awake()
    {
        tileDeck = GameObject.Find("Deck").GetComponent<Deck>();
        //PlayerPrefs.DeleteAll();
    }
    public void SetReady(bool b)
    {
        if (hasAuthority && b == true)
        {
            Debug.Log("Player has authority and is ready.");
            Cmd_SetReady();
        }      
        else if(hasAuthority && b == false)
        {
            Cmd_SetUnready();
        } 
    }
    [Command]
    public void Cmd_SetReady()
    {
        manager.SetPlayerAsReady(playerInt);
        isReadyToPlay = true;
    }
    [Command]
    public void Cmd_SetUnready()
    {
        manager.SetPlayerAsUnready(playerInt);
    }

    public override void OnStartClient()
    {
        manager = GameObject.Find("TurnManager").GetComponent<TurnManager>();
        playerInt = manager.GetPlayerInt();
        
        name = "Player " + playerInt;
        //tileDeck = GameObject.Find("Deck").GetComponent<Deck>();
    }

    public void SetWall(int skip, int take)
    {
        if(manager == null)
        {
            manager = GameObject.Find("TurnManager").GetComponent<TurnManager>();
        }
        playerWall = tileDeck.GetTilesList(skip, take);
        int iterator = 0;
        foreach (Tile t in playerWall)
        {
            t.SetPlayerOwner(this);
            t.SetPositionInWall(iterator);
            iterator++;
        }

    }
    [TargetRpc]
    public void TargetSetWall(NetworkConnection target, int skip, int take)
    {
        playerWall = tileDeck.GetTilesList(skip, take);
        int iterator = 0;
        foreach (Tile t in playerWall)
        {
            t.SetPlayerOwner(this);
            t.SetPositionInWall(iterator);
            iterator++;
        }
    }
   
    [TargetRpc]
    public void TargetShowWall(NetworkConnection target)
    {
        //Debug.Log("Player Showing Wall of size: " + playerWall.Count);
        foreach (Tile t in playerWall)
        {
            t.SetTileVisibility(true);
            t.SetPlayerOwner(this);
            if (t.GetHasAuthority())
            {
                Cmd_RemoveLocalAuthority(t.gameObject);
            }
            Cmd_AssignLocalAuthority(t.gameObject);
        }
    }
    
    [TargetRpc]
    public void TargetSetPlayerHandPosition(NetworkConnection target, Vector3 newPos)
    {
        playerHandPosition = newPos;
    }
    [TargetRpc]
    public void TargetSetPlayerDiscardPosition(NetworkConnection target, Vector3 newPos)
    {
        playerDiscardPosition = newPos;
    }
    [TargetRpc]
    public void TargetSetPlayerTurn(NetworkConnection target, bool b)
    {
        isTurn = b;
    }
    [TargetRpc]
    public void TargetStartMatch(NetworkConnection target)
    {
        gameHasStarted = true;
    }

    [ClientRpc]
    public void Rpc_BeginHandReset()
    {
        if (isServer)
        {
            playerHand.Clear();
            playerDiscards.Clear();
            playerWall.Clear();
            tileDeck.Rpc_BeginDeckReset();
            resetHasFinished = true;
        }
        
    }

    public void AddTileToHand(Tile t)
    {
        isSortingHand = true;
        DarkenWallWhileLoading();
        playerHand.Add(tileDeck.GetTiles()[t.GetPositionInDeck()]);
        Cmd_AddTileToHand(t.GetPositionInDeck());       
        playerHandPosition += new Vector3(.5f, 0);      
        if(playerHand.Count == 13 && isServer == false)
        {
            manager.ReadyGameButton.interactable = true;
            Cmd_SetReady();
        } 
    }
    [Command]
    public void Cmd_AddTileToHand(int tileInt)
    {
        if (!playerHand.Contains(tileDeck.GetTiles()[tileInt]))
        {
            playerHand.Add(tileDeck.GetTiles()[tileInt]);
        }
        Rpc_SyncAddTileToHand(tileInt);
    }
    [ClientRpc]
    public void Rpc_SyncAddTileToHand(int tileInt)
    {
        isSortingHand = false;
        RevertWallAfterLoading();
    }
    public void RemoveTileFromHand(Tile t)
    {
        isSortingHand = true;
        DarkenWallWhileLoading();
        playerHand.Remove(tileDeck.GetTiles()[t.GetPositionInDeck()]);
        Cmd_RemoveTileFromHand(t.GetPositionInDeck());
        manager.UnreadyGameButton.gameObject.SetActive(false);
        manager.ReadyGameButton.gameObject.SetActive(true);
        manager.ReadyGameButton.interactable = false;
        isReadyToPlay = false;
        Cmd_SetNotReadyForMatch();
        manager.Cmd_SetStartMatchButtonInteractability(false);
        playerHandPosition -= new Vector3(.5f, 0);
    }
    [Command]
    public void Cmd_RemoveTileFromHand(int tileInt)
    {
        playerHand.Remove(tileDeck.GetTiles()[tileInt]);
        Rpc_SyncRemoveTileFromHand(tileInt);
    }
    [ClientRpc]
    public void Rpc_SyncRemoveTileFromHand(int tileInt)
    {
        isSortingHand = false;
        RevertWallAfterLoading();
    }
    public void DiscardTile(Tile t)
    {
        playerDiscards.Add(t);
        DarkenWallWhileLoading();
        Cmd_DiscardTile(t.GetPositionInDeck());
        playerDiscardPosition += new Vector3(.5f, 0);
        EndTurn(t.GetValue());
    }
    [Command]
    public void Cmd_DiscardTile(int tileInt)
    {
        if (!playerDiscards.Contains(tileDeck.GetTiles()[tileInt]))
        {
            playerDiscards.Add(tileDeck.GetTiles()[tileInt]);
        }
        Rpc_SyncTileDiscard(tileInt);
    }
    [ClientRpc]
    public void Rpc_SyncTileDiscard(int tileInt)
    {
        RevertWallAfterLoading();
    }
    public void SortHand()
    {
        Debug.Log("Sorting hand");
        playerHandPosition -= new Vector3(playerHand.Count * .5f, 0);
        playerHand = tileDeck.SortTiles(playerHand);
        foreach(Tile t in playerHand)
        {
            t.MoveTile(playerHandPosition);
            playerHandPosition += new Vector3(.5f, 0);
        }
        
    }
    private void EndTurn(int discardValue)
    {
        Cmd_EndTurn(discardValue);        
    }
    public void StartTurn()
    {
        isTurn = true;

    }
    [Command]
    public void Cmd_EndTurn(int discardValue)
    {
        manager.AdvanceTurns(discardValue);
    }
    [Command]
    public void Cmd_SetReadyForMatch()
    {
        manager.StartMatchButton.gameObject.SetActive(true);
        manager.SetStartMatchButtonInteractability(true);
        isReadyToPlay = true;
    }
    [Command]
    public void Cmd_SetNotReadyForMatch()
    {
        //manager.StartMatchButton.gameObject.SetActive(false);
        manager.SetStartMatchButtonInteractability(false);
        
        isReadyToPlay = false;
    }
    [Command]
    public void Cmd_AssignLocalAuthority(GameObject obj)
    {
        
        NetworkInstanceId nIns = obj.GetComponent<NetworkIdentity>().netId;
        GameObject client = NetworkServer.FindLocalObject(nIns);
        NetworkIdentity ni = client.GetComponent<NetworkIdentity>();
        if (!ni.hasAuthority && ni.clientAuthorityOwner == null)
        {
            ni.AssignClientAuthority(connectionToClient);
        }
        else
        {
            if (ni.hasAuthority)
            {
               // Debug.Log("Failed to assign authority to " + obj.name + ", due to already having authority.");
            }
            else if(ni.clientAuthorityOwner != null)
            {
               // Debug.Log("Failed to assign authority to " + obj.name + ", due to the authority owner not being null.");
            }
        }
    }

    [Command]
    public void Cmd_RemoveLocalAuthority(GameObject obj)
    {
        NetworkInstanceId nIns = obj.GetComponent<NetworkIdentity>().netId;
        GameObject client = NetworkServer.FindLocalObject(nIns);
        NetworkIdentity ni = client.GetComponent<NetworkIdentity>();
        if (ni.hasAuthority && ni.clientAuthorityOwner != null)
        {
            ni.RemoveClientAuthority(ni.clientAuthorityOwner);
        }
        else
        {
            if (!ni.hasAuthority)
            {
                Debug.Log("Failed to remove authority from " + obj.name + ", due to not having authority.");
            }
            else if (ni.clientAuthorityOwner == null)
            {
                Debug.Log("Failed to remove authority from " + obj.name + ", due to the authority owner being null.");
            }
        }
    }
    public List<Tile> SortTiles(List<Tile> tiles)
    {
        tiles.Sort((a, b) => a.GetComponent<Tile>().GetValue().CompareTo(b.GetComponent<Tile>().GetValue()));

        return tiles;
    }
    public List<Tile> GetWall()
    {
        return playerWall;
    }
    public Vector3 GetPlayerHandPosition()
    {
        return playerHandPosition;
    }
    public Vector3 GetPlayerDiscardPosition()
    {
        return playerDiscardPosition;
    }
    public List<Tile> GetHand()
    {
        return playerHand;
    }
    public List<Tile> GetDiscards()
    {
        return playerDiscards;
    }
    public void SetPlayerCount(int i)
    {
        playerInt = i;
    }
    public bool GetIsPlayerTurn()
    {
        return isTurn;
    }
    public void SetIsPlayerTurn(bool b)
    {
        isTurn = b;
    }
    public int GetPlayerInt()
    {
        return playerInt;
    }
    public bool GetGameHasStarted()
    {
        return manager.gameHasStarted;
    }
    public string GetUsername()
    {
        return username;
    }
    public void SetUsername(string s)
    {
        username = s;
        Cmd_SyncUsername(username);
        Debug.Log(username);
    }
    public bool GetIsSortingHand()
    {
        return isSortingHand;
    }
    public void DarkenWallWhileLoading()
    {
        foreach(Tile t in playerWall)
        {
            t.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, .5f);
        }
    }
    public void RevertWallAfterLoading()
    {
        foreach (Tile t in playerWall)
        {
            t.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);
        }
    }
    public void SetUsernameText(Text text)
    {
        usernameText = text;
        username = text.text;
    }
    [ClientRpc]
    public void Rpc_SyncUsername(string name)
    {
        usernameText.text = name;
    }
    [Command]
    public void Cmd_SyncUsername(string name)
    {
        usernameText.text = name;
        username = name;
        Rpc_SyncUsername(name);
    }
    public bool GetIsReadyToPlay()
    {
        return isReadyToPlay;
    }
}
