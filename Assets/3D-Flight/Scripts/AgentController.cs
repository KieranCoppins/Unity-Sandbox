using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentController : MonoBehaviour
{

    public Transform target;

    public float maxSteeringAngle = 15;

    public float movementSpeed = 5;

    private Rigidbody rb;

    private Vector3 prevCircleMidPoint;
    private Vector3 circleMidPoint;
    public float radius;
    [Range(1.01f, 1.03f)] public float coeffecient;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        MoveTo(target.position);
    }

    void MoveTo(Vector3 position)
    {
        Vector3 direction = position - transform.position;
        var lookAt = Quaternion.FromToRotation(transform.forward, direction.normalized);

        lookAt.ToAngleAxis(out var angle, out var axis);
        angle = Mathf.Clamp(angle, 0, maxSteeringAngle);


        float dist = movementSpeed * Time.deltaTime;
        // Calculate turning circle
        radius = Mathf.Abs((1 + Time.deltaTime) / Mathf.Tan(maxSteeringAngle * Mathf.Deg2Rad));

        prevCircleMidPoint = circleMidPoint;
        Vector3 maxTurnDirection = Quaternion.AngleAxis(maxSteeringAngle, axis) * transform.forward;
        circleMidPoint = Vector3.Dot(direction, transform.right) > 0 ? transform.position + (Vector3.Cross(maxTurnDirection, transform.up) * -radius) : transform.position + (Vector3.Cross(direction.normalized, transform.up) * radius);

        if (Vector3.Distance(circleMidPoint, position) <= radius)
        {
            // It is inside our turning circle turn the opposite direction
            angle *= -1;
        }

        Quaternion clampedQuat = Quaternion.AngleAxis(angle, axis);

        // Go to it
        if (direction.magnitude > 0.01f)
        {
            transform.position = transform.position + (clampedQuat * transform.forward).normalized * dist;
            transform.forward = Quaternion.AngleAxis(angle * dist, axis) * transform.forward;
        }

        Debug.DrawRay(transform.position, transform.forward, Color.green);
        Debug.DrawRay(transform.position, direction.normalized, Color.red);
        Debug.DrawRay(transform.position, clampedQuat * transform.forward, Color.magenta);

        Debug.DrawLine(prevCircleMidPoint, circleMidPoint, Color.cyan, 2f);
    }

    Bounds CalculateBounds()
    {
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            bounds.Encapsulate(col.bounds);
        }
        return bounds;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(circleMidPoint, radius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(circleMidPoint, .1f);
    }
}
