using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class HideandSeekAgent : MonoBehaviour
{

    private List<Vector3> path;
    private NavMeshAgent nma;
    private Rigidbody rb;

    public enum State
    {
        COUNT,
        SEEK
    }

    public State state;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
