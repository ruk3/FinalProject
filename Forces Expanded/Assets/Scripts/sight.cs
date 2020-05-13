using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class sight : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform player;
    public float Angle;
    public float Radius;
    private bool inView;
    public Camera cam;
    private string tag = "runners";
    public NavMeshAgent agent;
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
            Gizmos.DrawRay(transform.position, (player.position-transform.position).normalized * Radius);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, (player.position-transform.position).normalized * Radius);
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

        if (Input.GetMouseButtonDown(0)&& tag=="runners")
        {
            
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    agent.SetDestination(hit.point);
                }
            
        }
    }

    

}