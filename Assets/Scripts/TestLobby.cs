using System.Collections;
using System.Collections.Generic;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class TestLobby : MonoBehaviour
{
    public Lobby hostLobby;
    public Lobby joinedLobby;
    public float heartBeatTimer;
    public float lobbyUpdateTomer;
    public string PlayerName;
    public async void Start()
    {
        await UnityServices.InitializeAsync(); //to initialise unity gaming service, needs unity.services.core, needs to be async await
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId); //AutheticationService needs unity.service.authentication
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync(); //anonmously login, no need to sign up with username or anything
        // CreateLobby();
        // ListLobbies();
        PlayerName = "Pranjali" + UnityEngine.Random.Range(0, 99);
        Debug.Log(PlayerName);
    }

    public void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates(); //this function updates the lobby if someones joined and shows it. w/o this we only see updates from one side,if someone joins, other side of lobby wont be able to see that someone joined
    }

    //create heartbeat for our lobby, so it doesnt get inactive after 30sec, we need to keep it alive for longer so players can join
    public async void HandleLobbyHeartbeat()
    {

        if (hostLobby != null)
        {
            heartBeatTimer -= Time.deltaTime;
            if (heartBeatTimer < 0f)
            {
                float heartBeatTimeMax = 15;
                heartBeatTimer = heartBeatTimeMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }

    private async void HandleLobbyPollForUpdates()
    {
        //1 request per second

        if (joinedLobby != null)
        {
            lobbyUpdateTomer -= Time.deltaTime;
            if (lobbyUpdateTomer < 0f)
            {
                float lobbyUpdateTimerMax = 15;
                lobbyUpdateTomer = lobbyUpdateTimerMax;

                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = lobby;
            }
        }


    }

    public async void CreateLobby()
    {
        //this function creates a lobby, wrap in try catch so if lobby not created for any reason it doesnt throw error
        try
        {
            string lobbyName = "MyLobby";
            int maxPlayers = 4;

            //createLobbyoptions create public and private lobbies
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>{
                    {"GameMode", new DataObject(DataObject.VisibilityOptions.Public,"CaptureTheFlag")},
                    {"Map", new DataObject(DataObject.VisibilityOptions.Public,"de_dust2")}
                }

            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions); //Lobby needs unity.service.lobbies.models, Lobbyservice needs unity.service.lobbyservices

            hostLobby = lobby;
            joinedLobby = hostLobby;

            Debug.Log("Created Lobby! " + lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Id + " " + lobby.LobbyCode);
            PrintPlayer(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    //to search for lobbies
    public async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            { //we can add filters to our lobbies, like which lobbies to show
                Count = 25,
                Filters = new List<QueryFilter>{
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0",QueryFilter.OpOptions.GT),
                    // new QueryFilter(QueryFilter.FieldOptions.S1,"CaptureTheFlag",QueryFilter.OpOptions.EQ)
                },
                Order = new List<QueryOrder>{
                    new QueryOrder(false,QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions); //if QueryLobbiesAsync args empty it'll list all the lobbies

            Debug.Log("Lobbies found" + queryResponse.Results);
            foreach (Lobby lobby in queryResponse.Results)
            {
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Data["GameMode"].Value);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }

    public async void JoinLobbyByCode(string lobbyCode)
    {
        //to join lobbies

        try
        {
            // QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            // await Lobbies.Instance.JoinLobbyByIdAsync(queryResponse.Results[0].Id);
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };
            Lobby lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);
            joinedLobby = lobby;
            Debug.Log("Joined Lobby with code " + lobbyCode);
            PrintPlayer(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }


    }

    //players can quickly join lobby
    private async void QuickJoinLobby()
    {
        try
        {
            await LobbyService.Instance.QuickJoinLobbyAsync();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private Player GetPlayer()
    {
        return new Player
        {

            Data = new Dictionary<string, PlayerDataObject>{
                        {"PlayerName",new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,PlayerName)}
                    }
        };
    }

    private void PrintPlayers()
    {
        PrintPlayer(joinedLobby);
    }

    //to get player info that have joined the lobby
    private void PrintPlayer(Lobby lobby)
    {
        Debug.Log("Players in Lobby " + lobby.Name + " " + lobby.Data["GameMode"].Value + " " + lobby.Data["Map"].Value);
        foreach (Player player in lobby.Players)
        {
            Debug.Log(player.Id + " " + player.Data["PlayerName"].Value);
        }
    }

    private async void UpdateLobbyGameMode(string gameMode)
    {
        try
        {
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>{
                {
                    "GameMode", new DataObject(DataObject.VisibilityOptions.Public,gameMode)
                }
            }
            });
            joinedLobby = hostLobby;
            PrintPlayer(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    //to update PlayerData on when joining lobby
    private async void UpdatePlayerName(string newPlayerName)
    {
        try
        {
            PlayerName = newPlayerName;
            await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>{
                {"PlayerName",new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,PlayerName)}
            }
            });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    //to leave a lobby
    private void LeaveLobby()
    {
        try
        {
            LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    //to kick player by host
    private void KickPlayer()
    {
        try
        {
            LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, joinedLobby.Players[1].Id); //kick [1] 2nd player as 1st player is host
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    //host migration
    private async void MigrateLobbyHost(){
        try{
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id,new UpdateLobbyOptions{
                HostId = joinedLobby.Players[1].Id
            });
            joinedLobby = hostLobby;
            PrintPlayer(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    //to delete lobby
    private async void DeleteLobby(){
        try{
            await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

}
