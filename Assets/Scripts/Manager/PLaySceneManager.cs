using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Cinemachine;
using StarterAssets;

public class PLaySceneManager : Singleton<PLaySceneManager>
{
    [SerializeField] private CinemachineVirtualCamera _playerFollowCamera;
    [SerializeField] public UICanvasControllerInput uiCanvasControllerInput;
    public CinemachineVirtualCamera PlayerFollowCamera { 
        get { 
            if(_playerFollowCamera == null){
                _playerFollowCamera = FindObjectOfType<CinemachineVirtualCamera>();
            }
            return _playerFollowCamera; 
        } 
    }
    // Start is called before the first frame update
    void Start()
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
}
