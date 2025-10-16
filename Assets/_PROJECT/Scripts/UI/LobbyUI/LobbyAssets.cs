using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyAssets : MonoBehaviour {



    public static LobbyAssets Instance { get; private set; }


    [SerializeField] private Sprite marineSprite;
    [SerializeField] private Sprite ninjaSprite;
    [SerializeField] private Sprite zombieSprite;


    private void Awake() {
        Instance = this;
    }

    public Sprite GetSprite(PlayerCharacter playerCharacter) {
        switch (playerCharacter) {
            default:
            case PlayerCharacter.Marine:   return marineSprite;
            case PlayerCharacter.Ninja:    return ninjaSprite;
            case PlayerCharacter.Zombie:   return zombieSprite;
        }
    }

}