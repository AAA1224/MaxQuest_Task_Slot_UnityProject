using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;
using UnitySocketIO;
using UnitySocketIO.Events;
using UnitySocketIO.SocketIO;

public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance { get; private set; }
    public GameObject socketControllerPrefab;
    public RoomSyncJSON roomStatus { get; private set; }

    [Header("Connection Settings")]
    public SocketIOController socket;
    public SocketIOSettings settings;
    [Space]
    
    [HideInInspector]
    public bool trigger_checkOnline = false;
    public bool _Online { get; private set; } = false;


    #region Events
    [HideInInspector]
    public delegate void OnSync();
    public static OnSync OnSyncronize;

    #endregion

    #region Player
    public string playerNickname { private set; get; }

    public void SetPlayerNickname(string nickname)
    {
        playerNickname = nickname;
    }
    #endregion

    #region UnityCallbacks

    private void Awake()
    {
        playerNickname = "";

        if (Instance == null && !Global.createdSocket)
            Instance = this;
        else
        {
            DestroyImmediate(this.gameObject);
            return;
        }

        Debug.Log("socket_state" + socket);
        if (socket == null && !Global.createdSocket)
        {
            Debug.Log("createSocket");
            socket = Instantiate(socketControllerPrefab).GetComponent<SocketIOController>();
            socket.gameObject.AddComponent<DontDestructableObj>();
            socket.Initialize(settings);
            
        }
    }

    private void OnDestroy()
    {
        if(socket != null)
            socket.Emit("player:leave");
    }

    #endregion

    #region Sockets
    public void EnterServer()
    {
        if (_Online)
            return;
        if (playerNickname == "") playerNickname = "Player #" + Random.Range(1000,9999);
        
        socket.On("player:enter", OnPlayerEnter);
        socket.On("room:sync", OnSyncRoom);
        socket.On("player:connected", OnSocketConnected);

        StartCoroutine(ConnectSocketToServer());
    }
    public void EnterDefaultServer()
    {
        StartCoroutine(ConnectSocketToServer());
    }
    IEnumerator ConnectSocketToServer() {
        print("Connecting...");
        yield return new WaitForSeconds(.5f);
        socket.Connect();
        _Online = true;
    }
    #endregion

    #region JSON Objets
    [System.Serializable]
    public class RoomSyncJSON
    {
        public RoomSyncData[] players;

        [System.Serializable]
        public class RoomSyncData
        {
            public string username;
            public int balance;
            public int seat;
            public string[] hand;
            public int avatar = 1;
        }
    }

    #endregion

    #region Suscribed Functions

    public void OnPlayerEnter(SocketIOEvent soIOEvent)
    {
        print(soIOEvent.data.ToString());
    }

    public void OnSyncRoom(SocketIOEvent soIOEvent)
    {

        roomStatus = JsonUtility.FromJson<RoomSyncJSON>(soIOEvent.data.ToString());
        print(soIOEvent.data.ToString());

    }

    public void OnSocketConnected(SocketIOEvent soIOEvent)
    {
        print(soIOEvent.data.ToString());
        SlotManager.instance.OnSocketConnected();
        if (trigger_checkOnline) {
            trigger_checkOnline = false;
            ReturnToLobby();
        }
    }

    #endregion

    public void ReturnToLobby()
    {
        socket.Emit("player:leave");
        StartCoroutine(DestroySocket());
    }

    private IEnumerator DestroySocket()
    {
        yield return null;
        // RoomGuide.GoToLobby();
    }

    public static SocketIOController GetSocket() => Instance.socket;
}
