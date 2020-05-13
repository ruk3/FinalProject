using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class sight : MonoBehaviour
{
    // Start is called before the first frame update
    
    public float Angle;
    public float Radius;
    private bool inView;
    public Camera cam;
    private string tag = "runners";
    public NavMeshAgent agent;
    private Transform foundRunner;
    public float mass;
    public Rigidbody rb;
    public float speed = 1.0f;
    private bool isWandering=false;
    private bool isWalking=false;
    private bool isRotatingRight = false;
    private float rotSpeed = 150f;
    private float moveSpeed =10f;
    public Material found;

    private void OnDrawGizmos()
    {
        //creates the border for the field of view
        Vector3 FOVBorder1 = Quaternion.AngleAxis(Angle, transform.up) * transform.forward * Radius;
        Vector3 FOVBorder2 = Quaternion.AngleAxis(-Angle, transform.up) * transform.forward * Radius;


        //shows the fov angle
        Gizmos.color = Color.yellow;
       
        Gizmos.DrawRay(transform.position, FOVBorder1);
        Gizmos.DrawRay(transform.position, FOVBorder2);
        if (inView)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, (transform.forward).normalized * Radius);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, (transform.forward).normalized * Radius);
        }
        //To be continued checking if in the FOV

    }
    
    public static bool FOV(Transform player, string target, float angle, float radius)
    {
        Collider[] overlaps = new Collider[10];
        int count = Physics.OverlapSphereNonAlloc(player.position, radius, overlaps);

        for(int i=0; i < count + 1; i++)
        {
            if (overlaps[i] != null)
            {

                if (overlaps[i].transform.tag == target)
                {
                    Vector3 dirBetween = (overlaps[i].transform.position - player.position).normalized;
                    dirBetween.y = 0;

                    float angBetween = Vector3.Angle(player.forward, dirBetween);

                    if(angBetween<=angle)
                    {
                        Ray ray = new Ray(player.position, overlaps[i].transform.position - player.position);
                        RaycastHit hit;
                        if (Physics.Raycast(ray, out hit, radius))
                        {
                            if (hit.transform == overlaps[i].transform)
                            {
                                return true;
                            }
                        }
                    }

                }
            }
        }
        return false;
    }
    public static Vector3 Destination(Transform player, string target, float angle, float radius)
    {
        Collider[] overlaps = new Collider[10];
        int count = Physics.OverlapSphereNonAlloc(player.position, radius, overlaps);

        for (int i = count+1; i > 0; i--)
        {
            if (overlaps[i] != null)
            {

                if (overlaps[i].transform.tag == target)
                {
                    Vector3 dirBetween = (overlaps[i].transform.position - player.position).normalized;
                    dirBetween.y = 0;

                    float angBetween = Vector3.Angle(player.forward, dirBetween);

                    if (angBetween <= angle)
                    {
                        Ray ray = new Ray(player.position, overlaps[i].transform.position - player.position);
                        RaycastHit hit;
                        if (Physics.Raycast(ray, out hit, radius))
                        {
                            if (hit.transform == overlaps[i].transform)
                            {
                                return hit.point;
                            }
                        }
                    }

                }
            }
        }
        return player.position;
    }




    private Vector3 Seek(Vector3 target)
    {
        var maxSpeed = 9f;
        var pos = transform.position;
        var T = 0.5f;
        var desiredDirection = (target - pos).normalized;
        var desiredVelocity = desiredDirection * Mathf.Max(desiredDirection.magnitude, maxSpeed);

        Vector3 seek = mass * ((desiredVelocity - rb.velocity) / T);

        return seek;
    }

    public void ApplyForce()
    {
        var force = ComputeForce();
        force.y = 0;

        rb.AddForce(force * 10f, ForceMode.Force);
    }

    private Vector3 ComputeForce()
    {
        var destination = Destination(rb.transform, "runners", Angle, Radius);
        var force = Seek(destination);
        if (force != Vector3.zero)
        {
            return force.normalized * Mathf.Min(force.magnitude, 10f);
        }
        else
        {
            return Vector3.zero;
        }

    }


    IEnumerator Wander(bool inView)
    {

        if (inView == false)
        {
            int rotating = Random.Range(1, 3);

            int walkTime = Random.Range(1, 7);

            isWandering = true;


            isWalking = true;
            yield return new WaitForSeconds(walkTime);
            isWalking = false;
            if (rotating == 1)
            {
                isRotatingRight = true;
                yield return new WaitForSeconds(3);
                isRotatingRight = false;
            }

            isWandering = false;
        }
        else
        {
            isWalking = true;
            yield return new WaitForSeconds(6);
            isWalking = false;
        }
    }



    void Start()
    {
        rb = agent.GetComponent<Rigidbody>();
        rb.mass = mass;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (transform.tag == "runners")
        {
            if (collision.gameObject.tag == "Seeker")
            {
                transform.tag = "Seeker";
                rb.GetComponent<MeshRenderer>().material = found;
            }
        }
    }

    private void FixedUpdate()
    {
        if (transform.tag == "Seeker")
        {
            tag = "runners";
        }
        else
        {
            tag = "Seeker";
        }

        inView = FOV(transform, tag, Angle, Radius);
        Vector3 Dest = Destination(transform, tag, Angle, Radius);
        
        if (inView)
        {
            Vector3 targetDirection = Dest - transform.position;
            float singleStep = speed * Time.deltaTime;
            Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDirection, singleStep, 0.0f);
            Debug.DrawRay(transform.position, newDir, Color.red);
            if (tag == "runners")
            {
                transform.rotation = Quaternion.LookRotation(newDir);
            }
            else
            {
                transform.rotation = Quaternion.LookRotation(-newDir);
                isRotatingRight = false;
                isWalking = true;
            }
        }

        if (transform.tag == "Seeker" && inView)
        {
            ApplyForce();
        }
        else
        {
            rb.velocity = Vector3.zero;

        }
        
    }

    private void Update()
    {
        if (transform.tag == "Seeker")
        {
            tag = "runners";
            
        }
        else
        {
            tag = "Seeker";
        }

        inView = FOV(transform, tag, Angle, Radius);
        if (inView==false) {
            if (isWandering == false)
            {
                StartCoroutine(Wander(inView));
            }
            if (isRotatingRight == true)
            {
                transform.Rotate(transform.up * Time.deltaTime * rotSpeed);
            }
            if (isWalking == true)
            {
                transform.position+=transform.forward * Time.deltaTime * moveSpeed;

            }
        }
        else
        {
            if (transform.tag == "runners")
            {
                if (isWandering == false)
                {
                    StartCoroutine(Wander(inView));
                }
            }
            else
            {

            }
        }

    }

}