
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnnemyCarrot : MonoBehaviour
{


    [SerializeField]
    private  float carrot_speed=5f;
    
    [SerializeField]
    private  float PatrolWaitDelay=2f;

    [SerializeField]
    private  float maxDegreesDelta=2f;

    [SerializeField]
    private  float _detectionRange=5f;
    
    [SerializeField]
    private  float leftangle=5f;
    [SerializeField]
    private  float rightangle=5f;

    [SerializeField]
    private float _firerate=1.5f;

    [SerializeField]
    private float _stundelay=1.5f;


    [SerializeField]
    private float  _fireanticipation=0.4f;

    [SerializeField]
    private float  _speedRotation=180;


    [SerializeField, Range(6f, 20f)]
    private float  _ballspeed=6;

    [SerializeField]
    private float _burstDelay=0.1f;
     [SerializeField]
    private float _recoildelay=0.1f;

     [SerializeField]
    private float _recoilspeed=5f;
    private bool canShoot=true;
    private bool _isfirsttime;

    private Transform currenttarget;
    private enum EnnemyState
    {
        Alert,Shooting,Stun,PatrolWait,PatrolMove
    }


    private EnnemyState _currentEnnemyState;
    public  GameObject _projectilePrefab;

    private Vector2 predictedPosition;
  
    public GameObject player;
    private Rigidbody2D rbr2_player;

    private Rigidbody2D rbr2_Carrot;
    private Vector3 direction_patrol;

    private SpriteRenderer spriteRenderer;

    public GameObject waypointA;
    public  GameObject waypointB;

    void Awake()
    {
        
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       rbr2_player=player.GetComponent<Rigidbody2D>();
       spriteRenderer=GetComponent<SpriteRenderer>();
       rbr2_Carrot=GetComponent<Rigidbody2D>();
       currenttarget=waypointB.transform;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {
        if(_currentEnnemyState==EnnemyState.Stun) return;
        predictedPosition=GetPredictedPosition();
        float sqrtRange=(player.transform.position-transform.position).sqrMagnitude;
        bool Range=sqrtRange<_detectionRange*_detectionRange;
        if (Range && (_currentEnnemyState==EnnemyState.PatrolMove || _currentEnnemyState==EnnemyState.PatrolWait))
        {

             changeState(EnnemyState.Alert);
        }
        if (!Range && (_currentEnnemyState!=EnnemyState.PatrolMove && _currentEnnemyState!=EnnemyState.PatrolWait))
        {   
            changeState(EnnemyState.PatrolMove);
        }
        if (_currentEnnemyState == EnnemyState.Alert ||  _currentEnnemyState == EnnemyState.Shooting)
        {
            RotateTowardsPrediction();
        }
        
       
        
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
            case EnnemyState.Shooting:
                StartCoroutine(ShootingRoutine());
                break;
            case EnnemyState.Stun:
                StartCoroutine(StunRoutine());
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

       float [] angles={leftangle,rightangle,0};
       foreach (float angle in angles)
        {
            var targetRotation=Quaternion.Euler(0,0,angle);
            while (Quaternion.Angle(transform.rotation,targetRotation)>0.5f)
            {
                transform.rotation=Quaternion.RotateTowards(transform.rotation,targetRotation,Time.deltaTime*maxDegreesDelta);
                yield return null;
            };       
        };
        changeState(EnnemyState.PatrolMove);
       
    }

    IEnumerator PatrolMoveRoutine()
    {
        
        while (Vector2.Distance(rbr2_Carrot.position,currenttarget.transform.position)>0.1f)
        {
            var direction_patrol=(currenttarget.transform.position-(Vector3)rbr2_Carrot.position).normalized;
            rbr2_Carrot.MovePosition((Vector3)rbr2_Carrot.position+direction_patrol*carrot_speed*Time.fixedDeltaTime);
            yield return null;
        }
        rbr2_Carrot.MovePosition(currenttarget.transform.position);
        yield return new WaitForSeconds(PatrolWaitDelay * 0.5f);
        currenttarget=(currenttarget==waypointA.transform)?waypointB.transform:waypointA.transform;
        changeState(EnnemyState.PatrolWait);
        yield break;
        
    }
    IEnumerator AlertRoutine()
    {
        spriteRenderer.color=Color.red;
        Debug.Log("L'IA t'a vu ! Elle prépare son tir...");
        yield return new WaitForSeconds(_fireanticipation);
        spriteRenderer.color=Color.black;
        changeState(EnnemyState.Shooting);
    }

    IEnumerator ShootingRoutine()
    {
        
        while (true)
        {
            predictedPosition=GetPredictedPosition();
            Debug.Log("PAN");
            Vector2 centerdir=((Vector3)predictedPosition-transform.position).normalized;
            Debug.DrawLine(transform.position, predictedPosition, Color.green, 1f);
            Debug.DrawRay(predictedPosition, Vector2.up, Color.red, 1f); // Une croix sur la cible
            float [] angles={-15,0,+15} ;
            foreach (float angle in angles)
            {
                Vector2 finaldir=Quaternion.Euler(0,0,angle)*centerdir;
                Spreadball(finaldir);
                yield return StartCoroutine(Recoil(centerdir));
                yield return new WaitForSeconds(_burstDelay);
            }
            
    
        
            yield return new WaitForSeconds(_firerate);  
    
            if ((player.transform.position-transform.position).sqrMagnitude>_detectionRange*_detectionRange)
            {
               changeState(EnnemyState.PatrolWait);   
               yield break;
            }
        }
        
            
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color=Color.yellow;
        Gizmos.DrawWireSphere(transform.position,_detectionRange);
        Gizmos.DrawLine(waypointA.transform.position, waypointB.transform.position);

        
    }

    void RotateTowardsPrediction()
    {
        Vector2 centerdir=((Vector3)predictedPosition-transform.position).normalized;
        var carrotanglewithDeg=Mathf.Atan2(centerdir.y,centerdir.x)*Mathf.Rad2Deg;
        Quaternion targetRotation=Quaternion.Euler(0,0,carrotanglewithDeg);
        transform.rotation=Quaternion.RotateTowards(transform.rotation,targetRotation,Time.fixedDeltaTime*_speedRotation);
    }

    
IEnumerator Recoil(Vector2 direction)
{
    float duration = 0.1f;
    float t = 0;

    Vector2 basePosition = rbr2_Carrot.position;

    while (t < duration)
    {
        float strength = Mathf.Sin((t / duration) * Mathf.PI);
        Vector2 offset = -direction * 0.3f * strength;

        rbr2_Carrot.MovePosition(basePosition + offset);

        t += Time.deltaTime;
        yield return null;
    }

    rbr2_Carrot.MovePosition(basePosition);
}


    void Spreadball(Vector2 vector2)
    {
            GameObject ball=Instantiate(_projectilePrefab,transform.position,Quaternion.identity);
            Debug.Log("Spawn projectile");
            Debug.Log(ball);
            var rb2=ball.GetComponent<Rigidbody2D>();
            rb2.linearVelocity=vector2*_ballspeed;
            
    }

    public Vector2 GetPredictedPosition()
    {
        var  V=rbr2_player.linearVelocity;
        var D= player.transform.position-transform.position;
        var b= 2*Vector2.Dot(D,V);
        var a= Vector2.Dot(V,V)-_ballspeed*_ballspeed;
        var c=Vector2.Dot(D,D);
        var delta=b*b-4*a*c;
        if (Mathf.Abs(a) < 0.0001f)
        {
            return player.transform.position;
        }
        if (delta > 0)
        {
            var t1=(-b+Mathf.Sqrt(delta))/(2*a);
            var t2=(-b-Mathf.Sqrt(delta))/(2*a);

            var tmp=-1f;
            if (t1 > 0)
            {
                tmp=t1;
            }
            if (0 < t2)
            {
                if (tmp < 0)
                {
                    tmp=t2;
                }
                else
                {
                    tmp= tmp<t2? tmp : t2;
                }
            }
            if (tmp < 0)
            {
                return player.transform.position;
            }
            else
            {
                return (Vector2)player.transform.position+rbr2_player.linearVelocity*tmp;
            }

            
            
        }
        else if (Mathf.Abs(delta) < 0.0001f)
        {
            var t0=-b/(2*a);

            return (Vector2)player.transform.position+V*t0;
        }
        else
        {
            return player.transform.position;
        }
    }


    IEnumerator  StunRoutine()
    {
        
        spriteRenderer.color=Color.gold;
        yield return new WaitForSeconds(_stundelay);
        spriteRenderer.color=Color.black;
        changeState(EnnemyState.PatrolWait);

    }

    
    public void OntriggerHit()
    {
       changeState(EnnemyState.Stun);
    }


    
}


