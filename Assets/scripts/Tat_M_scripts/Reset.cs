using UnityEngine;

public class ResetProgressButton : MonoBehaviour
{
    public void ResetProgress()
    {
        // Сбрасываем все сохранения
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        
        Debug.Log("Прогресс сброшен!");
    }
}