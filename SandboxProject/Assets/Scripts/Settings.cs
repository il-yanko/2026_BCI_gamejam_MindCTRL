using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private Slider slider;

    void Start()
    {
        float attenuation;
        mixer.GetFloat("masterVolume", out attenuation);
        slider.SetValueWithoutNotify(Mathf.Pow(10, attenuation / 20f));
    }

    public void SetVolume(float linearVolume)
    {
        float attenuation = Mathf.Log10(linearVolume) * 20f;
        mixer.SetFloat("masterVolume", attenuation);
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
