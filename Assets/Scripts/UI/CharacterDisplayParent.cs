using System;
using TMPro;
using UnityEngine;

public class CharacterDisplayParent : MonoBehaviour
{
   [SerializeField] private SpriteRenderer hostIndicator;
   [SerializeField] private TextMeshPro readyText;
   [SerializeField] private TextMeshPro playerNameText;

   private void Awake()
   {
      ResetUI();
   }

   public void ShowUI(LobbyMember player)
   {
      readyText.text = player.IsReady ? "Ready" : "Not Ready";
      readyText.color = player.IsReady ? Color.green : Color.red;
      playerNameText.text = player.SteamUserData.Nickname;
      readyText.gameObject.SetActive(true);
      playerNameText.gameObject.SetActive(true);
      hostIndicator.gameObject.SetActive(player.IsLobbyOwner);
   }

   public void ResetUI()
   {
      if (hostIndicator != null)
      {
         hostIndicator.gameObject.SetActive(false);   
      }

      if (readyText != null)
      {
         readyText.gameObject.SetActive(false);
      }

      if (playerNameText != null)
      {
         playerNameText.gameObject.SetActive(false);   
      }
   }

}
