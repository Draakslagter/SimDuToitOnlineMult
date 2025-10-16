using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class TestLobby : MonoBehaviour
{
    private Lobby _hostLobby;
    private Lobby _joinedLobby;
    
    private float _lobbyHeartBeatTimer;
    [SerializeField] private float heartBeatTimerMax = 15f;
    
    private float _lobbyUpdateTimer;
    [SerializeField] private float updateTimerMax = 1.5f;

    private string _playerName;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log($"Signed In {AuthenticationService.Instance.PlayerId}");
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        _playerName = $"Simon {UnityEngine.Random.Range(1, 100)}";
        Debug.Log(_playerName);
    }

    private void Update()
    {
        HandleLobbyHeartBeat();
        HandleLobbyPullForUpdates();
    }

    private async void HandleLobbyHeartBeat()
    {
        if (_hostLobby == null) return;
        
        _lobbyHeartBeatTimer -= Time.deltaTime;
        
        if (_lobbyHeartBeatTimer > 0) return;
        
        _lobbyHeartBeatTimer  = heartBeatTimerMax;
        await LobbyService.Instance.SendHeartbeatPingAsync(_hostLobby.Id);
    }

    private async void HandleLobbyPullForUpdates()
    {
        if (_joinedLobby == null) return;
        
        _lobbyUpdateTimer -= Time.deltaTime;
        
        if (_lobbyUpdateTimer > 0) return;
        
        _lobbyUpdateTimer  = updateTimerMax;
        
        Lobby lobby = await LobbyService.Instance.GetLobbyAsync(_joinedLobby.Id);
        _joinedLobby = lobby;
    }
    public async void CreateLobby()
    {
        try
        {
            var lobbyName = "TestLobby";
            int maxPlayers = 4;

            CreateLobbyOptions lobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = true,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    {"GameMode", new DataObject(DataObject.VisibilityOptions.Public, "Colour The Blocks")},
                    {"Map", new DataObject(DataObject.VisibilityOptions.Public, "de_dust2")}
                }
            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, lobbyOptions);
            
            _hostLobby = lobby;
            _joinedLobby = _hostLobby;
            
            PrintPlayers(_hostLobby);
            Debug.Log($"Lobby created: {lobby.Name}\nMax Players: {lobby.MaxPlayers}\nLobby ID: {lobby.Id}\n Lobby Code: {lobby.LobbyCode}");
        }
        catch (LobbyServiceException e)
        {
            Debug.Log($"Lobby creation failed: {e.Message}");
        }
    }

    public async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();
        
            Debug.Log($"Lobbies found: {queryResponse.Results.Count}");
            foreach (Lobby lobby in queryResponse.Results)
            {
                Debug.Log($"Lobby found: {lobby.Name}\nMax Players: {lobby.MaxPlayers}\nGame mode: {lobby.Data["GameMode"].Value}");
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log($"Lobby search failed: {e.Message}");
        }
    }

    public async void JoinLobby(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions lobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();
            
            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, lobbyByCodeOptions);
            _joinedLobby =  lobby;
            
            Debug.Log($"Lobby joined with code: {lobbyCode}");
            
            PrintPlayers(lobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log($"Lobby join failed: {e.Message}");
        }
    }

    public async void QuickJoinLobby()
    {
        try
        {
            await LobbyService.Instance.QueryLobbiesAsync();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log($"Lobby quick join failed: {e.Message}");
        }
    }

    private Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, _playerName) },
            }
        };
    }
    private void PrintPlayers(Lobby lobby)
    {
        Debug.Log($"Players found: {lobby.Name}\nGame mode: {lobby.Data["GameMode"].Value}\nMap: {lobby.Data["Map"].Value}");
        foreach (Player player in lobby.Players)
        {
            Debug.Log($"Player ID: {player.Id}\nPlayer Data: {player.Data["PlayerName"].Value}");
        }
    }

    private void PrintPlayers()
    {
        PrintPlayers(_joinedLobby);
    }

    private async void UpdateLobbyGameMode(string gameMode)
    {
        try
        {
            _hostLobby = await LobbyService.Instance.UpdateLobbyAsync(_hostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    {"GameMode", new DataObject(DataObject.VisibilityOptions.Public, gameMode)}
                }
            });
            _joinedLobby =  _hostLobby;
            
            PrintPlayers(_hostLobby);
        }
        catch (LobbyServiceException e)
        {
           Debug.Log($"Lobby update failed: {e.Message}");
        }
    }

    private async void UpdatePlayerName(string newPlayerName)
    {
        try
        {
            _playerName =  newPlayerName;
            await LobbyService.Instance.UpdatePlayerAsync(_joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, _playerName) }
                }
            });
        }
        catch (LobbyServiceException e)
        {
             Debug.Log($"Player update failed: {e.Message}");
        }
    }

    private async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, AuthenticationService.Instance.PlayerId);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log($"Lobby leave failed: {e.Message}");
        }
    }

    private async void KickPlayer()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, _joinedLobby.Players[1].Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log($"Lobby leave failed: {e.Message}");
        }
    }

    private async void MigrateLobbyHost()
    {
        try
        {
            _hostLobby = await LobbyService.Instance.UpdateLobbyAsync(_hostLobby.Id, new UpdateLobbyOptions
            {
                HostId = _joinedLobby.Players[1].Id
            });
            _joinedLobby =  _hostLobby;
            
            PrintPlayers(_hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log($"Lobby update failed: {e.Message}");
        }
    }

    private async void DeleteLobby()
    {
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(_joinedLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log($"Lobby delete failed: {e.Message}");
        }
    }
}