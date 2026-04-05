using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField]
    float lifetime=5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject,lifetime);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {
        
    }

    void OnTriggerEnter2D(Collider2D collision)
    {

        if (collision.CompareTag("Enemy")) return;
        if (collision.CompareTag("Player"))
        {
            
            Debug.Log("AIE !");
        }
        Destroy(gameObject);
    }
}
