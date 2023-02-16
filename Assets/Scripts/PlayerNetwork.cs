using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;


public class PlayerNetwork : NetworkBehaviour
{

    [SerializeField] private Transform spawnedObjectPrefab;
    private Transform spawnedObjectTransform;
    private NetworkVariable<MyCustomData> randomNumber = new NetworkVariable<MyCustomData>(new MyCustomData
    {
        _int = 56,
        _bool = true,

    }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner); //need to define and declare together,needs NetworkBehaviour class
    //thru networkVariable we can sync any numerical value across server. like in this instance, when we start we'll get 1 value on both sides, but when we press T and any random no. will be generated, and will be shown on both sides the same.
    //everyone can read, and owner can write means both server and client can press T and values changed and synced. if set to .Server then only server/host can do that
    //the types of NetworkVariable should be Value types only eg int float enum bool struct

    public struct MyCustomData : INetworkSerializable
    {
        //need to Serialize custom data types for network manager otherwise will get error.
        public int _int;
        public bool _bool;
        public FixedString128Bytes _message;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {   //these 2 statements serialize the values from  custom data type.

            serializer.SerializeValue(ref _int);
            serializer.SerializeValue(ref _bool);
            serializer.SerializeValue(ref _message);
        }
    }
    public override void OnNetworkSpawn() //override it, instead of virtual, to stop the 999+ console noti for same val, just show once when the val changed
    {
        randomNumber.OnValueChanged += (MyCustomData previousValue, MyCustomData newValue) =>
        {
            Debug.Log(OwnerClientId + " ; randomNumber :  " + newValue._int + " ; " + newValue._bool + " ; " + newValue._message);
        };
    }
    private void Update()
    {



        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.T))
        {   
            spawnedObjectTransform =   Instantiate(spawnedObjectPrefab);
            spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true); //to spawn it on network,on both sides. but if spwned from client, throw an error, to make this happen use ServerRpc
            // TestServerRpc(new ServerRpcParams());
            // TestClientRpc(new ClientRpcParams {Send = new ClientRpcSendParams {TargetClientIds = new List<ulong> {1}}}); // can send messages to specific clients based on IDs, rn its sending messagr from server to a clientID of 1
            // randomNumber.Value = new MyCustomData
            // {
            //     _int = 10,
            //     _bool = false,
            //     _message = "yaba diba dooo",
            // };
        }

        if(Input.GetKeyDown(KeyCode.Y)){
            // spawnedObjectTransform.GetComponent<NetworkObject>().Despawn(true); //use when you want to just remove it from network but want to keep it alive so you can maybe spawn it back again
            Destroy(spawnedObjectTransform.gameObject);
        }

        Vector3 moveDir = new Vector3(0, 0, 0);

        if (Input.GetKey(KeyCode.W)) moveDir.z = +1f;
        if (Input.GetKey(KeyCode.S)) moveDir.z = -1f;
        if (Input.GetKey(KeyCode.A)) moveDir.x = -1f;
        if (Input.GetKey(KeyCode.D)) moveDir.x = +1f;

        float moveSpeed = 3f;
        transform.position += moveDir * moveSpeed * Time.deltaTime;

    }

    [ServerRpc] private void TestServerRpc(ServerRpcParams serverRpcParams){ //only runs on server, not on client
        Debug.Log("TestServerRpc " + OwnerClientId + " ; " + serverRpcParams.Receive.SenderClientId); //this params sends only 0/1 in logs, 0 fro host, 1 for client, only visible on host side, to figure out which client send which message
    }

    [ClientRpc] private void TestClientRpc(ClientRpcParams clientRpcParams){ 
        Debug.Log("testClientRpc");
    }

    
}

