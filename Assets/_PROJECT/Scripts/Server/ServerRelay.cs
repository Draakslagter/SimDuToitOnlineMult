using System;
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
    [SerializeField] int maxNumberOfPlayers = 4;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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

    private async void CreateRelay()
    {
        try
        {
            var allocationHolder = await RelayService.Instance.CreateAllocationAsync(maxNumberOfPlayers - 1);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocationHolder.AllocationId);
            
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocationHolder.RelayServer.IpV4,
                (ushort)allocationHolder.RelayServer.Port,
                allocationHolder.AllocationIdBytes,
                allocationHolder.Key,
                allocationHolder.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"RelayServiceException: {e.Message}");
        }
    }

    private async void JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log($"Joining Relay with Code: {joinCode}");
            var joinAllocationHolder = await RelayService.Instance.JoinAllocationAsync(joinCode);
            
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocationHolder.RelayServer.IpV4,
                (ushort)joinAllocationHolder.RelayServer.Port,
                joinAllocationHolder.AllocationIdBytes,
                joinAllocationHolder.Key,
                joinAllocationHolder.ConnectionData,
                joinAllocationHolder.HostConnectionData
            );
            
            NetworkManager.Singleton.StartClient();
            
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"RelayServiceException: {e.Message}");
        }
    }
}