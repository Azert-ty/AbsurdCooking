using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class EnnemyCarrot : MonoBehaviour
{


    [SerializeField]
    private  float _detectionRange=5f;

    [SerializeField]
    private float _firerate=1.5f;

    private bool canShoot=true;
    private enum EnnemyState
    {
        Idle,Shooting
    }

    private EnnemyState _currentEnnemyState;


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
            _currentEnnemyState=EnnemyState.Shooting;
            
        }
        else
        {
            _currentEnnemyState=EnnemyState.Idle;
        }

        if (_currentEnnemyState == EnnemyState.Shooting && canShoot==true)
        {
            StartCoroutine(Shoot());
        }
    }

    IEnumerator Shoot()
    {
        
        canShoot=false;
        Debug.Log("Shoot");
        yield return new WaitForSeconds(_firerate);
        canShoot=true;

    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color=Color.red;
        Gizmos.DrawWireSphere(transform.position,_detectionRange);
    }
}
