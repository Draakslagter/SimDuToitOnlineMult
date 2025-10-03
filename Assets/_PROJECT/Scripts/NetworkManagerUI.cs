using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

//Script written in class
public class NetworkManagerUI : MonoBehaviour
{
   [SerializeField] private Button serverBtn, hostBtn, clientBtn;

   private void Awake()
   {
      serverBtn.onClick.AddListener(() =>
      {
         NetworkManager.Singleton.StartServer();
      });

      hostBtn.onClick.AddListener(() =>
      {
         NetworkManager.Singleton.StartHost();
      });
      clientBtn.onClick.AddListener(() =>
      {
         NetworkManager.Singleton.StartClient();
      });
      
      
   }
}
