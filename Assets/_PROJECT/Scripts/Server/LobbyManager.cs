using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public enum GameMode
{
    CaptureTheFlag,
    Conquest
}

public enum PlayerCharacter //To Remove/Change
{
    Marine,
    Ninja,
    Zombie
}

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    public const string KEY_PLAYER_NAME = "PlayerName";
    public const string KEY_PLAYER_CHARACTER = "Character"; //To Remove/Change
    public const string KEY_GAME_MODE = "GameMode";

    public event EventHandler OnLeftLobby;

    public event EventHandler<LobbyEventArgs> OnJoinedLobby;
    public event EventHandler<LobbyEventArgs> OnJoinedLobbyUpdate;
    public event EventHandler<LobbyEventArgs> OnKickedFromLobby;
    public event EventHandler<LobbyEventArgs> OnLobbyGameModeChanged;

    public class LobbyEventArgs : EventArgs
    {
        public Lobby lobby;
    }

    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;

    public class OnLobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }

    private GameMode _gameMode;
    private PlayerCharacter _playerCharacter; //To Remove/Change


    private float _lobbyHeartBeatTimer;
    [SerializeField] private float heartBeatTimerMax = 15f;

    private float _lobbyUpdateTimer;
    [SerializeField] private float updateTimerMax = 1.5f;

    private float _lobbyListRefreshTimer;
    [SerializeField] private float listRefreshTimerMax = 5f;

    private Lobby _joinedLobby;
    private string _playerName;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        HandleRefreshLobbyList(); // Disabled Auto Refresh for testing with multiple builds
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
    }

    public async void Authenticate(string playerName)
    {
        _playerName = playerName;
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(playerName);

        await UnityServices.InitializeAsync(initializationOptions);

        AuthenticationService.Instance.SignedIn += () =>
        {
            // do nothing
            Debug.Log("Signed in! " + AuthenticationService.Instance.PlayerId);

            RefreshLobbyList();
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void HandleRefreshLobbyList()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized ||
            !AuthenticationService.Instance.IsSignedIn) return;
        _lobbyListRefreshTimer -= Time.deltaTime;
        if (_lobbyListRefreshTimer > 0f) return;
        _lobbyListRefreshTimer = listRefreshTimerMax;

        RefreshLobbyList();
    }

    private async void HandleLobbyHeartbeat()
    {
        if (!IsLobbyHost()) return;

        _lobbyHeartBeatTimer -= Time.deltaTime;

        if (_lobbyHeartBeatTimer > 0) return;

        _lobbyHeartBeatTimer = heartBeatTimerMax;
        await LobbyService.Instance.SendHeartbeatPingAsync(_joinedLobby.Id);
    }

    private async void HandleLobbyPolling()
    {
        if (_joinedLobby == null) return;

        _lobbyUpdateTimer -= Time.deltaTime;

        if (_lobbyUpdateTimer > 0) return;

        _lobbyUpdateTimer = updateTimerMax;

        _joinedLobby = await LobbyService.Instance.GetLobbyAsync(_joinedLobby.Id);
        
        OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = _joinedLobby });

        if (IsPlayerInLobby()) return;
        // Player was kicked out of this lobby
        Debug.Log("Kicked from Lobby!");

        OnKickedFromLobby?.Invoke(this, new LobbyEventArgs { lobby = _joinedLobby });

        _joinedLobby = null;
    }
    
    public Lobby GetJoinedLobby() {
        return _joinedLobby;
    }

    public bool IsLobbyHost() {
        return _joinedLobby != null && _joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }
    
    private bool IsPlayerInLobby() {
        if (_joinedLobby == null || _joinedLobby.Players == null) return false;
        foreach (Player player in _joinedLobby.Players) {
            if (player.Id == AuthenticationService.Instance.PlayerId) {
                // This player is in this lobby
                return true;
            }
        }
        return false;
    }
    private Player GetPlayer()
    {
        return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject> {
            { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, _playerName) },
            { KEY_PLAYER_CHARACTER, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, PlayerCharacter.Marine.ToString()) } //To Remove/Change
        });
    }
    
    public void ChangeGameMode()  //To be removed
    {
        if (!IsLobbyHost()) return;
        _gameMode = Enum.Parse<GameMode>(_joinedLobby.Data[KEY_GAME_MODE].Value);

        switch (_gameMode) {
            default:
            case GameMode.CaptureTheFlag:
                _gameMode = GameMode.Conquest;
                break;
            case GameMode.Conquest:
                _gameMode = GameMode.CaptureTheFlag;
                break;
        }

        UpdateLobbyGameMode(_gameMode);
    }
    public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate, GameMode gameMode) {
        Player player = GetPlayer();

        CreateLobbyOptions options = new CreateLobbyOptions {
            Player = player,
            IsPrivate = isPrivate,
            Data = new Dictionary<string, DataObject> {
                { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) }
            }
        };

        Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

        _joinedLobby = lobby;

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby }); //WHY?

        Debug.Log("Created Lobby " + lobby.Name);
    }

    public async void RefreshLobbyList() {
        try {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;

            // Filter for open lobbies only //To Change --- Want to add lobby filter UI
            options.Filters = new List<QueryFilter> {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0")
            };

            // Order by newest lobbies first //To Change --- Want to add lobby filter UI
            options.Order = new List<QueryOrder> {
                new QueryOrder(
                    asc: false,
                    field: QueryOrder.FieldOptions.Created)
            };

            QueryResponse lobbyListQueryResponse = await LobbyService.Instance.QueryLobbiesAsync();

            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs { lobbyList = lobbyListQueryResponse.Results });
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void JoinLobbyByCode(string lobbyCode) {
        Player player = GetPlayer();

        Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, new JoinLobbyByCodeOptions {
            Player = player
        });

        _joinedLobby = lobby;

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby }); //WHY?
    }

    public async void JoinLobby(Lobby lobby) {
        Player player = GetPlayer();

        _joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions {
            Player = player
        });

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }

    public async void UpdatePlayerData(string playerName = null)
    {
        bool changed = false;
        if (playerName != null && _playerName != playerName)
        {
            _playerName = playerName;
            changed = true;
        }
        if (!changed) return;
        UpdatePlayerData(playerName, _playerCharacter);
    }

    public async void UpdatePlayerData(PlayerCharacter playerCharacter)
    {
        bool changed = false;
        if (_playerCharacter != playerCharacter)
        {
            _playerCharacter = playerCharacter;
            changed = true;
        }
        if (!changed) return;
        UpdatePlayerData(_playerName, playerCharacter);
    }
    private async void UpdatePlayerData(string playerName, PlayerCharacter playerCharacter) 
    {
        if (_joinedLobby == null) return;
        try {
            UpdatePlayerOptions options = new UpdatePlayerOptions();

            options.Data = new Dictionary<string, PlayerDataObject> {
                {
                    KEY_PLAYER_NAME, new PlayerDataObject(
                        visibility: PlayerDataObject.VisibilityOptions.Public,
                        value: _playerName)
                },
                {
                    KEY_PLAYER_CHARACTER, new PlayerDataObject(
                        visibility: PlayerDataObject.VisibilityOptions.Public,
                        value: _playerCharacter.ToString())
                }
            };

            string playerId = AuthenticationService.Instance.PlayerId;

            Lobby lobby = await LobbyService.Instance.UpdatePlayerAsync(_joinedLobby.Id, playerId, options);
            _joinedLobby = lobby;

            OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = _joinedLobby });
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }
    
    public async void QuickJoinLobby() {
        try {
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();

            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            _joinedLobby = lobby;

            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void LeaveLobby() {
        if (_joinedLobby != null) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, AuthenticationService.Instance.PlayerId);

                _joinedLobby = null;

                OnLeftLobby?.Invoke(this, EventArgs.Empty);
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public async void KickPlayer(string playerId) 
    {
        if (IsLobbyHost()) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, playerId);
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }
    
    public async void UpdateLobbyGameMode(GameMode gameMode) 
    {
        try {
            Debug.Log("UpdateLobbyGameMode " + gameMode);
            
            Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(_joinedLobby.Id, new UpdateLobbyOptions {
                Data = new Dictionary<string, DataObject> {
                    { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) }
                }
            });

            _joinedLobby = lobby;

            OnLobbyGameModeChanged?.Invoke(this, new LobbyEventArgs { lobby = _joinedLobby });
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void StartGame()
    {
        if (!IsLobbyHost()) return;
        
        try
        {
            Debug.Log("Starting game...");

            string relayCode = await ServerRelay.Instance.CreateRelay();

            Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(_joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    // { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                }
            });

            _joinedLobby = lobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log($"Game Start failed: {e.Message}");
        }
    }
}