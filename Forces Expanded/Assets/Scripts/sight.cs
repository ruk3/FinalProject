using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sight : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform player;
    public float Angle;
    public float Radius;
    private void OnDrawGizmos()
    {
        //creates the border for the field of view
        Vector3 FOVBorder1 = Quaternion.AngleAxis(Angle, transform.up) * transform.forward * Radius;
        Vector3 FOVBorder2 = Quaternion.AngleAxis(-Angle, transform.up) * transform.forward * Radius;


        //shows the fov angle
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, FOVBorder1);
        Gizmos.DrawRay(transform.position, FOVBorder2);

        //To be continued checking if in the FOV

    }
}