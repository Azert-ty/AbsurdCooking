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
       
        float sqrtRange=(player.transform.position-transform.position).sqrMagnitude;
        bool Range=sqrtRange<_detectionRange*_detectionRange;
        if (Range && canShoot)
        {
            StartCoroutine(Attack());
        }
        if (!Range)
        {   
            _isfirsttime=true;
            _currentEnnemyState=EnnemyState.Idle;
        }
        
    }

    IEnumerator Attack()
    {
       canShoot=false;
        if (_isfirsttime)
        {
            _currentEnnemyState=EnnemyState.Alert;
            Debug.Log("L'IA t'a vu ! Elle prépare son tir...");
            yield return new WaitForSeconds(_fireanticipation);
            _isfirsttime=false;
            
        }
        _currentEnnemyState=EnnemyState.Shooting;
        Debug.Log("PAN");
        yield return new WaitForSeconds(_firerate);        
        canShoot=true;

    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color=Color.red;
        Gizmos.DrawWireSphere(transform.position,_detectionRange);
    }
}
