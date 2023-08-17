# Simple Tashi Step by step : 
- This step 2 : When import Tashi and create lobby, list lobby, start room .

## Import Tashi newest version : 
- At this time : Tashi 0.3.2 is the newest. [Link](https://github.com/tashigg/tashi-network-transport/releases)
- Import Tashi by tar file.
- Go PackageManager and Import com.unity.multiplayer.tools Package.

- In this project, for more focus and simple, I'just write almost logic and func about Logic, Authen, Lobby in MenuSceneManager.

## Create Lobby and Get Lobby update in Tashi : 
**Overview** : Once the parameters (name, maxPlayerInRoom) have been supplied, we will construct the lobby (and start the server) and update the status once the lobby has been successfully created.
Do a hearbeat ping in Update() to keep the lobby active.
Update PlayerDataObject and LobbyData in Tashi by running Update OutgoingSessionDetails and Update IncomingSessionDetails.
This page will be updated with new information about Lobby.

- First Add `using Tashi.NetworkTransport` to MenuSceneManager.cs 
- Define NetworkTransport and create some func to CheckLobbyUpdate for update info lobby interval : 
```c# 
    public TashiNetworkTransport NetworkTransport => NetworkManager.Singleton.NetworkConfig.NetworkTransport as TashiNetworkTransport;
    public Lobby lobby;
    public bool isLobbyHost = false;
    public string currentLobbyId = "";
    public string currentLobbyCode = "";
    public float nextHeartbeat;
    public float nextLobbyRefresh;
    /* If Tashi has already been set as a PlayerDataObject, we can set our own PlayerDataPbject in the Lobby. */
    public bool isSetInitPlayerDataObject = true;
    ...
    public async void CreateLobby()
    {
        int maxPlayerInRoom = 8;
        if (int.TryParse(_numberPlayerInRoomTextField.text, out int rs))
        {
            maxPlayerInRoom = rs;
        }
        else
        {
            maxPlayerInRoom = 8;
        }

        _numberPlayerInRoomTextField.text = maxPlayerInRoom.ToString();
        
        NetworkManager.Singleton.StartServer();

        var lobbyOptions = new CreateLobbyOptions
        {
            IsPrivate = false,
        };
        string lobbyName = this.LobbyName();

        lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayerInRoom, lobbyOptions);
        this.currentLobbyId = lobby.Id;
        this.currentLobbyCode = lobby.LobbyCode;
        this.isLobbyHost = true;
        _roomCodeLobbyTextField.text = this.currentLobbyCode;
        Debug.Log(
            $"= Create Lobby name : {lobbyName} has max {maxPlayerInRoom} players. Lobby Code {this.currentLobbyCode}");
        UpdateStatusText();
        isSetInitPlayerDataObject = false;
    }
    void UpdateStatusText()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            statusText.text = $"Signed in as {AuthenticationService.Instance.Profile} (ID:{AuthenticationService.Instance.PlayerId}) in Lobby";
            // Shows how to get an access token
            statusText.text += $"\n{_clientCount} peer connections";
        }
        else
        {
            statusText.text = "Not Sign in yet";
        }
        if (string.IsNullOrEmpty(currentLobbyId) || string.IsNullOrEmpty(currentLobbyCode))
        {
        }
        else
        {
            statusText.text += $"\n In Lobby ID : {currentLobbyId} has code : {currentLobbyCode}";
            statusText.text += $"\n {_playerCount} players in lobby.";
        }
    }
    void Update(){
        this.CheckLobbyUpdate();
    }
    public async void CheckLobbyUpdate(){
        /* If Free, not in any lobby, show suiable UI */
        if (string.IsNullOrEmpty(currentLobbyId))
        {
            this._lobbyFreeGroup.SetActive(true);
            this._inLobbyGroup.SetActive(false);
            return;
        }

        /* If Are in lobby, just show suiable UI */
        this._lobbyFreeGroup.SetActive(false);
        this._inLobbyGroup.SetActive(true);

        if(Time.realtimeSinceStartup >= nextHeartbeat && isLobbyHost){
            nextHeartbeat = Time.realtimeSinceStartup + 15;
            /* Keep connection to lobby alive */
            await LobbyService.Instance.SendHeartbeatPingAsync(currentLobbyId);
        }

        if(Time.realtimeSinceStartup >= nextLobbyRefresh){
            this.nextLobbyRefresh = Time.realtimeSinceStartup + 2; /* Update after every 2 seconds */
            this.LobbyUpdate();
            this.ReceiveIncomingDetail();
        }
    }
    /* Tashi setup/update PlayerDataObject */
    public async void LobbyUpdate()
    {
        var outgoingSessionDetails = NetworkTransport.OutgoingSessionDetails;

        var updatePlayerOptions = new UpdatePlayerOptions();
        if (outgoingSessionDetails.AddTo(updatePlayerOptions))
        {
            // Debug.Log("= PlayerData outgoingSessionDetails AddTo TRUE so can UpdatePLayerAsync");
            lobby = await LobbyService.Instance.UpdatePlayerAsync(currentLobbyId, AuthenticationService.Instance.PlayerId,
                updatePlayerOptions);
            
            if (isSetInitPlayerDataObject == false)
            {
                isSetInitPlayerDataObject = true;
                UpdatePlayerDataInCurrentLobby(lobby, AuthenticationService.Instance.Profile,
                    isLobbyHost ? PlayerTypeInGame.Police.ToString() : PlayerTypeInGame.Thief.ToString(), false);
            }
        }
        if (isLobbyHost)
        {
            var updateLobbyOptions = new UpdateLobbyOptions();
            if (outgoingSessionDetails.AddTo(updateLobbyOptions))
            {
                // Debug.Log("= Lobby outgoingSessionDetails AddTo TRUE and Update Lobby Async.");
                lobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobbyId, updateLobbyOptions);
            }
        }
    }
    /* Tashi Update/get lobby session details */
    public async void ReceiveIncomingDetail()
    {
        if (NetworkTransport.SessionHasStarted) return;

        Debug.LogWarning("Receive Incoming Detail");

        lobby = await LobbyService.Instance.GetLobbyAsync(currentLobbyId);
        var incomingSessionDetails = IncomingSessionDetails.FromUnityLobby(lobby);
        this._playerCount = lobby.Players.Count;
        UpdateStatusText();


        // This should be replaced with whatever logic you use to determine when a lobby is locked in.
        if (this._playerCount > 1 && incomingSessionDetails.AddressBook.Count == lobby.Players.Count)
        {
            Debug.LogWarning("Update Session Details");
            NetworkTransport.UpdateSessionDetails(incomingSessionDetails);
        }

        /* Refresh List Player In Room
         Update player state/info in room in List Player
         */
        foreach (Transform child in _listPlayersContentTransform)
        {
            child.gameObject.SetActive(false);
        }
        listPlayers.Clear();
        for (int i = 0; i <= lobby.Players.Count - 1; i++)
        {
            Player pData = lobby.Players[i];
            Debug.Log("====== PLAYER DATA OBJECT INCOMMING DETAIL  ===== INDEX : " + i);
            
            if (pData.Data is null) continue;
            
            foreach (KeyValuePair<string, PlayerDataObject> k in pData.Data)
                Debug.Log($"= Key : {k.Key.ToString()} and Value = {k.Value.Value.ToString()}");

            PlayerItem playerItem;
            try
            {
                playerItem = _listPlayersContentTransform.GetChild(i).GetComponent<PlayerItem>();
            }
            catch (Exception)
            {
                playerItem = Instantiate(_playerItemPrefab, _listPlayersContentTransform);    
            }

            string name = !pData.Data.ContainsKey("Name") ? "" : pData.Data["Name"].Value;
            string role = !pData.Data.ContainsKey("Role") ? "" : pData.Data["Role"].Value;
            bool isReady = !pData.Data.ContainsKey("IsReady") ? false : Convert.ToBoolean(pData.Data["IsReady"].Value);

            playerItem.SetData("#" + (i + 1), name, role, isReady);
            listPlayers.Add(playerItem);
        }
    }

```

## Get List lobbies existing and Join Lobby : 
- First of all, create UI List lobby include : List Lobby, Lobby Item, UI In lobby, UI Free Not in Lobby.
- In MenuSceneManager.cs,  : 
```c# 
    using Unity.Services.Lobbies.Models;
    
    [SerializeField] private GameObject profileMenu;
    [SerializeField] private GameObject lobbyMenu;
    [SerializeField] private string SceneGamePlayName = "PlayScene";

    [Header("Profile Menu")] [SerializeField]
    private TMP_InputField _nameTextField;

    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button signInButton;

    public TashiNetworkTransport NetworkTransport =>
        NetworkManager.Singleton.NetworkConfig.NetworkTransport as TashiNetworkTransport;

    [Header("Lobby Menu")] [SerializeField]
    private TMP_InputField _numberPlayerInRoomTextField;

    [SerializeField] private TMP_InputField _roomCodeToJoinTextField;
    [SerializeField] private TMP_InputField _roomCodeLobbyTextField; /* room code of lobby you are in */


    [SerializeField]
    private GameObject _lobbyFreeGroup; /* Include buttons, components when are free, not in any lobby or room */

    [SerializeField] private GameObject _inLobbyGroup; /* Include buttons, components when are in lobby */
    [SerializeField] private Button _createLobbyButton;
    [SerializeField] private Button _joinLobbyButton;
    [SerializeField] private Button _exitLobbyButton;
    [SerializeField] private Button _startRoomButton;
    [SerializeField] private Button _readyRoomButton;

    [Header("List PLayers in Room")] [SerializeField]
    private Transform _listPlayersContentTransform;

    [SerializeField] private PlayerItem _playerItemPrefab;
    public List<PlayerItem> listPlayers = new();

    [Header("List Lobbies")] [SerializeField]
    private Button _reloadListLobbiesButton;

    [SerializeField] private Transform _listLobbiesContentTransform;
    [SerializeField] private LobbyItem _lobbyItemPrefab;
    public List<LobbyItem> listLobbies = new();

    public async void JoinLobbyByRoomCode(string roomCode)
    {
        NetworkManager.Singleton.StartClient();
        lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(roomCode);
        
        this.currentLobbyId = lobby.Id;
        this.currentLobbyCode = lobby.LobbyCode;
        Debug.Log($"Join lobby Id {this.currentLobbyId} has code {this.currentLobbyCode}");
        this.isLobbyHost = false;
        _roomCodeLobbyTextField.text = this.currentLobbyCode;
        UpdateStatusText();
        isSetInitPlayerDataObject = false;
    }

    public async void JoinLobbyByLobbyId(string lobbyId)
    {
        NetworkManager.Singleton.StartClient();
        lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
        
        this.currentLobbyId = lobby.Id;
        this.currentLobbyCode = lobby.LobbyCode;
        Debug.Log($"Join lobby Id {this.currentLobbyId} has code {this.currentLobbyCode}");
        this.isLobbyHost = false;
        _roomCodeLobbyTextField.text = this.currentLobbyCode;
        UpdateStatusText();
        isSetInitPlayerDataObject = false;
        
        UpdatePlayerDataInCurrentLobby(lobby, AuthenticationService.Instance.Profile,
            isLobbyHost ? PlayerTypeInGame.Police.ToString() : PlayerTypeInGame.Thief.ToString(), false);
    }
    public async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    /* Just get the lobby's available slots using the filter. */
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            /* Disative all old lobby item in list */
            foreach (Transform child in _listLobbiesContentTransform)
            {
                child.gameObject.SetActive(false);
            }
            listLobbies.Clear();
            /* Show every lobby item in list */
            int i = 0;
            foreach (Lobby lobby in queryResponse.Results)
            {
                LobbyItem lobbyItem;
                try
                {
                    lobbyItem = _listLobbiesContentTransform.GetChild(i).GetComponent<LobbyItem>();
                }
                catch (Exception)
                {
                    lobbyItem = Instantiate(_lobbyItemPrefab, _listLobbiesContentTransform);
                }
                lobbyItem.SetData("#" + (i + 1), lobby.Id, lobby.LobbyCode, lobby.Name);
                lobbyItem.SetOnClickJoin(OnClickJoinLobby);
                listLobbies.Add(lobbyItem);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Exception : " + e.ToString());
        }
    }
```
- Dont forget add onClick.AddListener for all your button with suitable func.
- When you already sign in and not in any lobby, so call get list lobbies and after a time call func to get list lobbies look like realtime : 
```c# 
    IEnumerator IEGetListLobbies(float delayTime = 3f){
        while(AuthenticationService.Instance.IsSignedIn && string.IsNullOrEmpty(currentLobbyId)){
            yield return new WaitForSeconds(delayTime);
            ListLobbies();
        }
    }
```

## Create List Player In Room : Name, State 
- Define some variable to control list player data in MenuSceneManager: 
```c# 
    [Header("List PLayers in Room")]
    [SerializeField] private Transform _listPlayersContentTransform;
    [SerializeField] private PlayerItem _playerItemPrefab; /* NOtice : PlayerItem */
    public List<PlayerItem> listPlayers = new();
```
- It's the same with List Lobby, so I create the PlayerItem.cs for each item Player in Player List : 
```c#
    public class PlayerItem : MonoBehaviour {
    [SerializeField] public TextMeshProUGUI sttText;
    [SerializeField] public TextMeshProUGUI nameText;
    [SerializeField] public TextMeshProUGUI roleText;
    [SerializeField] public Image readyImage;
    [SerializeField] public Sprite isReadyImage;
    [SerializeField] public Sprite isNotReadyImage;

    void Start(){
        
    }
    public void SetData(string stt,string name, PlayerTypeInGame role, bool isReady = false){
        sttText.text = stt;
        nameText.text = name;
        roleText.text = role.ToString();
        SetReadyImage(isReady);
        this.gameObject.SetActive(true);
    }
    public void SetReadyImage(bool isReady = false){
        if(isReady){
            readyImage.sprite = isReadyImage;
        }else{
            readyImage.sprite = isNotReadyImage;
        }
    }
}
```

- **Notice** : _PlayerDataObject in Lobby, when you LobbyService.Instance.UpdatePlayerAsync(currentLobbyId, playerId, options) , it's just update which param you put in the options, event if you create new options, old data still exist if you dont remove or change it._

- Exit lobby : apply for both clients and host. If you are host and leave lobby, one of client left will become the host. 
```c# 
    public async void ExitCurrentLobby()
    {
        /* Remove this player out of this lobby */
        if (lobby.Players.Count > 1)
        {
            await LobbyService.Instance.RemovePlayerAsync(currentLobbyId, AuthenticationService.Instance.PlayerId);
        }
        else
        {
            await LobbyService.Instance.DeleteLobbyAsync(currentLobbyId);
        }

        currentLobbyCode = null;
        currentLobbyId = null;
        isLobbyHost = false;
        UpdateStatusText();
        ListLobbies();
        StartCoroutine(IEGetListLobbies());
    } 
    public void OnApplicationQuit()
    {
        ExitCurrentLobby();
    }
```

