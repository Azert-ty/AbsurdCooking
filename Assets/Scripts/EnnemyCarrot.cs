

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
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
    private float _recoildelay=0.1f;

     [SerializeField]
    private float _recoilspeed=5f;
    
    [SerializeField]
    private LayerMask layerMask;

    [SerializeField]
    private LayerMask layerMask_wall;

    private Transform currenttarget;
    private enum EnnemyState
    {
        PatrolWait,PatrolMove,Alert,Chase,Search
    }



    private EnnemyState _currentEnnemyState;
    public  GameObject _projectilePrefab;

    private Vector2 predictedPosition;
  
    public GameObject player;
    private Rigidbody2D rbr2_player;

    private Rigidbody2D rbr2_Carrot;
    private Vector3 direction_patrol;

    private SpriteRenderer spriteRenderer;


    public List<Transform> waypoints;

    private int currentWaypointIndex=0;

    void Awake()
    {
        
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       rbr2_player=player.GetComponent<Rigidbody2D>();
       spriteRenderer=GetComponent<SpriteRenderer>();
       rbr2_Carrot=GetComponent<Rigidbody2D>();
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


    bool hitawall()
    {
        RaycastHit2D hit2D=Physics2D.Raycast(transform.position,transform.right,Mathf.Infinity,layerMask_wall);

        if (hit2D.collider==null)
        {
            return false;
        }
        return true;
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
                rbr2_Carrot.linearVelocity=Vector2.zero; 
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
        float baseangle=Mathf.Atan2(nextdir.y,nextdir.x);


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
        
        while (Vector2.Distance(rbr2_Carrot.position,waypoints[currentWaypointIndex].transform.position)>0.1f)
        {
            float distance=Vector2.Distance(rbr2_Carrot.position,waypoints[currentWaypointIndex].transform.position);
            var direction_patrol=(waypoints[currentWaypointIndex].transform.position-(Vector3)rbr2_Carrot.position).normalized;
            float speedfactor=Mathf.SmoothStep(0.2f,1f,distance);
            float finalspeed=carrot_speed*speedfactor;
            rbr2_Carrot.MovePosition((Vector3)rbr2_Carrot.position+direction_patrol*finalspeed*Time.fixedDeltaTime);
            float angle=Mathf.Atan2(direction_patrol.y,direction_patrol.x)*Mathf.Rad2Deg;
            var targetRotation= Quaternion.Euler(0,0,angle);
            var currentrotation=transform.rotation;
            transform.rotation=Quaternion.RotateTowards(currentrotation,targetRotation,Time.fixedDeltaTime*maxDegreesDelta_for_patrol_move);
            yield return new WaitForFixedUpdate();
        }
        // rbr2_Carrot.MovePosition(currenttarget.transform.position);
        yield return new WaitForSeconds(PatrolMoveDelay + Random.Range(-0.2f, 0.2f));
        // currenttarget=(currenttarget==waypointA.transform)?waypointB.transform:waypointA.transform;
        currentWaypointIndex=(currentWaypointIndex+1)%waypoints.Count;
        changeState(EnnemyState.PatrolWait);
        yield break;
        
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


