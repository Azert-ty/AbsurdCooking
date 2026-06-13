using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ExitZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (!GameManager.Instance.HasObjective())
        {
            Debug.Log("Il faut récupérer le trésor avant de sortir !");
            return;
        }

        GameManager.Instance.Victory();
    }
}