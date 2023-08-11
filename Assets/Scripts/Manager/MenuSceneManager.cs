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
    [Header("Profile Menu")]
    [SerializeField] private TMP_InputField _nameTextField;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button signInButton;

    public TashiNetworkTransport NetworkTransport => NetworkManager.Singleton.NetworkConfig.NetworkTransport as TashiNetworkTransport;
    [Header("Lobby Menu")]
    [SerializeField] private TMP_InputField _numberPlayerInRoomTextField;
    [SerializeField] private TMP_InputField _roomCodeToJoinTextField;
    [SerializeField] private TMP_InputField _roomCodeLobbyTextField; /* room code of lobby you are in */


    [SerializeField] private GameObject _lobbyFreeGroup; /* Include buttons, components when are free, not in any lobby or room */
    [SerializeField] private GameObject _inLobbyGroup; /* Include buttons, components when are in lobby */
    [SerializeField] private Button _createLobbyButton;
    [SerializeField] private Button _joinLobbyButton;
    [SerializeField] private Button _exitLobbyButton;
    [SerializeField] private Button _startRoomButton;

    [Header("List Lobbies")]
    [SerializeField] private Button _reloadListLobbiesButton;
    [SerializeField] private Transform _listLobbiesContentTransform;
    [SerializeField] private LobbyItem _lobbyItemPrefab;
    public List<LobbyItem> listLobbies = new();

    public bool isLobbyHost = false;
    public string currentLobbyId = "";
    public string currentLobbyCode = "";
    public float nextHeartbeat;
    public float nextLobbyRefresh;
    private int _playerCount = 0;  /* Number player in lobby */

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

        CheckAuthentication();
    }
    void Update()
    {
        this.CheckLobbyUpdate();
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
    IEnumerator IEGetListLobbies(float delayTime = 3f){
        while(AuthenticationService.Instance.IsSignedIn){
            yield return new WaitForSeconds(delayTime);
            ListLobbies();
        }
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
    public async void JoinLobbyButtonClick()
    {
        var lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(this._roomCodeToJoinTextField.text);
        this.currentLobbyId = lobby.Id;
        this.currentLobbyCode = lobby.LobbyCode;
        Debug.Log($"Join lobby Id {this.currentLobbyId} has code {this.currentLobbyCode}");
        this.isLobbyHost = false;
        _roomCodeLobbyTextField.text = this.currentLobbyCode;
        UpdateStatusText();
    }
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
    public async void CheckLobbyUpdate()
    {
        /* If Free, not in any lobby */
        if (string.IsNullOrEmpty(currentLobbyId))
        {
            this._lobbyFreeGroup.SetActive(true);
            this._inLobbyGroup.SetActive(false);
            return;
        }
        /* If Are in lobby */
        this._lobbyFreeGroup.SetActive(false);
        this._inLobbyGroup.SetActive(true);
        if (isLobbyHost)
        {
            _startRoomButton.interactable = true;
        }
        else
        {
            _startRoomButton.interactable = false;
        }

        if (Time.realtimeSinceStartup >= nextHeartbeat && isLobbyHost)
        {
            nextHeartbeat = Time.realtimeSinceStartup + 15;
            await LobbyService.Instance.SendHeartbeatPingAsync(currentLobbyId);
        }

        if (Time.realtimeSinceStartup >= nextLobbyRefresh)
        {
            this.nextLobbyRefresh = Time.realtimeSinceStartup + 2;
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
                Filters = new List<QueryFilter>{
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>{
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);
            // Debug.Log("= Lobbies found : " + queryResponse.Results.Count);

            foreach (Transform child in _listLobbiesContentTransform)
            {
                Destroy(child.gameObject);
            }
            listLobbies.Clear();
            int i = 0;
            foreach (Lobby lobby in queryResponse.Results)
            {
                i++;
                // Debug.Log($"= Lobby {lobby.Name} has max {lobby.MaxPlayers} players");
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
    public async void LobbyUpdate()
    {
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
    public async void ReceiveIncomingDetail()
    {
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

}
