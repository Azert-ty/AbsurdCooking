

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(SpriteRenderer))]
public class EnnemyCarrot : MonoBehaviour
{
    


     [SerializeField]
    private float angle=45f;
    [SerializeField]
    private  float PatrolMoveDelay;

    [SerializeField]
    private  float carrot_speed=5f;

    [SerializeField]
    private  float maxDegreesDelta_for_patrol_move=5f;
    
    [SerializeField]
    private  float PatrolWaitDelay=2f;

    [SerializeField]
    private  float maxDegreesDelta_for_patrol_wait=2f;

    [SerializeField]
    private  float _detectionRange=5f;
    
    [SerializeField]
    private  float leftangle=5f;
    [SerializeField]
    private  float rightangle=5f;

    [SerializeField]
    private float _stundelay=1.5f;


    [SerializeField]
    private float  _fireanticipation=0.4f;

    [SerializeField]
    private float  _speedRotation=180;


 
    
    
    [SerializeField]
    private LayerMask layerMask;

    [SerializeField]
    private LayerMask layerMask_wall;

    private enum EnnemyState
    {
        PatrolWait,PatrolMove,Alert,Chase,Search
    }



    private EnnemyState _currentEnnemyState;
    public  GameObject _projectilePrefab;

    private Vector2 predictedPosition;
  
    public GameObject player;


    private SpriteRenderer spriteRenderer;


    public List<Transform> waypoints;

    private int currentWaypointIndex=0;

    private NavMeshAgent navMeshAgent;

    void Awake()
    {
        
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       spriteRenderer=GetComponent<SpriteRenderer>();

        navMeshAgent=GetComponent<NavMeshAgent>();
        navMeshAgent.updateRotation = false;
        navMeshAgent.updateUpAxis = false;



       _currentEnnemyState = EnnemyState.PatrolMove;
        StartCoroutine(PatrolMoveRoutine());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {
        
        bool detectionSystem=DetectionSystem();
        if (detectionSystem && (_currentEnnemyState==EnnemyState.PatrolMove || _currentEnnemyState==EnnemyState.PatrolWait))
        {
             changeState(EnnemyState.Alert);
        }
        if (!detectionSystem && (_currentEnnemyState!=EnnemyState.PatrolMove && _currentEnnemyState!=EnnemyState.PatrolWait))
        {   
            changeState(EnnemyState.PatrolMove);
        }


    
    }




    bool  DetectionSystem()
    {
        float sqrtRange=(player.transform.position-transform.position).sqrMagnitude;
        bool Range=sqrtRange<_detectionRange*_detectionRange;
        if (!Range)
        {
            return false ;
        }

        Vector2 forward=transform.right;
        Vector2 directtoPlayer=(player.transform.position-transform.position).normalized;
        float dot=Vector2.Dot(forward,directtoPlayer);
        if (dot < Mathf.Cos(angle*Mathf.Deg2Rad) )
        {
            return false;
        }

        RaycastHit2D hit=Physics2D.Raycast(transform.position,directtoPlayer,Mathf.Sqrt(sqrtRange),layerMask);
        Debug.DrawRay(transform.position,directtoPlayer*Mathf.Sqrt(sqrtRange),Color.red);
        if(hit.collider==null)
        {
            return false;
        }
        

        if (hit.collider.gameObject==player)
        {
            return true;
        }

        return false;
    }

    void changeState(EnnemyState newState)
    {
        StopAllCoroutines();
        _currentEnnemyState=newState;

        switch (newState)
        {
            case EnnemyState.Alert:
                StartCoroutine(AlertRoutine());
                break;
            
            
            case EnnemyState.PatrolWait:
                StartCoroutine(PatrolWaitRoutine());
                break;
            case EnnemyState.PatrolMove:
                StartCoroutine(PatrolMoveRoutine());
                break;
            
        }
    }

    IEnumerator PatrolWaitRoutine()
    {
        Vector2 nextdir=(waypoints[currentWaypointIndex].position-transform.position).normalized;
        float baseangle=Mathf.Atan2(nextdir.y,nextdir.x)*Mathf.Rad2Deg;


       float [] angles={baseangle ,baseangle+leftangle,baseangle-rightangle,baseangle};
       foreach (float angle in angles)
        {
            var targetRotation=Quaternion.Euler(0,0,angle);
            while (Quaternion.Angle(transform.rotation,targetRotation)>0.5f)
            {
                transform.rotation=Quaternion.RotateTowards(transform.rotation,targetRotation,Time.deltaTime*maxDegreesDelta_for_patrol_wait);
                yield return null;
            };  
             yield return new WaitForSeconds(0.2f) ;   
        };
        yield return new WaitForSeconds(PatrolWaitDelay) ;   
        changeState(EnnemyState.PatrolMove);
       
    }

    IEnumerator PatrolMoveRoutine()
    {
        
        navMeshAgent.SetDestination(waypoints[currentWaypointIndex].transform.position);
        navMeshAgent.isStopped=false;
        while (navMeshAgent.pathPending || 
      (navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance && navMeshAgent.hasPath))
        {
           
            Vector2 velocity=navMeshAgent.desiredVelocity.normalized;
           if (velocity.sqrMagnitude>0.1f)
           {
             float angle=Mathf.Atan2(velocity.y,velocity.x)*Mathf.Rad2Deg;
             var targetRotation= Quaternion.Euler(0,0,angle);
             var currentrotation=transform.rotation;
             transform.rotation=Quaternion.RotateTowards(currentrotation,targetRotation,Time.deltaTime*maxDegreesDelta_for_patrol_move);
              
             
           }
            yield return null;
        }
        navMeshAgent.isStopped=true;
        yield return new WaitForSeconds(PatrolMoveDelay );
        currentWaypointIndex=(currentWaypointIndex+1)%waypoints.Count;
        changeState(EnnemyState.PatrolWait);
        
    }
    IEnumerator AlertRoutine()
    {
        spriteRenderer.color=Color.red;
        Debug.Log("L'IA t'a vu ! Elle prépare son tir...");
        yield return new WaitForSeconds(_fireanticipation);
        spriteRenderer.color=Color.black;
        // changeState(EnnemyState.Shooting);
    }

   

    void OnDrawGizmos()
    {
        Gizmos.color=Color.blue;
        Gizmos.DrawWireSphere(transform.position,_detectionRange); 
        Vector2 forward=transform.right;

        Gizmos.color=Color.yellow;
        Gizmos.DrawLine(transform.position,transform.position+(Vector3)forward*3f);


       
        Vector2 left=Quaternion.Euler(0,0,angle)*forward;
        Vector2 right=Quaternion.Euler(0,0,-angle)*forward;

        Gizmos.color=Color.red;
        Gizmos.DrawLine(transform.position,transform.position+(Vector3)left*3f);

        Gizmos.DrawLine(transform.position,transform.position+(Vector3)right*3f);

    }
    
}


