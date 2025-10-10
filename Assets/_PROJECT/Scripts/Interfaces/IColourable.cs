using Unity.Netcode;
using UnityEngine;

public interface IColourable
{
   [ClientRpc]
   public void ChangeCubeColourClientRpc(Color contestingColour);
}
