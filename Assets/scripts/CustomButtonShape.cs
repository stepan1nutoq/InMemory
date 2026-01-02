using UnityEngine;
using UnityEngine.UI;

public class CustomButtonShape : MonoBehaviour
{
    public float alpha = 0.1f; // 0 - прозрачные участки, 1 - непрозрачные

    void Start()
    {
        GetComponent<Image>().alphaHitTestMinimumThreshold = alpha;
    }
}
