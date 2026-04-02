using System;
using System.Collections;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class EnnemyCarrot : MonoBehaviour
{


    [SerializeField]
    private  float _detectionRange=5f;

    [SerializeField]
    private float _firerate=1.5f;


    [SerializeField]
    private int  _fireanticipation=3;

    private bool canShoot=true;
    private bool _isfirsttime;
    private enum EnnemyState
    {
        Idle,Alert,Shooting
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
       
        // else
        // {
        //     _currentEnnemyState=EnnemyState.Idle;
        // }

        if(_currentEnnemyState==EnnemyState.Alert && canShoot)
        {
            StartCoroutine(Attack());
        }

        
    }

    // IEnumerator Shoot()
    // {
        
    //     canShoot=false;

    //     Debug.Log("Shoot");
    //     yield return new WaitForSeconds(_firerate);
    //     canShoot=true;

    // }

    // IEnumerator AlertShoot()
    // {
    //     yield return new WaitForSeconds(_fireanticipation);
        
    // }

    IEnumerator Attack()
    {
        canShoot=false;
         if ((player.transform.position - transform.position).sqrMagnitude <_detectionRange *_detectionRange)
        {
            _currentEnnemyState=EnnemyState.Alert;
            yield return new WaitForSeconds(_fireanticipation); 
            _isfirsttime=true;
            _currentEnnemyState=EnnemyState.Shooting;
        }
        if (_currentEnnemyState==EnnemyState.Shooting)
        {
            Debug.Log("shoot");
            yield return new WaitForSeconds(_firerate); 
            canShoot=true;
             _currentEnnemyState=EnnemyState.Shooting;
        }
        else
        {
            _isfirsttime=false;
            _currentEnnemyState=EnnemyState.Idle;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color=Color.red;
        Gizmos.DrawWireSphere(transform.position,_detectionRange);
    }
}
