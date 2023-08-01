using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.SceneManagement;
using System;

public class MenuSceneManager : Singleton<MenuSceneManager>
{
    [SerializeField] private TMP_InputField _nameTextField;
    [SerializeField] private string SceneGamePlayName = "PlayScene";
    void Start()
    {
        string name = PlayerPrefs.GetString(Constants.NAME_PREF, "");
        PlayerDataManager.Instance.SetName(name);
        _nameTextField.text = name;
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
