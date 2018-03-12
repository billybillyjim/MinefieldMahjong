#if ENABLE_UNET


using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

namespace UnityEngine.Networking

{
    [AddComponentMenu("Network/NetworkManagerHUD")]
    [RequireComponent(typeof(NetworkManager))]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class NetworkManagerHUD : MonoBehaviour
    {
        public NetworkManager manager;

        public int offsetX;
        public int offsetY;

        public Button LANHostButton;
        public Button LANClientButton;
        public Button ClientReadyButton;
        public Button StopButton;
        public Button EnableMatchMakerButton;
        public Button FindMatchesButton;
        public Button CreateMatchButton;
        public Text NetworkPortText;
        public Text ClientAddressText;

        public GameObject NetworkPanel;
        public GameObject MatchMakerPanel;
        public GameObject MatchesPanel;
        public GameObject JoinGameButtonObject;
        public GameObject MatchListPanel;
        public GameObject MatchInfoListPanel;
        public GameObject ConnectedNetworkPanel;

        public GameObject Title;

        public GameObject matchNameText;
        public GameObject playerCountText;
        private bool showInternetMatches;
        private bool noMatchesShown = false;

        private List<GameObject> buttons = new List<GameObject>();

        void Awake()
        {
            manager = GetComponent<NetworkManager>();

            LANHostButton.onClick.AddListener(StartHost);
            LANClientButton.onClick.AddListener(StartClient);
            //ClientReadyButton.onClick.AddListener(SetClientAsReady);
            StopButton.onClick.AddListener(StopConnection);
            EnableMatchMakerButton.onClick.AddListener(StartMatchMaker);
            FindMatchesButton.onClick.AddListener(FindInternetMatch);
            CreateMatchButton.onClick.AddListener(CreateInternetMatch);

            NetworkPanel.SetActive(true);
            MatchMakerPanel.SetActive(false);
            MatchesPanel.SetActive(false);
        }
        void Update()
        {
            if (showInternetMatches)
            {
                ShowInternetMatches();
            }
        }
        private void StartHost()
        {
            manager.StartHost();           
        }
        private void StartClient()
        {
            manager.StartClient();
        }
        private void SetClientAsReady()
        {
            ClientScene.Ready(manager.client.connection);

            if (ClientScene.localPlayers.Count == 0)
            {
                ClientScene.AddPlayer(0);
            }
        }
        private void StopConnection()
        {
            manager.StopHost();
        }
        private void StartMatchMaker()
        {
            manager.StartMatchMaker();
            NetworkPanel.SetActive(false);
            MatchMakerPanel.SetActive(true);

        }
        private void ShowInternetMatchData(Match.MatchInfoSnapshot match)
        {
            GameObject joinButtonObject = Instantiate(JoinGameButtonObject, new Vector3(-100, 100, 0), Quaternion.identity, MatchInfoListPanel.transform);
         
            joinButtonObject.GetComponent<Button>().onClick.AddListener(delegate { JoinInternetMatch(match); });
            matchNameText.GetComponent<Text>().text = match.name;
            playerCountText.GetComponent<Text>().text = "# of Players: " + match.currentSize;
        }
        private void FindInternetMatch()
        {
            manager.matchMaker.ListMatches(0, 20, "", true, 0, 0, manager.OnMatchList);
            MatchesPanel.SetActive(true);
            MatchMakerPanel.SetActive(false);
            showInternetMatches = true;
        }
        private void CreateInternetMatch()
        {
            manager.matchMaker.CreateMatch(manager.matchName, manager.matchSize, true, "", "", "", 0, 0, manager.OnMatchCreate);
        }
        private void JoinInternetMatch(Match.MatchInfoSnapshot match)
        {
            manager.matchName = match.name;
            manager.matchSize = (uint)match.currentSize;
            manager.matchMaker.JoinMatch(match.networkId, "", "", "", 0, 0, manager.OnMatchJoined);
            MatchMakerPanel.SetActive(false);
            MatchesPanel.SetActive(false);
            Title.SetActive(false);
            ConnectedNetworkPanel.SetActive(true);
        }
        public void ShowInternetMatches()
        {
            DestroyButtons();
            if (manager.matches != null && manager.matches.Count > 0)
            {
                MatchListPanel.transform.GetChild(0).gameObject.SetActive(false);
                int ypos = 40 + offsetY;
                matchNameText.SetActive(true);
                playerCountText.SetActive(true);
                foreach (var match in manager.matches)
                {
                    GameObject joinButtonObject = Instantiate(JoinGameButtonObject, new Vector3(0,100 + ypos,0), Quaternion.identity, MatchListPanel.transform);
                    joinButtonObject.GetComponent<Button>().onClick.AddListener(delegate { ShowInternetMatchData(match); });
                    joinButtonObject.GetComponentInChildren<Text>().text = match.name;
                    buttons.Add(joinButtonObject);
                }
                showInternetMatches = false;
                Debug.Log("There were " + manager.matches.Count + " matches.");
            }
            else if((manager.matches == null || manager.matches.Count == 0) && !noMatchesShown)
            {
                noMatchesShown = true;
                GameObject obj = new GameObject();
                obj.transform.SetParent(MatchListPanel.transform);
                Text txt = obj.AddComponent<Text>();
                obj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 381);
                txt.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
                txt.color = Color.black;
                txt.alignment = TextAnchor.MiddleCenter;

                txt.text = "There are currently no games being hosted.";
                showInternetMatches = false;
                matchNameText.SetActive(false);
                playerCountText.SetActive(false);

                Debug.Log("Matches was null");
            }
        }
        private void DestroyButtons()
        {
            for(int i = buttons.Count; i > 0; i--)
            {
                Destroy(buttons[i]);
            }
            buttons.Clear();
        }
        public void ExitInternetMatchListMenu()
        {
            GameObject panel = MatchListPanel;
            List<Transform> children = panel.GetComponentsInChildren<Transform>().ToList();
            children.Remove(panel.transform);
            foreach(Transform child in children)
            {
                GameObject.Destroy(child.gameObject);
            }
        }
    }
};

#endif //ENABLE_UNET