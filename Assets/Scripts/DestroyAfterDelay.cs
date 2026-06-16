using UnityEngine;

public class DestroyAfterDelay : MonoBehaviour
{
    [SerializeField] private float delay = 1.5f;

    private void Start()
    {
        Destroy(gameObject, delay);
    }
}