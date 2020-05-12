using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class Agent : MonoBehaviour
{
    public float radius;
    public float mass;
    public float perceptionRadius;
    public Vector3 velocity;

    private List<Vector3> path;
    private NavMeshAgent nma;
    private Rigidbody rb;

    private HashSet<GameObject> nearbyNeighbors = new HashSet<GameObject>();
    private HashSet<GameObject> contactWalls = new HashSet<GameObject>();
    private string scene_name;

    #region LineofSightVariables
    public enum SightSensitivity { STRICT, LOOSE };

    //Sight sensitivity
    public SightSensitivity Sensitity = SightSensitivity.STRICT;

    //Can we see target
    public bool CanSeeAgent = false;

    //FOV
    public float FieldOfView = 45f;

    //Reference to eyes
    public Transform EyePoint = null;

    //Reference to last know object sighting, if any
    public Vector3 LastKnowSighting = Vector3.zero;
    #endregion

    void Awake()
    {
        LastKnowSighting = transform.position;
    }

    void Start()
    {
        scene_name = SceneManager.GetActiveScene().name;
        path = new List<Vector3>();
        nma = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        velocity = rb.velocity;

        gameObject.transform.localScale = new Vector3(2 * radius, 1, 2 * radius);
        nma.radius = radius;
        rb.mass = mass;
        GetComponent<SphereCollider>().radius = perceptionRadius / 2;

    }

    private void Update()
    {
        if (path.Count > 1 && Vector3.Distance(transform.position, path[0]) < 1.1f)
        {
            path.RemoveAt(0);
        } else if (path.Count == 1 && Vector3.Distance(transform.position, path[0]) < 2f)
        {
            path.RemoveAt(0);

            if (path.Count == 0)
            {
                gameObject.SetActive(false);
                AgentManager.RemoveAgent(gameObject);
            }
        }

        #region Visualization

        if (false)
        {
            if (path.Count > 0)
            {
                Debug.DrawLine(transform.position, path[0], Color.green);
            }
            for (int i = 0; i < path.Count - 1; i++)
            {
                Debug.DrawLine(path[i], path[i + 1], Color.yellow);
            }
        }

        if (false)
        {
            foreach (var neighbor in nearbyNeighbors)
            {
                Debug.DrawLine(transform.position, neighbor.transform.position, Color.yellow);
            }
        }

        #endregion
    }

    #region Public Functions

    public void ComputePath(Vector3 destination)
    {
        nma.enabled = true;
        var nmPath = new NavMeshPath();
        nma.CalculatePath(destination, nmPath);
        path = nmPath.corners.Skip(1).ToList();
        //path = new List<Vector3>() { destination };
        //nma.SetDestination(destination);
        nma.enabled = false;
    }

    public Vector3 GetVelocity()
    {
        return rb.velocity;
    }

    #endregion

    #region Incomplete Functions

    private Vector3 ComputeForce()
    {
        //var force = Vector3.zero;
        if (scene_name == "SF Scene")
        {
            var force = CalculateGoalForce(maxSpeed: 1)+ CalculateAgentForce() + CalculateWallForce();
            if (force != Vector3.zero)
            {
                return force.normalized * Mathf.Min(force.magnitude, Parameters.maxSpeed);
            }
            else
            {
                return Vector3.zero;
            }
        }
        else if (scene_name == "followTheLeader")
        {
            var force = CalculateFollowTheLeader();
            if (force != Vector3.zero)
            {
                return force.normalized * Mathf.Min(force.magnitude, Parameters.maxSpeed);
            }
            else
            {
                return Vector3.zero;
            }

        }
        else if (scene_name == "PursueAndEvade")
        {
            var force = CalculateFollowPursueAndEvade(maxSpeed: 3);

            if (force != Vector3.zero)

            {
                return force.normalized * Mathf.Min(force.magnitude, Parameters.maxSpeed);
            }

            else
            {
                return Vector3.zero;
            }

        }
        else
        {
            return Vector3.zero;
        }
    }

    private Vector3 CalculateGoalForce(float maxSpeed)
    {
        var desiredDirection = path[0] - transform.position;
        var desiredVelocity = desiredDirection.normalized * Mathf.Min(desiredDirection.magnitude, maxSpeed);

        Vector3 goalForce = mass * ((desiredVelocity - rb.velocity) / Parameters.T);

        return goalForce;
    }

    private Vector3 CalculateAgentForce()
    {
        var agentForce = Vector3.zero;

        foreach (var i in nearbyNeighbors)
        {
            if (!AgentManager.IsAgent(i))
            {
                continue;
            }

            var neighbor = AgentManager.agentsObjs[i];
            var direction = transform.position - neighbor.transform.position;
            direction = direction.normalized;
            var overlap = (radius + neighbor.radius) - Vector3.Distance(transform.position, neighbor.transform.position);
            var g = overlap > 0f ? overlap : 0;
            var tan = Vector3.Cross(Vector3.up, direction);

            var psychForce = Parameters.A * Mathf.Exp(overlap / Parameters.B);
            var nonPenForce = Parameters.k * g;
            var slidingFricForce = Parameters.Kappa * g * Vector3.Dot(rb.velocity - neighbor.GetVelocity(), tan) * tan;

            agentForce += direction * (psychForce + nonPenForce) + slidingFricForce;

        }

        return agentForce;
    }

    private Vector3 CalculateWallForce()
    {
        var wallForce = Vector3.zero;

        foreach (var i in contactWalls)
        {
            var direction = transform.position - i.transform.position;
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
            {
                direction.z = 0;
                direction.y = 0;
            } else
            {
                direction.x = 0;
                direction.y = 0;
            }
            direction = direction.normalized;

            var overlap = (radius + 0.5f) - Vector3.Distance(transform.position, i.transform.position);
            var g = overlap > 0f ? overlap : 0;
            var tan = Vector3.Cross(Vector3.up, direction);

            var psychForce = (Parameters.A * 0.005f) * Mathf.Exp(overlap / Parameters.B);
            var nonPenForce = (Parameters.k * 0.01f)* g;
            var slidingFricForce = Parameters.Kappa * g * Vector3.Dot(rb.velocity, tan) * tan;

            wallForce += direction * (psychForce + nonPenForce) - slidingFricForce;
        }
        
        return wallForce;
    }
    private Vector3 CalculateFollowTheLeader()
    {
        var agentForce = Vector3.zero;
        var followDistance = 3f;

        GetComponent<SphereCollider>().radius = 10;
        bool isLeader = (int.Parse(name.Split(' ')[1])) == 0;
        var agentRender = GetComponent<Renderer>();

        if (!isLeader)
        {
            foreach (var i in nearbyNeighbors)
            {
                var neighbor = AgentManager.agentsObjs[i];
                var leader = int.Parse(neighbor.name.Split(' ')[1]) == 0;
                
                if (leader)
                {
                    var tv = neighbor.GetVelocity() * -1f;
                    tv = tv.normalized * followDistance;
                    var followPoint = i.transform.position + tv;
                    var ahead = i.transform.position + tv * -1f; ;

                    var desiredDirection = followPoint - transform.position;
                    var desiredVelocity = desiredDirection.normalized * desiredDirection.magnitude;

                    agentForce += mass * ((desiredVelocity - rb.velocity) / Parameters.T) + CalculateAgentForce() * 0.0001f;
                }
                else
                {
                    agentForce += CalculateAgentForce() * 0.00001f;
                }
            }
        } else
        {

            agentRender.material.SetColor("_Color", Color.green);

            var desiredDirection = path[0] - transform.position;
            var desiredVelocity = desiredDirection.normalized * desiredDirection.magnitude;

            agentForce = (desiredVelocity - rb.velocity) / Parameters.T;

        }

        return agentForce;

    }
    
    private Vector3 CalculateFollowPursueAndEvade(float maxSpeed)
    {
        var agentForce = Vector3.zero;
        AgentManager.instance.destination = new Vector3(49,0,49);
        GetComponent<SphereCollider>().radius = 10;
        bool isEvader = (int.Parse(name.Split(' ')[1])) %2==0;
       
        if (isEvader)
        {
            var agentRender = GetComponent<Renderer>();
            agentRender.material.SetColor("_Color", Color.green);
            foreach (var n in nearbyNeighbors.Where(n => AgentManager.IsAgent(n)))
            {
                var neighbor = AgentManager.agentsObjs[n];
                var direction = (transform.position - neighbor.transform.position);
                direction = direction.normalized;
                var desiredVelocity = direction.normalized * Mathf.Min(direction.magnitude, maxSpeed);
                var overlap = (radius + neighbor.radius) - Vector3.Distance(transform.position, neighbor.transform.position);
                var g = overlap > 0f ? overlap : 0;
                var tan = Vector3.Cross(Vector3.up, direction);

                var slidingFricForce = g * Vector3.Dot(rb.velocity - neighbor.GetVelocity(), tan) * tan;

                var pursuer = int.Parse(n.name.Split(' ')[1]) %2== 1;
                if (pursuer)
                {
                    agentForce += mass * ((desiredVelocity - rb.velocity));
                    break;
                } else
                {
                    agentForce += direction + slidingFricForce;
                }
            }
        }
        else
        {
            foreach (var n in nearbyNeighbors.Where(n => AgentManager.IsAgent(n)))
            {
                var neighbor = AgentManager.agentsObjs[n];
                var desiredDirection = neighbor.transform.position - transform.position;
                var desiredVelocity = desiredDirection.normalized * Mathf.Min(desiredDirection.magnitude, maxSpeed);

                var evader = int.Parse(n.name.Split(' ')[1]) %2==0;
                if (evader)
                {
                    agentForce += mass * ((desiredVelocity - rb.velocity));
                    break;
                }
            }
        }
        return agentForce;
    }

    private Vector3 Pursue()
    {
        GameObject closestNeighbor;
        var closestDistance = Mathf.Infinity;

        foreach (var i in nearbyNeighbors)
        {
            var distance = (EyePoint.position - i.transform.position).magnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNeighbor = i;
            }
        }

        var T = 3;
        //var predictPosition = closestNeighbor.transform.position + closestNeighbor.velocity;

        return Vector3.zero;
    }

    public void ApplyForce()
    {
        var force = ComputeForce();
        force.y = 0;

        rb.AddForce(force * 10, ForceMode.Force);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (AgentManager.IsAgent(other.gameObject))
        {
            nearbyNeighbors.Add(other.gameObject);
        }
    }
    
    public void OnTriggerExit(Collider other)
    {
        if (nearbyNeighbors.Contains(other.gameObject))
        {
            nearbyNeighbors.Remove(other.gameObject);
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (WallManager.IsWall(collision.gameObject))
        {
            contactWalls.Add(collision.gameObject);
        }
    }

    public void OnCollisionExit(Collision collision)
    {
        if (contactWalls.Contains(collision.gameObject))
        {
            contactWalls.Remove(collision.gameObject);
        }
    }

    bool InFOV()
    {
        //Get direction to target
        Vector3 DirToTarget = Target.position - EyePoint.position;

        //Get angle between forward and look direction
        float Angle = Vector3.Angle(EyePoint.forward, DirToTarget);

        //Are we within field of view?
        if (Angle <= FieldOfView)
            return true;

        //Not within view
        return false;
    }

    #endregion
}
