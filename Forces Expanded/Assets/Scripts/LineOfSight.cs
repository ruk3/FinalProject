﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineOfSight : MonoBehaviour
{
    //How sensitive should we be to sight
    public enum SightSensitivity { STRICT, LOOSE };

    //Sight sensitivity
    public SightSensitivity Sensitivity = SightSensitivity.STRICT;

    //Can we see target
    public bool CanSeeTarget = false;

    //FOV
    public float FieldOfView = 45f;

    //Reference to target
    private Transform Target = null;

    //Reference to eyes
    public Transform EyePoint = null;

    //Reference to transform component
    private Transform ThisTransform = null;

    //Reference to sphere collider
    private SphereCollider ThisCollider = null;

    //Reference to last know object sighting, if any
    public Vector3 LastKnownLocation = Vector3.zero;

    void Awake()
    {
        ThisTransform = GetComponent<Transform>();
        ThisCollider = GetComponent<SphereCollider>();
        LastKnownLocation = ThisTransform.position;
        Target = GameObject.FindGameObjectWithTag("Seeker").GetComponent<Transform>();
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

    bool ClearLineofSight()
    {
        RaycastHit Info;

        if (Physics.Raycast(EyePoint.position, (Target.position - EyePoint.position).normalized, out Info, ThisCollider.radius))
        {
            //If player, then can see player
            if (Info.transform.CompareTag("Seeker"))
                return true;
        }

        return false;
    }
    void UpdateSight()
    {
        switch (Sensitivity)
        {
            case SightSensitivity.STRICT:
                CanSeeTarget = InFOV() && ClearLineofSight();
                break;

            case SightSensitivity.LOOSE:
                CanSeeTarget = InFOV() || ClearLineofSight();
                break;
        }
    }
    void OnTriggerStay(Collider Other)
    {
        UpdateSight();

        //Update last known location
        if (CanSeeTarget)
            LastKnownLocation = Target.position;
    }
    //------------------------------------------
    void OnTriggerExit(Collider Other)
    {
        if (!Other.CompareTag("Seeker")) return;

        CanSeeTarget = false;
    }
}
