using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkMessenger : NetworkBehaviour {

    NetworkManager NetManager;
    TurnManager turnManager;
    public int connectedPlayers;

    private void Start()
    {
        NetManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
        turnManager = GameObject.Find("TurnManager").GetComponent<TurnManager>();
        if (isServer)
        {
            NetworkServer.RegisterHandler(MsgType.Highest + 2, OnMessageReceived);
        }
    }
    private void Update()
    {
        if (!isServer)
        {
            return;
        }
        if(NetManager.numPlayers != connectedPlayers)
        {           
            if(NetManager.numPlayers < 2)
            {
                connectedPlayers = NetManager.numPlayers;
            }
            if(NetManager.numPlayers > connectedPlayers)
            {
                Debug.Log("Player Connected.");
                //StartCoroutine(PlayerConnected(2));
                connectedPlayers = NetManager.numPlayers;
            }
        }
    }
    IEnumerator PlayerConnected(float time)
    {
        yield return new WaitForSeconds(time);
        turnManager.UpdatePlayersList();
        Debug.Log("Waited for " + time + " seconds and updated player list.");

    }
    void OnMessageReceived(NetworkMessage message)
    {
        if (!isServer)
        {
            return;
        }

        Debug.Log(message);
    }
}
