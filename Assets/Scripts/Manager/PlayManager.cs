using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Cinemachine;
using StarterAssets;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Collections;
using Random = UnityEngine.Random;

public class PlayManager : NetworkBehaviour
{
    public static PlayManager Instance { get; private set; }
    [SerializeField] private CinemachineVirtualCamera _playerFollowCamera;
    [SerializeField] public UICanvasControllerInput uiCanvasControllerInput;
    [SerializeField] public TextMeshProUGUI _playerStatus;
    [SerializeField] public TextMeshProUGUI _amoutPlayerOnline;

    [Header("Logic Game")]
    [SerializeField] private Transform[] listSpawnBonusPosition; /* List positions could be choose for spawn new Bonus */
    [SerializeField] private GameObject policeBonusPrefab; 
    [SerializeField] private GameObject thiefBonusPrefab; 
    [SerializeField] private int maxPoliceBonus;  /* Max Police Bonus could be spawn in game */
    [SerializeField] private int maxThiefBonus; /* Max Thief Bonus could be spawn in game */
    private List<ulong> listPoliceBonusIdSpawned = new List<ulong>(); /* Store List Police Bonus are spawned in game */
    private List<ulong> listThiefBonusIdSpawned = new List<ulong>(); /* Store List Thief Bonus are spawned in game */
    [SerializeField] public Transform policeSpawnTransform;
    [SerializeField] public Transform thiefSpawnTransform;
    [SerializeField] public GameObject explosionBoomPrefab;

    private NetworkVariable<int> playersInRoom = new NetworkVariable<int>();
    [SerializeField] private TextMeshProUGUI[] listPlayerNameText; 
    public int PlayersInRoom
    {
        get { return playersInRoom.Value; }
    }
    Dictionary<ulong, NetCodeThirdPersonController> playersList = new Dictionary<ulong, NetCodeThirdPersonController>();
    public Dictionary<ulong, NetCodeThirdPersonController> PlayersList { get => playersList; }
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
        foreach(TextMeshProUGUI item in listPlayerNameText){
            item.gameObject.SetActive(false);
        }
        _playerStatus.text = PlayerDataManager.Instance.playerData.status.ToString();

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        Debug.Log($"= Start PlaySceneManager : ClientID : {OwnerClientId} IsServer : " + IsServer.ToString());
        if(IsServer){
            SpawnBonusPrefabServerRpc(); 
        }
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"= PlaySceneManager ClientID : {OwnerClientId} OnNetworkSpawn");
        if(IsServer){
            playersInRoom.Value ++;
        }
    }
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client ID {clientId} just Connected...");
        if (IsServer)
        {
            /* If you put this code outside IsServer : get error cannot write */
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
        int i = 0 ;
        foreach(KeyValuePair<ulong, NetCodeThirdPersonController> player in PlayersList){
            // Debug.LogWarning($"= Client ID {player.Key} has Name {player.Value.PlayerName}");
            if(i <= listPlayerNameText.Length){
                listPlayerNameText[i].text = string.Format("#{0}: {3} - {1} - ID : {2} - P : {4}", i+1, player.Value.PlayerName, player.Key.ToString(), player.Value.TypeInGame.ToString(), player.Value.Point);
                listPlayerNameText[i].gameObject.SetActive(true);
                i++;
            }
        }
        for(int j = i; j <= listPlayerNameText.Length - 1; j++){
            listPlayerNameText[j].gameObject.SetActive(false);
        }
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

    #region ServerRPC 
    [ServerRpc]
    private void SpawnBonusPrefabServerRpc(){
        Debug.Log("= SpawnBonusPrefabServerRpc");

        /* Spawn Police Bonus Prefab */
        GameObject bonusP = Instantiate(policeBonusPrefab, listSpawnBonusPosition[Random.Range(0, listSpawnBonusPosition.Length)].position, Quaternion.identity);
        bonusP.transform.position += new Vector3(Random.Range(-1f, 1f), 1f, Random.Range(-1f,1f));
        NetworkObject bonusPoliceNetworkObj = bonusP.GetComponent<NetworkObject>();
        bonusPoliceNetworkObj.Spawn();
        listPoliceBonusIdSpawned.Add(bonusPoliceNetworkObj.NetworkObjectId);

        /* Spawn Police Bonus Prefab */
        GameObject bonusT = Instantiate(thiefBonusPrefab, listSpawnBonusPosition[Random.Range(0, listSpawnBonusPosition.Length)].position, Quaternion.identity);
        bonusT.transform.position += new Vector3(Random.Range(-1f, 1f), 1f, Random.Range(-1f,1f));
        NetworkObject bonusThiefNetworkObj = bonusT.GetComponent<NetworkObject>();
        bonusThiefNetworkObj.Spawn();
        listThiefBonusIdSpawned.Add(bonusThiefNetworkObj.NetworkObjectId);
    }
    [ServerRpc]
    private void PoliceTouchedPoliceBonusServerRpc(ServerRpcParams serverRpcParams = default){
        var senderId = serverRpcParams.Receive.SenderClientId;
        Debug.Log($"= PoliceTouchedPoliceBonusServerRpc: SenderID {senderId} touched ");
    }
    #endregion
}
