using System;
using Unity.VisualScripting;
using UnityEngine;

public class EnnemyCarrot : MonoBehaviour
{


    [SerializeField]
    private  float _detectionRange=5f;

    public GameObject player;
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
        if ((player.transform.position - transform.position).sqrMagnitude <_detectionRange *_detectionRange)
        {
            Debug.Log("Missiles");
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color=Color.red;
        Gizmos.DrawWireSphere(transform.position,_detectionRange);
    }
}
