# Simple Tashi Step by step : 

## Import base package before Tashi : 
- In Unity Editor > Package manager > Unity Registry : Find and Install some packages (Netcode of gameobjects , Lobby, Relay maybe).

## Import newest Tashi package : 
- 28/7/2023 : 0.3.0 is available here : https://github.com/tashigg/tashi-network-transport/releases/tag/v0.3.0  (Update Tashi Relay)
- Download it and put tgz into folder Assets > Plugins 
- In Unity Editor > Window > Package Manager > Add package from tar ball > Choose Tashi.tgz from Assets > Plugins. 

- Tashi Relay URL https://eastus.relay.infra.tashi.dev/  ( Steven )

## Add Network Manager and Funtional button : 
- Create empty GameObject, add Network Manager component : Choose Transport protocol, drag player prefab and setup network prefabs lists.
- Choose Tashi Network Transport : Fill setup for relay base url or not. 
- Create in UI some buttons such as Start Server, Start Host and Start Client. Each button will call the function corresponding to function in Network Manager. 
- Create empty GameObject and attach new script to manage this scene. Script maybe have name like PlaySceneManager.cs 
- Open PlaySceneManager.cs and add some basically func : Start Host, Start Server, Start Client. using Unity.NetCode to use NetworkManager.Singleton or get info about NetCode.
- Add onClick for 3 button that we created before
- In Player Prefabs : Add component Network Object, Network Transform, Network Animation .. for sync some basic info. 
- Here I override Network Transform and Animation to Client Network Transform, Client Network Animation to turn off authoriatative from server. Trust on your clients.

* When import using Tashi, got an error when build to Mac app : The type 'Random' is defined in an assembly that is not referenced. You must add a reference to assembly 'Unity.Mathematics, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'.  So back to using NetCode for continues.

## Setup Player : Move, control 
* Target : Setup for control right client owned, camera follow client owned. 
- In Third Person Control : In Update > Check if(!IsOwner) return; so if you're not owner of this client, you can control. 
- In PlaySceneManager.cs, create variable for PlayerFollowCamera, refer it from editor or load from script : 
```c# 
[SerializeField] private CinemachineVirtualCamera _playerFollowCamera;
    public CinemachineVirtualCamera PlayerFollowCamera { 
        get { 
            if(_playerFollowCamera == null){
                _playerFollowCamera = FindObjectOfType<CinemachineVirtualCamera>();
            }
            return _playerFollowCamera; 
        } 
    }
```
- At Start func in Player script, check FollowCamera if it's owner and local player. 
```c# 

if (IsLocalPlayer && IsOwner)
            {
                PLaySceneManager.Instance.PlayerFollowCamera.Follow = CinemachineCameraTarget.transform;
            }
```


## Set Username Sync NetCode : 
- In PlayerController (or st containt Player Logic, inherit NetworkBehaviour), create a playerName variable : 
```c# 
        public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>();
        public TextMeshPro playerNameText;
```
- You can create a struct called NetworkString repace for FixedString32Bytes and you can using NetworkVariable such as normaly string type. [Link](https://youtu.be/rFCFMkzFaog?t=1010)
- in override func OnNetworkSpawn : 
```c# 
    /* Listen event when playerName changed value on server */
    playerName.OnValueChanged += OnPlayerNameChanged;
    /* Check if this client spawned, so set the player name notice to server */
    if(IsLocalPlayer){
        SetPlayerNameServerRpc(PlayerDataManager.Instance.playerData.name);
    }
```
- Create ServerRpc and ClientRpc : 
```c# 
        [ServerRpc(RequireOwnership = false)]
        public void SetPlayerNameServerRpc(string name)
        {
            Debug.Log(" SetPlayerNameServerRpc : " + name);
            /* When Network Variable change in server, it'll trigger event, notify to all clients via event OnValueChanged */
            playerName.Value = new FixedString32Bytes(name);
        }
```
- At func listen event PlayerName Change : OnPlayerNameChanged : do something such as set text in UI
```c# 
      private void OnPlayerNameChanged(FixedString32Bytes previous, FixedString32Bytes current)
        {
            Debug.Log($"= ClientID {NetworkManager.LocalClientId} detect Player Name Change : {current}");
            playerNameText.text = current.ToString();
        }
```
- Get back to Player prefab, Create UI Text show player name, and referencens it to variable `playerNameText` that declare at first step. 
- That's almost done : Start Host with name, Client join also have their custom name. 
* But still got a Bug. that's in last client view, the names of the previous players have not been updated. I'll fix it in nexts step. 
