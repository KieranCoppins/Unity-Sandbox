using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
public class Ship : MonoBehaviour
{
    private Rigidbody rb;

    // The target our ship is trying to travel to
    public Transform target;

    public float acceleration = 5f;

    public GameObject forwardThrustMesh;
    public GameObject backwardThrustMesh;
    public GameObject upthrustMesh;
    public GameObject downThrustMesh;
    public GameObject rightThrustMesh;
    public GameObject leftThrustMesh;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (target != null)
        {
            if (Vector3.Distance(transform.position, target.position) < 0.1f)
            {
                Dampen();
                return;
            }
            MoveTo(target.position);
        }
    }


    void MoveTo(Vector3 target)
    {
        // Get the direction of the target
        Vector3 direction = target - transform.position;

        // Calculate our relative velocity to the target
        Vector3 relativeVelocity = rb.velocity - direction;

        // Calculate the breaking distance to reach the target
        float breakingDistance = 0.5f * (rb.mass * (rb.velocity.magnitude * rb.velocity.magnitude) / acceleration);

        // If we are within the breaking distance, slow down. If our breaking distance is below 0 it is not a valid breaking distance
        if (relativeVelocity.magnitude > 0 && Mathf.Abs(direction.magnitude) < breakingDistance)
        {
            ResolveThrust(-direction.normalized * acceleration);
        }
        // Otherwise, speed up
        else
        {
            ResolveThrust(direction.normalized * acceleration);
        }
    }

    void Dampen()
    {
        float clampedMagnitude = Mathf.Clamp(rb.velocity.magnitude / Time.fixedDeltaTime, -acceleration, acceleration);
        ResolveThrust(-rb.velocity.normalized * clampedMagnitude);
    }

    /// <summary>
    /// Resolves the given thrust vector to the ship's rigidbody and animates accordingly
    /// </summary>
    /// <param name="thrustVector"></param>
    void ResolveThrust(Vector3 thrustVector)
    {
        // Calculate our force needed to make our velocity direction to match the thrust vector direction. Ignore magnitudes
        Vector3 x = thrustVector - rb.velocity;

        Debug.DrawRay(transform.position, thrustVector.normalized, Color.red);
        Debug.DrawRay(transform.position, rb.velocity.normalized, Color.blue);
        Debug.DrawRay(transform.position, x.normalized, Color.magenta);
        Debug.DrawRay(transform.position, (thrustVector + x).normalized, Color.green);

        thrustVector = x;

        rb.AddForce(thrustVector);
        // Deconstruct our thrust vector to be our local forward, right, up vectors
        Vector3 localThrust = transform.InverseTransformDirection(thrustVector);

        if (localThrust.z > -0.0005 && localThrust.z < 0.0005)
        {
            // Animate no thrust
            forwardThrustMesh.SetActive(false);
            backwardThrustMesh.SetActive(false);
        }
        else if (localThrust.z > 0)
        {
            // Animate forward thrust
            backwardThrustMesh.SetActive(true);
            forwardThrustMesh.SetActive(false);
        }
        else
        {
            // Animate backward thrust
            forwardThrustMesh.SetActive(true);
            backwardThrustMesh.SetActive(false);
        }

        if (localThrust.y > -0.0005 && localThrust.y < 0.0005)
        {
            // Animate no thrust
            upthrustMesh.SetActive(false);
            downThrustMesh.SetActive(false);
        }
        else if (localThrust.y > 0)
        {
            // Animate up thrust
            downThrustMesh.SetActive(true);
            upthrustMesh.SetActive(false);
        }
        else
        {
            // Animate down thrust
            upthrustMesh.SetActive(true);
            downThrustMesh.SetActive(false);
        }

        if (localThrust.x > -0.0005 && localThrust.x < 0.0005)
        {
            // Animate no thrust
            leftThrustMesh.SetActive(false);
            rightThrustMesh.SetActive(false);
        }
        else if (localThrust.x > 0)
        {
            // Animate right thrust
            leftThrustMesh.SetActive(true);
            rightThrustMesh.SetActive(false);
        }
        else
        {
            // Animate left thrust
            rightThrustMesh.SetActive(true);
            leftThrustMesh.SetActive(false);
        }

    }
}
