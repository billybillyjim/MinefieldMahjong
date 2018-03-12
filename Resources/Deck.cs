using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class Deck : NetworkBehaviour {

    [SerializeField]
    private List<Tile> tileDeck = new List<Tile>();
    [SerializeField]
    private List<Sprite> tileSprites = new List<Sprite>(50);
    [SerializeField]
    private Sprite blankSprite;
    private static System.Random rng = new System.Random();
    public bool isShuffling = false;
    public Player localPlayer;
    private Tile doraIndicator;
    private Tile reverseDoraIndicator;

    public bool resetHasFinished = false;

    public void AddTileToDeck(Tile t)
    {
        tileDeck.Add(t);
    }

    private void LoadTileSprites()
    {
        for (int i = 1; i < 44; i++)
        {
            if(i < 10)
            {   
                tileSprites.Add(Resources.Load("Tiles/" + "Dots " + i, typeof(Sprite)) as Sprite);
            }
            else if(i >= 10 && i < 20)
            {
                tileSprites.Add(Resources.Load("Tiles/" + "Bamboo " + i, typeof(Sprite)) as Sprite);
            }
            else if (i >= 20 && i < 30)
            {
                tileSprites.Add(Resources.Load("Tiles/" + "Characters " + i, typeof(Sprite)) as Sprite);
            }
            else if (i >= 30 && i < 40)
            {
                tileSprites.Add(Resources.Load("Tiles/" + "Wind " + i, typeof(Sprite)) as Sprite);
            }
            else if (i >= 40)
            {            
                tileSprites.Add(Resources.Load("Tiles/" + "Dragon " + i, typeof(Sprite)) as Sprite);
            }
            //Fixes Wind and Dragon Sprite Loading. 
            if(i == 29)
            {
                tileSprites.Add(new Sprite());
            }
        }
    }

    private void Start()
    {
        LoadTileSprites();           
    }
    //Returns the entire tile deck.
    public List<Tile> GetTiles()
    {
        return tileDeck;
    }
    //Creates an array of random numbers for shuffling, then calls an RPC to shuffle the deck.
    public void ShuffleDeck()
    {
        int count = tileDeck.Count;
        Debug.Log(tileDeck.Count);
        int[] randomNumbersArray = new int[150];
        while (count > 1)
        {
            count--;
            int i = rng.Next(count + 1);
            randomNumbersArray[count] = i;
        }

        Rpc_SyncTilePositionValues(randomNumbersArray);
    }

    public Sprite GetSprite(Tile t)
    {
        return tileSprites[t.GetValue()];
    }
    public Sprite GetBlankSprite()
    {
        return blankSprite;
    }
    public List<Sprite> GetSprites()
    {
        return tileSprites;
    }
    //Sets tile sprite to visible or blank.
    public void SetTileVisibility(bool b, Tile t)
    {
        if (b)
        {
            t.gameObject.GetComponent<SpriteRenderer>().sprite = tileSprites[t.GetValue()];
        }
        else
        {
            t.gameObject.GetComponent<SpriteRenderer>().sprite = blankSprite;
        }
    }
    public void SetAllTileVisibility(bool b)
    {
        if (isServer)
        {
            RpcUpdateSprites(b, tileDeck.Count, 0);
        }
    }

    [ClientRpc]
    public void RpcAddTileToDeck(GameObject obj)
    {
        if (!isServer)
        {
            tileDeck.Add(obj.GetComponent<Tile>());           
        }
        if (isClient)
        {
            if (tileDeck.Count > 135)
            {
                localPlayer.SetReady(true);
            }
        }
    }

    //Changes the tile sprite visiblity for all tiles between skip and skip + take for all players.
    [ClientRpc]
    public void RpcUpdateSprites(bool b, int take, int skip)
    {     
        foreach (Tile t in tileDeck.Skip(skip).Take(take))
        {
            SetTileVisibility(b, t);
        }
    }
    [ClientRpc]
    public void Rpc_UpdateSprite(bool b, int tilePos)
    {
        SetTileVisibility(b, tileDeck[tilePos]);
    }
    //Changes the tile sprite visiblity for all tiles between skip and skip + take for one player.
    [TargetRpc]
    public void TargetUpdateSprites(NetworkConnection connection, bool b, int take, int skip)
    {     
        foreach (Tile t in tileDeck.Skip(skip).Take(take))
        {
            SetTileVisibility(b, t);
        }
    }
    
    //Moves all the tiles between skip and skip + take for all players. x and y mark starting coordinates.
    [ClientRpc]
    public void Rpc_MoveTiles(float x, float y, int take, int skip)
    {
        int iterator = 0;
        float tempX = x;
        foreach (Tile t in tileDeck.Skip(skip).Take(take))
        {
            t.gameObject.transform.position = new Vector3(tempX, y);
            tempX += .5f;
            iterator++;
            if (iterator >= 17)
            {
                iterator = 0;
                tempX = x;
                y -= .8f;
            }          
        }
    }
    [ClientRpc]
    public void Rpc_MoveDora(float x, float y, int tilePos)
    {
        Tile dora = tileDeck[tilePos];
        dora.transform.position = new Vector3(x, y);
    }
    //Moves all the tiles between skip and skip + take for the wall width, to x and y coordinates.
    [ClientRpc]
    public void RpcMoveWallTiles(float x, float y, int take, int skip, int width)
    {
        int iterator = 0;
        float tempX = x;
        foreach (Tile t in tileDeck.Skip(skip).Take(take))
        {
            t.gameObject.transform.position = new Vector3(tempX, y);
            tempX += .5f;
            iterator++;
            if (iterator >= width)
            {
                iterator = 0;
                tempX = x;
                y -= .65f;
            }
        }
    }
    //Position values are a poorly named int that holds the tile's position in the deck for sorting.
    [ClientRpc]
    public void RpcSetTilePositionValues()
    {
        int iterator = 0;      
        foreach(Tile t in tileDeck)
        {
            t.SetPositionInDeck(iterator);
            iterator++;
        }       
        if (isClient)
        {
            tileDeck = SortTilesByPosition(tileDeck);
            if (iterator > 135)
            {
                localPlayer.SetReady(true);
            }
            else
            {
                localPlayer.SetReady(false);
            }
        }
    }
    [ClientRpc]
    public void Rpc_SyncTilePositionValues(int[] randomNumbersArray)
    {
        if (isClient)
        {
            int count = tileDeck.Count;
            while (count > 1)
            {
                count--;
                int i = randomNumbersArray[count];
                Tile t = tileDeck[i];
                tileDeck[i] = tileDeck[count];
                tileDeck[count] = t;
            }

            int iterator = 0;
            foreach (Tile t in tileDeck)
            {
                t.SetPositionInDeck(iterator);

                iterator++;
            }
            if (iterator > 135)
            {
                localPlayer.SetReady(true);
            }
            else
            {
                localPlayer.SetReady(false);
            }
        }
    }
    //Position values are a poorly named int that holds the tile's position in the deck for sorting.
    public void UpdateTilePositionValues()
    {
        int i = 0;
        foreach(Tile t in tileDeck)
        {
            t.SetPositionInDeck(i);
            i++;
        }
    }
    [ClientRpc]
    public void RpcUpdateTilePosition(int tilePos, float x, float y)
    {
        tileDeck[tilePos].transform.position = new Vector3(x, y);
    }

    [ClientRpc]
    public void Rpc_BeginDeckReset()
    {
        foreach(Tile t in tileDeck)
        {
            GameObject o = t.gameObject;
            Destroy(o);
        }
        tileDeck.Clear();
        resetHasFinished = true;
    }

    //Spreads out all the tiles to see. Not used in normal gameplay.
    private void SpreadTiles(List<Tile> tiles)
    {
        float xPos = tiles[0].transform.position.x;

        tiles = SortTiles(tiles);

        foreach (Tile t in tiles)
        {
            t.transform.position = new Vector3(xPos, tiles[0].transform.position.y);
            xPos += .5f;
        }
    }
    //Sorts the tiles by value.
    public List<Tile> SortTiles(List<Tile> tiles)
    {
        tiles.Sort((a, b) => a.GetComponent<Tile>().GetValue().CompareTo(b.GetComponent<Tile>().GetValue()));

        return tiles;
    }
    /*
    public void FlipDoraIndicator(int tilePos)
    {
        doraIndicator = tileDeck[tilePos];
        Rpc_UpdateSprite(true, tilePos);
        Rpc_MoveDora(7,2.54f, tilePos);
    }*/
    public void FlipReverseDoraIndicator()
    {
        if (doraIndicator != null)
        {
            if (doraIndicator.GetPositionInDeck() == 135)
            {
                reverseDoraIndicator = tileDeck[134];
            }
            else
            {
                reverseDoraIndicator = tileDeck[doraIndicator.GetPositionInDeck() + 1];
            }
            Rpc_UpdateSprite(true, reverseDoraIndicator.GetPositionInDeck());
            Rpc_MoveDora(doraIndicator.transform.position.x + 1, doraIndicator.transform.position.y, reverseDoraIndicator.GetPositionInDeck());
        }
    }
    //Sorts the tiles by their position in the deck.
    public List<Tile> SortTilesByPosition(List<Tile> tiles)
    {
        tiles.Sort((a, b) => a.GetComponent<Tile>().GetPositionInDeck().CompareTo(b.GetComponent<Tile>().GetPositionInDeck()));

        return tiles;
    }
    //Returns a portion of the deck between skip and skip + take.
    public List<Tile> GetTilesList(int skip, int take)
    {
        List<Tile> tiles = tileDeck.Skip(skip).Take(take).ToList();
        
        return tiles;
    }
    //Local player is the client's player object. Used to make sure all players are ready before continuing.
    public void SetLocalPlayer(Player p)
    {
        localPlayer = p;
    }
    public void SetResetHasFinished(bool b)
    {
        resetHasFinished = b;
    }
}
