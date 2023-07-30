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
    }
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log("= OnClientConnected : Client Id : " + clientId);
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Client connected. You can now call ServerRpc methods.");
        }

        ReCalcTotalClientsTextServerRPC();
    }
    [ServerRpc(RequireOwnership = false)]
    void ReCalcTotalClientsTextServerRPC()
    {
        int total = NetworkManager.Singleton.ConnectedClients.Count;
        Debug.Log("= Server ReCalcTotalClientsTextServerRPC ConnectedClients: " + total);
        SetTotalClientTextClientRpc(total);
    }
    [ClientRpc]
    void SetTotalClientTextClientRpc(int value)
    {
        Debug.Log("= Client SetTotalClientTextClientRpc ConnectedClients: " + value);
        _amoutPlayerOnline.text = value.ToString();
    }
    void Update()
    {
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
