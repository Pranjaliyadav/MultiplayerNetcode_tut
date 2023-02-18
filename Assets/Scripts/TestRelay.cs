using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine.Networking;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;


public class TestRelay : MonoBehaviour
{
    public async void Start()
    {
        await UnityServices.InitializeAsync(); //initialsie unity api services, need unity.serives.core

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed In " + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync(); //needs unity.services.authentication
    }

    //create relay
    public async void CreateRelay()
    {
        try
        {
            //allocation needs unity.services.relay.models 
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3); //createAllocationAsync takes no. of player excluding host, so if 3, the total players are 4 inluding 1 host
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log(joinCode);
            //also takes the region where relay should be created, if left empty, decides the ebst on its own
            
            //relayeServerData needs unity.networking.transport.relay
            RelayServerData relayServerData = new RelayServerData(allocation,"dtls");

            //networkManger needs unity.netcode, unityTransport needs unity.netcode.transport.utp
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            

        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void JoinRelay(string joinCode)
    {
        try { 
            Debug.Log("Joining Relay with " + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

             //relayeServerData needs unity.networking.transport.relay
            RelayServerData relayServerData = new RelayServerData(joinAllocation,"dtls");

            //networkManger needs unity.netcode, unityTransport needs unity.netcode.transport.utp
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();
             }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

}
