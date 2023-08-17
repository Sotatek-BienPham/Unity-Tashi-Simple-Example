using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Random = UnityEngine.Random;
using Tashi.NetworkTransport;

public class MenuSceneManager : Singleton<MenuSceneManager>
{
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

    public Lobby lobby;
    public bool isLobbyHost = false;
    public string currentLobbyId = "";
    public string currentLobbyCode = "";
    public float nextHeartbeat;
    public float nextLobbyRefresh;
    /* If Tashi has already been set as a PlayerDataObject, we can set our own PlayerDataPbject in the Lobby. */
    public bool isSetInitPlayerDataObject = true; 
    private int _playerCount = 0; /* Number player in lobby */

    private int _clientCount = 0; /* total clients are connect */

    protected override void Awake()
    {
        base.Awake();
        UnityServicesInit();
    }

    private async void UnityServicesInit()
    {
        await UnityServices.InitializeAsync();
    }

    void Start()
    {
        string name = PlayerPrefs.GetString(Constants.NAME_PREF, "");
        PlayerDataManager.Instance.SetName(name);
        _nameTextField.text = name;
        /* Listen player name text field value changed */
        _nameTextField.onValueChanged.AddListener(delegate { OnPlayerNameChange(); });

        _createLobbyButton.onClick.AddListener(CreateLobby);
        _joinLobbyButton.onClick.AddListener(JoinLobbyButtonClick);

        _reloadListLobbiesButton.onClick.AddListener(ListLobbies);

        _startRoomButton.onClick.AddListener(StartHost);
        _readyRoomButton.onClick.AddListener(ToggleReadyState);
        _exitLobbyButton.onClick.AddListener(ExitCurrentLobby);

        CheckAuthentication();
    }

    public async void ToggleReadyState()
    {
        Lobby lobby = await LobbyService.Instance.GetLobbyAsync(currentLobbyId);
        Player p = lobby.Players.Find(x => x.Id == AuthenticationService.Instance.PlayerId);
        if (p is null) return;
        string _isReadyStr = p.Data["IsReady"].Value;
        bool _isReady = bool.Parse(_isReadyStr);
        UpdatePlayerDataIsReadyInLobby(!_isReady);
    }

    void Update()
    {
        CheckLobbyUpdate();
    }

    void CheckAuthentication()
    {
        /* Check signed in */
        if (AuthenticationService.Instance.IsSignedIn)
        {
            profileMenu.SetActive(false);
            lobbyMenu.SetActive(true);
        }
        else
        {
            profileMenu.SetActive(true);
            lobbyMenu.SetActive(false);
        }

        UpdateStatusText();
    }

    public void OnPlayerNameChange()
    {
        Debug.Log("OnPlayerNameChange : " + _nameTextField.text);
        PlayerDataManager.Instance.SetName(_nameTextField.text);
    }

    public async void SignInButtonClicked()
    {
        if (string.IsNullOrEmpty(_nameTextField.text))
        {
            Debug.Log($"Signing in with the default profile");
            // await UnityServices.InitializeAsync();
        }
        else
        {
            Debug.Log($"Signing in with profile '{_nameTextField.text}'");
            /* Init Unity Services. But now no need cause inited in Awake() */
            // var options = new InitializationOptions();
            // options.SetProfile(_nameTextField.text);
            // await UnityServices.InitializeAsync(options);

            /* Switch to new Profile name. Profile init in awake() is default */
            AuthenticationService.Instance.SwitchProfile(_nameTextField.text);
        }

        try
        {
            signInButton.interactable = false;
            statusText.text = $"Signing in .... ";
            AuthenticationService.Instance.SignedIn += delegate
            {
                Debug.Log("SignedIn OK!");
                signInButton.interactable = true;
                PlayerDataManager.Instance.SetId(AuthenticationService.Instance.PlayerId);
                UpdateStatusText();
                profileMenu.SetActive(false);
                lobbyMenu.SetActive(true);

                ListLobbies();
                StartCoroutine(IEGetListLobbies());
            };

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (Exception e)
        {
            signInButton.interactable = true;
            statusText.text = $"Sign in failed : {e.ToString()} ";
            Debug.LogException(e);
            throw;
        }
    }

    IEnumerator IEGetListLobbies(float delayTime = 3f)
    {
        while (AuthenticationService.Instance.IsSignedIn && string.IsNullOrEmpty(currentLobbyId))
        {
            yield return new WaitForSeconds(delayTime);
            ListLobbies();
        }
    }

    void UpdateStatusText()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            statusText.text =
                $"Signed in as {AuthenticationService.Instance.Profile} (ID:{AuthenticationService.Instance.PlayerId}) in Lobby";
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

    public async void JoinLobbyButtonClick()
    {
        lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(this._roomCodeToJoinTextField.text);
        
        NetworkManager.Singleton.StartClient();
        this.currentLobbyId = lobby.Id;
        this.currentLobbyCode = lobby.LobbyCode;
        Debug.Log($"Join lobby Id {this.currentLobbyId} has code {this.currentLobbyCode}");
        this.isLobbyHost = false;
        _roomCodeLobbyTextField.text = this.currentLobbyCode;
        UpdateStatusText();
        isSetInitPlayerDataObject = false;
    }

    public async void UpdatePlayerDataIsReadyInLobby(bool isReady)
    {
        try
        {
            UpdatePlayerOptions options = new UpdatePlayerOptions();

            options.Data = new Dictionary<string, PlayerDataObject>()
            {
                {
                    "IsReady", new PlayerDataObject(
                        visibility: PlayerDataObject.VisibilityOptions.Public,
                        value: isReady.ToString())
                }
            };

            //Ensure you sign-in before calling Authentication Instance
            //See IAuthenticationService interface
            string playerId = AuthenticationService.Instance.PlayerId;

            lobby = await LobbyService.Instance.UpdatePlayerAsync(currentLobbyId, playerId, options);
            Debug.Log("= UpdatePlayerDataIsReadyInLobby : isReady " + isReady.ToString());
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    /* To update some Player Info through lobby such as name, isReady state, role */
    public async void UpdatePlayerDataInCurrentLobby(Lobby lobby, string name, string role, bool isReady)
    {
        /* Add Player Data into Lobby */
        try
        {
            //Ensure you sign-in before calling Authentication Instance
            //See IAuthenticationService interface
            string playerId = AuthenticationService.Instance.PlayerId;

            /* Find PlayerData for current this Player  */
            Player p = lobby.Players.Find(x => x.Id == playerId);
            if (p is null) return;
            Debug.Log("= UpdatePlayerDataInCurrentLobby : ID : " + p.Id);
            Dictionary<string, PlayerDataObject> oldData = p.Data;
            if (oldData is null)
            {
                oldData = new Dictionary<string, PlayerDataObject>();
            }

            UpdatePlayerOptions options = new UpdatePlayerOptions();
            // options.Data = oldData; 
            options.Data = new Dictionary<string, PlayerDataObject>();

            options.Data["Name"] = new PlayerDataObject(
                visibility: PlayerDataObject.VisibilityOptions.Public,
                value: name);
            options.Data["Role"] = new PlayerDataObject(
                visibility: PlayerDataObject.VisibilityOptions.Public,
                value: role);
            options.Data["IsReady"] = new PlayerDataObject(
                visibility: PlayerDataObject.VisibilityOptions.Public,
                value: isReady.ToString());

            lobby = await LobbyService.Instance.UpdatePlayerAsync(currentLobbyId, playerId, options);
            
            
            Debug.Log("====== PLAYER DATA OBJECT AFTER =====");
            foreach (KeyValuePair<string, PlayerDataObject> k in lobby.Players.Find(x=>x.Id == playerId).Data)
                Debug.Log($"= Key : {k.Key.ToString()} and Value = {k.Value.Value.ToString()}");

            //...
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

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

    public async void CheckLobbyUpdate()
    {
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

        _startRoomButton.gameObject.SetActive(isLobbyHost);
        _readyRoomButton.gameObject.SetActive(!isLobbyHost);

        if (Time.realtimeSinceStartup >= nextHeartbeat && isLobbyHost)
        {
            nextHeartbeat = Time.realtimeSinceStartup + 15;
            /* Keep connection to lobby alive */
            await LobbyService.Instance.SendHeartbeatPingAsync(currentLobbyId);
        }

        if (Time.realtimeSinceStartup >= nextLobbyRefresh)
        {
            this.nextLobbyRefresh = Time.realtimeSinceStartup + 2; /* Update after every 2 seconds */
            this.LobbyUpdate();
            this.ReceiveIncomingDetail();
        }
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

    public void OnClickJoinLobby(string lobbyId)
    {
        if (string.IsNullOrEmpty(this.currentLobbyId))
            JoinLobbyByLobbyId(lobbyId);
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

    public string LobbyName()
    {
        return AuthenticationService.Instance.Profile + "_lobby_" + Random.Range(1, 100);
    }

    public void StartHost()
    {
        try
        {
            PlayerDataManager.Instance.SetName(_nameTextField.text);
        }
        catch (Exception e)
        {
            Debug.Log(" Excep : " + e);
        }

        AsyncOperation progress = SceneManager.LoadSceneAsync(SceneGamePlayName, LoadSceneMode.Single);

        progress.completed += (op) =>
        {
            PlayerDataManager.Instance.SetStatus(PlayerStatus.InRoom);
            NetworkManager.Singleton.StartHost();
        };
    }

    public void StartClient()
    {
        PlayerDataManager.Instance.SetName(_nameTextField.text);
        AsyncOperation progress = SceneManager.LoadSceneAsync(SceneGamePlayName, LoadSceneMode.Single);
        progress.completed += (op) =>
        {
            Debug.Log($"Scene {SceneGamePlayName} loaded.");
            PlayerDataManager.Instance.SetStatus(PlayerStatus.InRoom);
            NetworkManager.Singleton.StartClient();
            Debug.Log("Started Client");
        };
    }

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
}