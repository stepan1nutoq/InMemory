using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

// скрипт нужен в главном меню чтобы при перезапуске игры настройки громкости сохранялись

public class MusicLoad : MonoBehaviour
{
    public string volumeParameter = "MasterVolume";
    public AudioMixer mixer;
    private const float _multiplier = 20f;
    private float _volumeValue;
    public Slider slider;

    void Start()
    {
        _volumeValue = PlayerPrefs.GetFloat(volumeParameter, Mathf.Log10(slider.value) * _multiplier);
        mixer.SetFloat(volumeParameter, _volumeValue);
    }
}
