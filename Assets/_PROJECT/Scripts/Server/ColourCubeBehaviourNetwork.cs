using Unity.Netcode;
using UnityEngine;

public class ColourCubeBehaviourNetwork : NetworkBehaviour, IColourable
{
    private static readonly int ContestingColour = Shader.PropertyToID("_contestingColour");
    private Renderer _cubeRenderer;

    private void Awake()
    {
        if (GetComponent<Renderer>() == null)
        {
            _cubeRenderer = GetComponent<Renderer>();
        }
    }
    
    
    [ClientRpc]
    public void ChangeCubeColourClientRpc(Color contestingColour)
    {
        _cubeRenderer.material.SetColor(ContestingColour,contestingColour);
    }
}
