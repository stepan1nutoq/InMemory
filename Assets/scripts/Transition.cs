using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition: MonoBehaviour
{
    public void Transition(string Scene)
    {
        SceneManager.LoadScene(Scene);
    }
}