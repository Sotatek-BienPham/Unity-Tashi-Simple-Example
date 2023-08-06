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
                PlaySceneManager.Instance.PlayerFollowCamera.Follow = CinemachineCameraTarget.transform;
            }
```

## Set Number Players In Room in UI : 
- Create a Network Variable store this value : 
```c# 
    private NetworkVariable<int> playersInRoom = new NetworkVariable<int>();
    public int PlayersInRoom
    {
        get { return playersInRoom.Value; }
    }
```
- Then when Start: Handle event OnClientConnected and Disconnected for changing number players in room like this : 
```c#
NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
```
- Excecute change playersInRoom value when in server : When on server change this value, all client gonna be change after.
```c# 
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
```
- Finnaly, in Update we can update UI text to show : 
```c# 
    _amoutPlayerOnline.text = PlayersInRoom.ToString();
```
** The more simple way to show total clients connected : 
- On Start() : Add lines with func OnClientConnected, OnClientDisconnected are same as above: 
```c# 
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        if(IsServer){
            playersInRoom.Value ++;
        }
```
- Then in Update(), set UI text : 
    ```c# 
    _amoutPlayerOnline.text = PlayersInRoom.ToString();
    ```
## Set Username Sync NetCode : 
- In PlayerController (or st containt Player Logic, inherit NetworkBehaviour), create a playerName variable : 
```c# 
        public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>("No-name", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public string PlayerName {
            get {return playerName.Value.ToString();}
        }
        public TextMeshPro playerNameText;
```
- You can create a struct called NetworkString repace for FixedString32Bytes and you can using NetworkVariable such as normaly string type. [Link](https://youtu.be/rFCFMkzFaog?t=1010)

* C1 : - in override func OnNetworkSpawn : 
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
So I found a simple way to set player name for each player : 
- In OnNetworkSpawn() add this line, it'll change name for owner client and change in all other clients : 
```c# 
            if (IsOwner)
            {
                playerName.Value = new FixedString32Bytes(PlayerDataManager.Instance.playerData.name);
            }
```
- in Update() func, set playerNameText : 
        playerNameText.text = PlayerName;

## Manager List Player Network in Play Scene Manager :
- In PlaySceneManager.cs, create variable store player network spawned : 
```c# 
    Dictionary<ulong, NetCodeThirdPersonController> playersList = new Dictionary<ulong, NetCodeThirdPersonController>();
    public Dictionary<ulong, NetCodeThirdPersonController> PlayersList { get => playersList; }
```
- And so I'll update value playersList in player script (NetCodeThirdPersonController for me) : 
```c# 
public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                playerName.Value = new FixedString32Bytes(PlayerDataManager.Instance.playerData.name);
            }
            /* Add new player to list */
            PlaySceneManager.Instance.PlayersList.Add(this.OwnerClientId, this);
            StartLocalPlayer();
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            /* Remove player by clientID from list  */
            PlaySceneManager.Instance.PlayersList.Remove(this.OwnerClientId);
        }
```
- And so on, in PlaySceneManager listPlayer will update automacally when has change. You cound use this player list to show List Player in game (about name, hp, score or st);
- In this project, I firstly create a table to show List Player Name are in room. Back to UI Editor and create UI in play scene.
- And finnally, referencs list table, text row to fill list player in room info table. Example I writing in Update() : 
```c# 
    void Update()
    {
        _amoutPlayerOnline.text = PlayersInRoom.ToString();
        int i = 0 ;
        foreach(KeyValuePair<ulong, NetCodeThirdPersonController> player in PlayersList){
            // Debug.LogWarning($"= Client ID {player.Key} has Name {player.Value.PlayerName}");
            if(i <= listPlayerNameText.Length){
                listPlayerNameText[i].text = string.Format("#{0}: {3} - {1} - ID : {2}", i+1, player.Value.PlayerName, player.Key.ToString(), player.Value.TypeInGame.ToString());
                listPlayerNameText[i].gameObject.SetActive(true);
                i++;
            }
        }
        for(int j = i; j <= listPlayerNameText.Length - 1; j++){
            listPlayerNameText[j].gameObject.SetActive(false);
        }
    }
```

## Add Logic Game Hide and Seek : 
- In this project, The host will be Police and catch all other clients. The clients will be Thief and start running when spawned.
- Create tag Police and Thief. 
- Create variable storage state typeInGame of player , put it in NetCodeThirdPersonController.cs: 
```c# 
private NetworkVariable<PlayerTypeInGame> typeInGame = new NetworkVariable<PlayerTypeInGame>(PlayerTypeInGame.Thief, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public PlayerTypeInGame TypeInGame
        {
            get { return typeInGame.Value; }
        }
```
- OnNetworkSpawn(), Add listten OnTypeInGame value changed, check if IsOwner > if IsHost will setup this Player is Police : change typeInGame.Value and tag such as code below : 
```c# 
        public override void OnNetworkSpawn()
        {
            typeInGame.OnValueChanged += OnTypeInGameChange;
            if (IsOwner)
            {
                playerName.Value = new FixedString32Bytes(PlayerDataManager.Instance.playerData.name);
                /* Host create this room will be Police, and all next clients are thief */
                if (IsHost)
                {
                    typeInGame.Value = PlayerTypeInGame.Police;
                    this.tag = Constants.TAG_POLICE;
                }
                else
                {
                    typeInGame.Value = PlayerTypeInGame.Thief;
                    this.tag = Constants.TAG_THIEF;
                }
            }
            /* .... do st other */
        }
        public void OnTypeInGameChange(PlayerTypeInGame pre, PlayerTypeInGame current){
            this.tag = current.ToString(); /* Police or Thief */
        }
        public override void OnNetworkDespawn()
        { /* Remove listen when OnNetworkDespawn */
            base.OnNetworkDespawn();
            typeInGame.OnValueChanged -= OnTypeInGameChange;
            PlaySceneManager.Instance.PlayersList.Remove(this.OwnerClientId);
        }
```
- Make something diff between Police and Thief, basiclly change text color for simple example, so when set playerNameText, I change the color also in Update(): 
```c# 
            if (TypeInGame == PlayerTypeInGame.Police)
            {
                playerNameText.color = Color.green;
            }
            else
            {
                playerNameText.color = Color.red;
            }
```

============================ LOGIC POLICE TOUCH THIEF ================= 
## Logic Police touch Thief : Show effect, count points, make thief immortar some seconds when get touched by Police : 
### Add Event Manager to manage all events in game : 
- Add script EventManager.cs in first scene and tick isPersistance for DontDestroyOnLoad. Add EventName TouchThief and we'll use it later.
Note that you should go Project Setting > Script Excecute Order and set timing for EventManager running first/before. 
- To check Police have touched thief or not, I've set the tag for particular player with their role. 
- In Third Person Controller, we have to check collide in BasicRigidBodyPush.cs that attach in Player Prefab. 
- In BasicRigidBodyPush.cs, create or adjust OnControllerColliderHit if it's exist like below : 
```c# 
    private void OnControllerColliderHit(ControllerColliderHit hit){
        ...
        /* If not police, dont check collide */
        /* If not police, dont check collide */
        if (this.gameObject.tag != Constants.TAG_POLICE) return;

		/* If you are Police, let's check what you touch */
        if (hit.gameObject.tag == Constants.TAG_THIEF)
        {
            /* Touched to Thief , let's do something */
			NetCodeThirdPersonController target = hit.gameObject.GetComponent<NetCodeThirdPersonController>();
			Debug.Log("Touch to Thief : IsImmortal : " + target.IsImmortal.ToString());
			/* Firstly check if this thief is in immortal state -> do nothing
			If are playing as normal, trigger event that police touch this thief and do some logic */
			if(!target.IsImmortal){
				/* Call func ON Touch Thief. */
				this.gameObject.GetComponent<NetCodeThirdPersonController>().OnTouchThief(target);
				
			}
			
        }
    }
```
- Ok so here, we sent event to this player know that they're touching thief and idetity of that thief via NetCodeThirdPersonController target variable. 
- In NetCodeThirdPersonController.cs, we add some codes : 
```c# 

        #region  Game Logic 
        /* Listen event TouchThief and ready to make notify to server know that I've catched a thief */
        public void OnTouchThief(NetCodeThirdPersonController target)
        {
            Debug.Log($"= Event OnTouchThief : I'm {PlayerName} - ID {OwnerClientId} and I catched a thief has name is {target.PlayerName} - ID: {target.OwnerClientId}");

            /* Call to ServerRpc to notify excute explosion effect for all clients */
            OnPoliceCatchedThiefServerRpc(target.OwnerClientId);
        }
```
- OK greate. Now you can start running 2 Instance game as Police and thief, and whenever Police touch to Thief, in log you'll see something like this : "= Event OnTouchThief : I'm Luuna - ID 1 and I catched a thief has name is Luuna - ID: 1" -> Notify the information that's what I need to next step.
### Show prefab explosion and notify ServerRpc so that all clients are aware that someone has been caught: 
- OnTouchThief we've created above, I'll call a ServerRpc to notyfy excecute explostion for all clients : 
```c# 
    public void OnTouchThief(Dictionary<string, object> msg){
        ...
        /* Call to ServerRpc to notify excute explosion effect for all clients */
            OnPoliceCatchedThiefServerRpc(OwnerClientId, target.OwnerClientId);
    }
```
- Create func OnPoliceCatchedThiefServerRpc like this: 
```c# 
    [ServerRpc]
        public void OnPoliceCatchedThiefServerRpc(ulong fromClientId, ulong targetClientId, ServerRpcParams serverRpcParams = default){
            /* We have 2 ways to this thing : Choose one and comment the other one */
            
            /* Option 1: Spawn on server, so all clients automacally spawn this effect : But got error when you trying destroy this object from clients */
            GameObject explosionVfx = Instantiate(PlaySceneManager.Instance.explosionBoomPrefab);
            explosionVfx.GetComponent<NetworkObject>().Spawn();
            explosionVfx.transform.position = NetworkManager.Singleton.ConnectedClients[targetClientId].PlayerObject.transform.position;

            /* Option 2: Notify for all client know where explosion happend and act it on client : So you can control and destroy this object */
            ShowExplosionEffectInClientRpc(targetClientId);
        }
```
- In this video I'll using option 2 because the explosion it's just to show vfx and not affect to logic/points or something important. 
- So continously create ShowExplosionEffectInClientRpc to receive notify from ServerRpc : 
```c# 
        [ClientRpc]
        public void ShowExplosionEffectInClientRpc(ulong targetClientId){
            /* Receive info from Server and perform explosion in client */
            GameObject explosionVfx = Instantiate(PlaySceneManager.Instance.explosionBoomPrefab);
            explosionVfx.transform.position = PlaySceneManager.Instance.PlayersList[targetClientId].gameObject.transform.position;
            /* I've set auto destroy this particle system when it's done.  */
        }
```
- It's maybe ok rn. Let's run 2 instance game and check collide between police and thief.

### Make game logic IsImmortal for Thief in 3 seconds when have been caught by Police : 
- Create variable isImmortal : 
```c# 
    [Tooltip("isImmortal : true -> police cannot catch this thief when touch. This variable just change on ServerRpc. Don't trust client")]
        private NetworkVariable<bool> isImmortal = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public bool IsImmortal { get { return isImmortal.Value; } }
```
- Listen event when isImmortal value changed in OnNetworkSpawn and OnNetworkDespawn : 
```c# 
        base.OnNetworkSpawn();
        isImmortal.OnValueChanged += OnIsImmortalChange;
        ...
        base.OnNetworkDespawn();
        isImmortal.OnValueChanged -= OnIsImmortalChange;

        /* Cause I change isImmortal in server so in this func just using for Logging */
        public void OnIsImmortalChange(bool pre, bool current)
        {   
            if(!IsOwner) return; /* If it's not owner, do nothing */
            Debug.Log($"= OnIsImmortalChange Client Name {PlayerName} ID {NetworkManager.LocalClientId} change isImmortal from {pre.ToString()} to {current.ToString()}");
        }
```
- Now it's time to detech when we should change isImmortal value. This will be change in server when target thief got caught. Get back to func OnPoliceCatchedThiefServerRpc(), add the line make target thief immortal for some seconds : 
```c# 
    public void OnPoliceCatchedThiefServerRpc(ulong targetClientId, ServerRpcParams serverRpcParams = default){
        ...
            /* Set target Client immortal in some seconds */
            NetCodeThirdPersonController targetPlayer = NetworkManager.Singleton.ConnectedClients[targetClientId].PlayerObject.GetComponent<NetCodeThirdPersonController>();
            targetPlayer.isImmortal.Value = true;
            StartCoroutine(IESetImmortalFalse(targetPlayer, 3f)); /* delay 3 seconds before change isImmortal to false */
    }
    public IEnumerator IESetImmortalFalse(NetCodeThirdPersonController targetPlayer, float delay)
    {
        Debug.Log($"= IESetImmortalFalse Client Name {targetPlayer.PlayerName} Id {targetPlayer.OwnerClientId} start Coroutine change isImmortal to false");
        yield return new WaitForSeconds(delay);
        targetPlayer.isImmortal.Value = false;
    }
``` 
- Almost done.  Run and check.


### Logic count points : Increase/Decrease Point when Police touched Thief : 
- According my point of view, I think have 2 ways : change point and update in Update() or change point and listen OnValueChange to update UI. I use first way. 
- Declare 'Point' variable to store the point of each player  : 
```c# 
       /* Point to count the game logic : Police touch thief -> police's point ++ , thief's point -- */
        private NetworkVariable<int> point = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public int Point {
            get { return point.Value;}
        }
```
- When Police touched Thief, make some login in ServerRpc, and in here, we calc the point (Increase Police's Point and Decrease Thief's point) like below : 
```c# 
        [ServerRpc(RequireOwnership = false)]
        public void OnPoliceCatchedThiefServerRpc(ulong targetClientId, ServerRpcParams serverRpcParams = default){
        ...
        /* Logic Increase Police's point, Decrease Thief's point */
            targetPlayer.point.Value --;
            senderPlayer.point.Value ++;

        }
```
- So now go back to PlaySceneManager and add the text point into table list Player. This table'll reload every update by change this line become : 
```c# 
    ...
    listPlayerNameText[i].text = string.Format("#{0}: {3} - {1} - ID : {2} - P : {4}", i+1, player.Value.PlayerName, player.Key.ToString(), player.Value.TypeInGame.ToString(), player.Value.Point);
```








