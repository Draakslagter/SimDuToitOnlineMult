using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using AuthenticationException = System.Security.Authentication.AuthenticationException;

public class ServerRelay : MonoBehaviour
{
    public static ServerRelay Instance {get; private set;}

    [SerializeField] int maxNumberOfPlayers = 4;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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

    private async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();

            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log($"Signed In {AuthenticationService.Instance.PlayerId}");
            };
            
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (AuthenticationException e)
        {
            Debug.LogError($"AuthenticationException: {e.Message}");
        }
    }

    
    public async Task<string> CreateRelay()
    {
        try
        {
            Allocation allocationHolder = await RelayService.Instance.CreateAllocationAsync(maxNumberOfPlayers - 1);
            
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocationHolder.AllocationId);
            
            Debug.Log(joinCode);
            
            RelayServerData relayServerData = allocationHolder.ToRelayServerData("dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();
            
            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"RelayServiceException: {e.Message}");
            return null;
        }
    }

    public async void JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log($"Joining Relay with Code: {joinCode}");
            var joinAllocationHolder = await RelayService.Instance.JoinAllocationAsync(joinCode);
            
            RelayServerData relayServerData = joinAllocationHolder.ToRelayServerData("dtls");
            
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            
            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"RelayServiceException: {e.Message}");
        }
    }
}