using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Tile : NetworkBehaviour {

    [SyncVar]
    private int tileValue;
    [SyncVar]
    private string tileName;
    [SerializeField]
    private Sprite storedSprite;
    [SerializeField]
    private Sprite blankSprite;
    [SerializeField]
    private Player owner;
    private Vector3 storedPosition;
    [SerializeField]
    private int positionInDeck;
    public GameObject toolTip;

    private Vector3 lastPos;

    private bool tileIsReset = false;

    public void SetTileInfo(int val, string name)
    {
        tileValue = val;
        tileName = name;
       
    }
    public int GetValue()
    {
        return tileValue;
    }

    public override void OnStartClient()
    {
        if (isClient)
        {
            gameObject.name = tileName + " " + tileValue;
            GetComponent<SpriteRenderer>().sprite = GameObject.Find("Deck").GetComponent<Deck>().GetSprite(this);
        }
        else
        {
            GetComponent<SpriteRenderer>().sprite = storedSprite;
        }
        storedSprite = GetComponent<SpriteRenderer>().sprite;       
    }

    public void SetTileVisibility(bool b)
    {
        if (b)
        {
            GetComponent<SpriteRenderer>().sprite = storedSprite;
        }
        else
        {
            GetComponent<SpriteRenderer>().sprite = blankSprite;
        }
    }
    public void OnMouseUp()
    {
        //Add tile to hand
        if (owner != null && owner.GetHand().Count < 13
            && owner.GetHand().Contains(this) == false
            && owner.GetIsPlayerTurn() == false
            && owner.GetIsSortingHand() == false
            && owner.GetGameHasStarted() == false)
        {
            storedPosition = transform.position;
            MoveTile(owner.GetPlayerHandPosition());
            owner.AddTileToHand(this);
            owner.SortHand();
        }
        //Remove tile from hand
        else if (owner != null
            && owner.GetHand().Contains(this) == true
            && owner.GetIsPlayerTurn() == false
            && owner.GetGameHasStarted() == false
            && owner.GetIsSortingHand() == false)
        {
            owner.RemoveTileFromHand(this);
            MoveTile(storedPosition);
            owner.SortHand();
        }
        //Discard tile
        else if (owner != null 
            && owner.GetHand().Contains(this) == false 
            && owner.GetIsPlayerTurn() == true 
            && owner.GetGameHasStarted() == true 
            && owner.GetIsSortingHand() == false)
        {         
            MoveTile(owner.GetPlayerDiscardPosition());
            owner.DiscardTile(this);
            Cmd_SetVisibility(true);    
                  
        }
    }
    private void OnMouseEnter()
    {
        if (toolTip != null)
        {
            toolTip.GetComponentInChildren<Text>().text = "" + tileName + " " + ((tileValue % 10) + 1);
            toolTip.SetActive(true);
            //toolTip.transform.position = transform.position + new Vector3(0, 40, 0);
        }
    }
    private void OnMouseOver()
    {
        if(toolTip != null)
        {
            toolTip.transform.position = Input.mousePosition + new Vector3(0, 30, 0);
        }
    }
    private void OnMouseExit()
    {
        if(toolTip != null)
        {
            toolTip.SetActive(false);
        }      
    }
    public void MoveTile(Vector3 newPos)
    {
        transform.position = newPos;
    }
    [Command]
    public void Cmd_MoveTile(Vector3 newPos)
    {
        Debug.Log("Cmd being moved.");
        //transform.position = newPos;
        Rpc_SyncPosition(newPos);
    }
    [ClientRpc]
    public void Rpc_SyncPosition(Vector3 newPos)
    {
        Debug.Log("Rpc being moved");
        transform.position = newPos;
    }
    [Command]
    public void Cmd_SetVisibility(bool b)
    {
        SetTileVisibility(b);
        Rpc_SyncVisibility(b);
    }
    [ClientRpc]
    public void Rpc_SyncVisibility(bool b)
    {
        SetTileVisibility(b);
    }
    public void ResetTile()
    {
        if (isServer)
        {
            Rpc_SyncTileReset();
        }     
    }
    [ClientRpc]
    public void Rpc_SyncTileReset()
    {
        if(owner != null)
        {
            owner.Cmd_RemoveLocalAuthority(this.gameObject);
        }           
        owner = null;
        storedPosition = new Vector3(0, 0);
        tileIsReset = true;
    }
    public void SetPlayerOwner(Player p)
    {
        owner = p;
        toolTip = owner.manager.ToolTip;
    }
    public Player GetPlayerOwner()
    {
        return owner;
    }
    public void SetPositionInWall(int i)
    {
        
    }
    public void SetPositionInDeck(int i)
    {
        positionInDeck = i;
    }
    public int GetPositionInDeck()
    {
        return positionInDeck;
    }
    public void SetPositionInHand(int i)
    {

    }
    public bool GetHasAuthority()
    {
        return hasAuthority;
    }
    public bool GetTileIsReset()
    {
        return tileIsReset;
    }
}
