using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Cinemachine;
using StarterAssets;
using UnityEngine.SceneManagement;
using TMPro;

public class PLaySceneManager : NetworkBehaviour
{
    public static PLaySceneManager Instance { get; private set; }
    [SerializeField] private CinemachineVirtualCamera _playerFollowCamera;
    [SerializeField] public UICanvasControllerInput uiCanvasControllerInput;
    [SerializeField] public TextMeshProUGUI _playerStatus;
    [SerializeField] public TextMeshProUGUI _amoutPlayerOnline;
    private NetworkVariable<int> playersInRoom = new NetworkVariable<int>();
    public int PlayersInRoom
    {
        get { return playersInRoom.Value; }
    }
    // Dictionary<uint, PlayerData> players = new Dictionary<uint, PlayerData>();
    // public Dictionary<uint, PlayerData> Players { get => players; }
    public CinemachineVirtualCamera PlayerFollowCamera
    {
        get
        {
            if (_playerFollowCamera == null)
            {
                _playerFollowCamera = FindObjectOfType<CinemachineVirtualCamera>();
            }
            return _playerFollowCamera;
        }
    }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        _playerStatus.text = PlayerDataManager.Instance.playerData.status.ToString();
        _amoutPlayerOnline.text = "1";
        if (IsClient)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

    }
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client ID {clientId} just Connected...");
        if (IsServer)
        {
            playersInRoom.Value++;
        }
    }
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client ID {clientId} just disconnected...");
        if (IsServer)
        {
            playersInRoom.Value--;
        }
    }

    void Update()
    {
        _amoutPlayerOnline.text = PlayersInRoom.ToString();
    }
    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
    }
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }
    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }
    public void DisconnectClient()
    {
        Debug.Log("== Disconnect this client ID : " + NetworkManager.Singleton.LocalClientId);
        NetworkManager.Singleton.Shutdown();
        Cleanup();
        PlayerDataManager.Instance.SetStatus(PlayerStatus.Offline);
        SceneManager.LoadScene(0);
    }
    public void Cleanup()
    {
        if (NetworkManager.Singleton != null)
        {
            Destroy(NetworkManager.Singleton.gameObject);
        }
    }
}
