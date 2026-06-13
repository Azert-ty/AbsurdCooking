using TMPro;
using UnityEngine;

public class BlinkText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMesh;

    private void Update()
    {
        float alpha = Mathf.PingPong(Time.time * 2f, 0.5f) + 0.5f;

        Color color = textMesh.color;
        color.a = alpha;

        textMesh.color = color;
    }
}