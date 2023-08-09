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
using Random = UnityEngine.Random;

public class MenuSceneManager : Singleton<MenuSceneManager>
{

    [SerializeField] private GameObject profileMenu;
    [SerializeField] private GameObject lobbyMenu;
    [SerializeField] private string SceneGamePlayName = "PlayScene";
    [Header("Profile Menu")]
    [SerializeField] private TMP_InputField _nameTextField;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button signInButton;
    [Header("Lobby Menu")]
    // public TashiNetworkTransport NetworkTransport => NetworkManager.Singleton.NetworkConfig.NetworkTransport as TashiNetworkTransport;
    [SerializeField] private TMP_InputField _numberPlayerInRoomTextField;
    [SerializeField] private TMP_InputField _roomCodeToJoinTextField;
    [SerializeField] private Button _createLobbyButton;
    [SerializeField] private Button _joinLobbyButton;
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
    private async void UnityServicesInit(){
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
        _joinLobbyButton.onClick.AddListener(JoinLobby);
        CheckAuthentication();
    }
    void Update(){
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
    void UpdateStatusText()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            statusText.text = $"Signed in as {AuthenticationService.Instance.Profile} (ID:{AuthenticationService.Instance.PlayerId}) in Lobby";
            // Shows how to get an access token
            // Debug.Log($"Access Token: {AuthenticationService.Instance.AccessToken}");

            statusText.text += $"\n{_clientCount} peer connections";
        }
        else
        {
            statusText.text = "Not Sign in yet";
        }
        if(string.IsNullOrEmpty(currentLobbyId) || string.IsNullOrEmpty(currentLobbyCode)){

        }else{
            statusText.text += $"\n In Lobby ID : {currentLobbyId} has code : {currentLobbyCode}";
            statusText.text += $"\n {_playerCount} players in lobby.";
        }
    }

    public async void CreateLobby(){
        int maxPlayerInRoom = 8;
        if(int.TryParse(_numberPlayerInRoomTextField.text, out int rs)){
            maxPlayerInRoom = rs;
        }else{
            maxPlayerInRoom = 8;
        }
        _numberPlayerInRoomTextField.text = maxPlayerInRoom.ToString();

        var lobbyOptions = new CreateLobbyOptions{
            IsPrivate = false,
        };
        string lobbyName = this.LobbyName();
        Debug.Log($"= Create Lobby name : {lobbyName} has max {maxPlayerInRoom} players.");

        var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayerInRoom, lobbyOptions);
        this.currentLobbyId = lobby.Id;
        this.currentLobbyCode = lobby.LobbyCode;
        this.isLobbyHost = true;
        UpdateStatusText();
    }
    public void JoinLobby(){
        string roomCode = _roomCodeToJoinTextField.text;

    }
    public async void CheckLobbyUpdate(){
        // if(string.IsNullOrEmpty(currentLobbyId)) return;

        // if(Time.realtimeSinceStartup >= nextHeartbeat && isLobbyHost){
        //     nextHeartbeat = Time.realtimeSinceStartup + 15;
        //     await LobbyServices.Instance.SendHeartbeatPingAsync(currentLobbyId);
        // }

        // if(Time.realtimeSinceStartup >= nextLobbyRefresh){
        //     this.nextLobbyRefresh = Time.realtimeSinceStartup + 2;
        //     this.LobbyUpdate();
        //     this.ReceiveIncomingDetail();
        // }
    }
    public async void LobbyUpdate(){
        // var outgoingSessionDetails = NetworkTransport.OutgoingSessionDetails;

        // var updatePlayerOptions = new UpdatePlayerOptions();
        // if (outgoingSessionDetails.AddTo(updatePlayerOptions))
        // {
        //     await LobbyService.Instance.UpdatePlayerAsync(currentLobbyId, AuthenticationService.Instance.PlayerId, updatePlayerOptions);
        // }

        // if (isLobbyHost)
        // {
        //     var updateLobbyOptions = new UpdateLobbyOptions();
        //     if (outgoingSessionDetails.AddTo(updateLobbyOptions))
        //     {
        //         await LobbyService.Instance.UpdateLobbyAsync(currentLobbyId, updateLobbyOptions);
        //     }
        // }
    }
    public async void ReceiveIncomingDetail(){
        // if (NetworkTransport.SessionHasStarted) return;
        
        // Debug.LogWarning("Receive Incoming Detail");

        // var lobby = await LobbyService.Instance.GetLobbyAsync(currentLobbyId);
        // var incomingSessionDetails = IncomingSessionDetails.FromUnityLobby(lobby);
        // this._playerCount = lobby.Players.Count;
        // UpdateStatusText();

        // // This should be replaced with whatever logic you use to determine when a lobby is locked in.
        // if (this.playerCount > 1 && incomingSessionDetails.AddressBook.Count == lobby.Players.Count)
        // {
        //     Debug.LogWarning("Update Session Details");
        //     NetworkTransport.UpdateSessionDetails(incomingSessionDetails);
        // }
    }
    public string LobbyName(){
        return AuthenticationService.Instance.Profile + " _lobby_" + Random.Range(1,100);
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
