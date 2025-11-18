using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PlayerPrefExample : MonoBehaviour
{
   [Header("UI References")]
   [SerializeField] private TMP_InputField nameInput;
   [SerializeField] private Slider volumeSlider;
   [SerializeField] private TMP_Text infoText;

   private void Awake()
   {
      if (!nameInput || !volumeSlider || !infoText)
      {
         Debug.Log("UI Refernces not assigned");
         enabled = false;
         return;
      }

      LoadSettings();
   }

   public void LoadSettings()
   {
      string playerName;
      float volume;

      if (PlayerPrefs.HasKey(PlayerPrefKeys.PlayerName)
         || PlayerPrefs.HasKey(PlayerPrefKeys.Volume))
      {
         playerName = PlayerPrefs.GetString(PlayerPrefKeys.PlayerName);
         volume = PlayerPrefs.GetFloat(PlayerPrefKeys.Volume);
      }
      else
      {
         playerName = "";
         volume = 1.0f;
         infoText.text = "Default Settings Applied";
      }

      nameInput.text = playerName;
      volumeSlider.value = volume;

      AudioListener.volume = volume;
   }

   public void saveSettings()
   {
      string playerName = nameInput.text?.Trim() ?? "";
      float volume = Mathf.Clamp01(volumeSlider.value);
      
      PlayerPrefs.SetString(PlayerPrefKeys.PlayerName, playerName);
      PlayerPrefs.SetFloat(PlayerPrefKeys.Volume, volume);
      
      PlayerPrefs.Save();
      infoText.text = "Settings saved";
   }

   public void ResetSettings()
   {
      PlayerPrefs.DeleteKey(PlayerPrefKeys.PlayerName);
      PlayerPrefs.DeleteKey(PlayerPrefKeys.Volume);
      
      nameInput.text = "";
      volumeSlider.value = 1.0f;
      AudioListener.volume = 1.0f;
   }
}
