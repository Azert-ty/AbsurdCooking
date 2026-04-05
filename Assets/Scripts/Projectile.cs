using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField]
    float lifetime=5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {
        Destroy(gameObject,lifetime);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {

        Destroy(gameObject);
        if (collision.tag == "Player")
        {
            Debug.Log("AIE !");
        }
    }
}
