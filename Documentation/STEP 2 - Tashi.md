# Simple Tashi Step by step : 
- This step 2 : When import Tashi and create lobby, list lobby, start room .

## Import Tashi newest version : 
- At this time : Tashi 0.3.2 is the newest. [Link](https://github.com/tashigg/tashi-network-transport/releases)
- Import Tashi by tar file.
- Go PackageManager and Import com.unity.multiplayer.tools Package.

- In this project, for more focus and simple, I'just write almost logic and func about Logic, Authen, Lobby in MenuSceneManager.

## Create Lobby and Get Lobby update in Tashi : 
- add `using Tashi.NetworkTransport` to MenuSceneManager.cs 
- Define NetworkTransport and create some func to CheckLobbyUpdate for update info lobby interval : 
```c# 
    public TashiNetworkTransport NetworkTransport => NetworkManager.Singleton.NetworkConfig.NetworkTransport as TashiNetworkTransport;
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

        var lobbyOptions = new CreateLobbyOptions
        {
            IsPrivate = false,
        };
        string lobbyName = this.LobbyName();

        var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayerInRoom, lobbyOptions);
        this.currentLobbyId = lobby.Id;
        this.currentLobbyCode = lobby.LobbyCode;
        this.isLobbyHost = true;
        _roomCodeLobbyTextField.text = this.currentLobbyCode;
        Debug.Log($"= Create Lobby name : {lobbyName} has max {maxPlayerInRoom} players. Lobby Code {this.currentLobbyCode}");
        UpdateStatusText();
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
        if(string.IsNullOrEmpty(currentLobbyId)) return;

        if(Time.realtimeSinceStartup >= nextHeartbeat && isLobbyHost){
            nextHeartbeat = Time.realtimeSinceStartup + 15;
            await LobbyService.Instance.SendHeartbeatPingAsync(currentLobbyId);
        }

        if(Time.realtimeSinceStartup >= nextLobbyRefresh){
            this.nextLobbyRefresh = Time.realtimeSinceStartup + 2;
            this.LobbyUpdate();
            this.ReceiveIncomingDetail();
        }
    }
    public async void LobbyUpdate(){
        var outgoingSessionDetails = NetworkTransport.OutgoingSessionDetails;

        var updatePlayerOptions = new UpdatePlayerOptions();
        if (outgoingSessionDetails.AddTo(updatePlayerOptions))
        {
            await LobbyService.Instance.UpdatePlayerAsync(currentLobbyId, AuthenticationService.Instance.PlayerId, updatePlayerOptions);
        }

        if (isLobbyHost)
        {
            var updateLobbyOptions = new UpdateLobbyOptions();
            if (outgoingSessionDetails.AddTo(updateLobbyOptions))
            {
                await LobbyService.Instance.UpdateLobbyAsync(currentLobbyId, updateLobbyOptions);
            }
        }
    }
    public async void ReceiveIncomingDetail(){
        if (NetworkTransport.SessionHasStarted) return;
        
        Debug.LogWarning("Receive Incoming Detail");

        var lobby = await LobbyService.Instance.GetLobbyAsync(currentLobbyId);
        var incomingSessionDetails = IncomingSessionDetails.FromUnityLobby(lobby);
        this._playerCount = lobby.Players.Count;
        UpdateStatusText();

        // This should be replaced with whatever logic you use to determine when a lobby is locked in.
        if (this._playerCount > 1 && incomingSessionDetails.AddressBook.Count == lobby.Players.Count)
        {
            Debug.LogWarning("Update Session Details");
            NetworkTransport.UpdateSessionDetails(incomingSessionDetails);
        }
    }

```

## Get List lobbies existing and Join Lobby : 
- First of all, create UI List lobby include : List Lobby, Lobby Item, UI In lobby, UI Free Not in Lobby.
- In MenuSceneManager.cs,  : 
```c# 
    using Unity.Services.Lobbies.Models;
    [SerializeField] private GameObject _lobbyFreeGroup; /* Include buttons, components when are free, not in any lobby or room */
    [SerializeField] private GameObject _inLobbyGroup; /* Include buttons, components when are in lobby */
        [SerializeField] private Button _exitLobbyButton;
    [SerializeField] private Button _startRoomButton;
        [Header("List Lobbies")]
    [SerializeField] private Button _reloadListLobbiesButton;
    [SerializeField] private Transform _listLobbiesContentTransform;
    [SerializeField] private LobbyItem _lobbyItemPrefab;
    public List<LobbyItem> listLobbies = new();

    public async void JoinLobbyByRoomCode(string roomCode)
    {
        var lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(roomCode);
        this.currentLobbyId = lobby.Id;
        this.currentLobbyCode = lobby.LobbyCode;
        Debug.Log($"Join lobby Id {this.currentLobbyId} has code {this.currentLobbyCode}");
        this.isLobbyHost = false;
        _roomCodeLobbyTextField.text = this.currentLobbyCode;
        UpdateStatusText();
    }
    public async void JoinLobbyByLobbyId(string lobbyId)
    {
        var lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
        this.currentLobbyId = lobby.Id;
        this.currentLobbyCode = lobby.LobbyCode;
        Debug.Log($"Join lobby Id {this.currentLobbyId} has code {this.currentLobbyCode}");
        this.isLobbyHost = false;
        _roomCodeLobbyTextField.text = this.currentLobbyCode;
        UpdateStatusText();
    }
        public async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>{
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>{
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);
            Debug.Log("= Lobbies found : " + queryResponse.Results.Count);

            foreach (Transform child in _listLobbiesContentTransform)
            {
                Destroy(child.gameObject);
            }
            listLobbies.Clear();
            int i = 0;
            foreach (Lobby lobby in queryResponse.Results)
            {
                i++;
                Debug.Log($"= Lobby {lobby.Name} has max {lobby.MaxPlayers} players");
                var lobbyItem = Instantiate(_lobbyItemPrefab, _listLobbiesContentTransform);
                lobbyItem.SetData("#" + i, lobby.Id, lobby.LobbyCode, lobby.Name);
                lobbyItem.SetOnClickJoin(OnClickJoinLobby);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Exception : " + e.ToString());
        }
    }
    public void OnClickJoinLobby(string lobbyId)
    {
        if (string.IsNullOrEmpty(this.currentLobbyId))
            JoinLobbyByLobbyId(lobbyId);
    }
```
- Dont forget add onClick.AddListener for all your button with suitable func.
- When you already sign in, so call get list lobbies and after a time call func to get list lobbies look like realtime : 
```c# 
    IEnumerator IEGetListLobbies(float delayTime = 3f){
        while(AuthenticationService.Instance.IsSignedIn){
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

- Notice : PlayerDataObject in Lobby, when you LobbyService.Instance.UpdatePlayerAsync(currentLobbyId, playerId, options) , it's just update which param you put in the options, event if you create new options, old data still exist if you dont remove or change it. 
