
using System.Collections;
using UnityEngine;

public class EnnemyCarrot : MonoBehaviour
{


    [SerializeField]
    private  float _detectionRange=5f;

    [SerializeField]
    private float _firerate=1.5f;


    [SerializeField]
    private float  _fireanticipation=3;

    [SerializeField]
    private float  _speedRotation=180;


    [SerializeField]
    private float  _ballspeed=5;

    [SerializeField]
    private float _burstDelay=0.1f;

    private bool canShoot=true;
    private bool _isfirsttime;
    private enum EnnemyState
    {
        Idle,Alert,Shooting
    }

    private EnnemyState _currentEnnemyState;
    public  GameObject _projectilePrefab;

    public Vector2 V;
    private Vector2 predictedPosition;
  
    public GameObject player;
    private Rigidbody2D rbr2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       rbr2=player.GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {
        predictedPosition=GetPredictedPosition();
       
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
        if (_currentEnnemyState == EnnemyState.Alert ||  _currentEnnemyState == EnnemyState.Shooting)
        {
            RotateTowardsPrediction();
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
        Vector2 centerdir=((Vector3)predictedPosition-transform.position).normalized;
        Debug.DrawLine(transform.position, predictedPosition, Color.green, 1f);
        Debug.DrawRay(predictedPosition, Vector2.up, Color.red, 1f); // Une croix sur la cible
        float [] angles={-15,0,+15} ;
        foreach (float angle in angles)
        {
            Vector2 finaldir=Quaternion.Euler(0,0,angle)*centerdir;
            Spreadball(finaldir);
            yield return new WaitForSeconds(_burstDelay);
        }
    
        yield return new WaitForSeconds(_firerate);        
        canShoot=true;

    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color=Color.red;
        Gizmos.DrawWireSphere(transform.position,_detectionRange);
    }

    void RotateTowardsPrediction()
    {
        Vector2 centerdir=((Vector3)predictedPosition-transform.position).normalized;
        var carrotanglewithDeg=Mathf.Atan2(centerdir.y,centerdir.x)*Mathf.Rad2Deg;
        Quaternion targetRotation=Quaternion.Euler(0,0,carrotanglewithDeg);
        transform.rotation=Quaternion.RotateTowards(transform.rotation,targetRotation,Time.fixedDeltaTime*_speedRotation);
    }

    void Spreadball(Vector2 vector2)
    {
            GameObject ball=Instantiate(_projectilePrefab,transform.position,Quaternion.identity);
            Debug.Log("Spawn projectile");
            Debug.Log(ball);
            var rb2=ball.GetComponent<Rigidbody2D>();
            rb2.linearVelocity=vector2*_ballspeed;   
    }

    Vector2 GetPredictedPosition()
    {
        var  V=rbr2.linearVelocity;
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
                return (Vector2)player.transform.position+rbr2.linearVelocity*tmp;
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
}

