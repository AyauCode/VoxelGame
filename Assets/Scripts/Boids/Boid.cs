using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    public LayerMask terrainLayer;
    public float turnSpeed;
    public float speed;
    //public float alignment, cohesion, separation;
    public float viewRadius;

    Vector3 cohesionVelocity, separationVelocity, alignmentVelocity;
    Vector3 velocity;
    float viewRadiusSquared;
    void Start()
    {
        viewRadiusSquared = viewRadius * viewRadius;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 0, 1f, 0.4f);
        Gizmos.DrawSphere(this.transform.position, viewRadius);
        Gizmos.color = new Color(0, 1, 0, 1f);
        Gizmos.DrawLine(this.transform.position, this.transform.position + this.transform.forward.normalized * viewRadius);
    }
    public void UpdateBoid(float alignment, float cohesion, float separation)
    {
        this.velocity += alignmentVelocity * alignment + cohesionVelocity * cohesion + separationVelocity * separation;
        this.velocity = velocity.normalized;

        RaycastHit hit;
        if (Physics.Raycast(new Ray(this.transform.position, this.transform.forward), out hit, viewRadius, terrainLayer))
        {
            velocity = (hit.point - this.transform.position);
            velocity = velocity.normalized;
            velocity *= -1;
        }

        this.transform.forward = Vector3.Slerp(this.transform.forward, this.velocity, turnSpeed);
        this.transform.position += this.transform.forward * speed * Time.deltaTime;
    }
    public void LoopPosition(Bounds cubeBounds)
    {
        Vector3 updatePos = this.transform.position;
        //X LOOP
        if(this.transform.position.x <= cubeBounds.min.x)
        {
            updatePos.x = cubeBounds.max.x;
        }
        else if(this.transform.position.x >= cubeBounds.max.x)
        {
            updatePos.x = cubeBounds.min.x;
        }
        //Y LOOP
        if (this.transform.position.y <= cubeBounds.min.y)
        {
            updatePos.y = cubeBounds.max.y;
        }
        else if (this.transform.position.y >= cubeBounds.max.y)
        {
            updatePos.y = cubeBounds.min.y;
        }
        //Z LOOP
        if (this.transform.position.z <= cubeBounds.min.z)
        {
            updatePos.z = cubeBounds.max.z;
        }
        else if (this.transform.position.z >= cubeBounds.max.z)
        {
            updatePos.z = cubeBounds.min.z;
        }

        this.transform.position = updatePos;
    }
    public void SetVelocity(Vector3 velocity)
    {
        this.velocity = velocity;
    }
    public Vector3 GetDirection()
    {
        return this.velocity;
    }
    public bool IsBoidInView(Vector3 pos)
    {
        return (pos-this.transform.position).sqrMagnitude <= viewRadiusSquared ? true : false;
    }
    public void AlignmentStep(List<Boid> boids)
    {
        alignmentVelocity = Vector3.zero;
        int count = 0;
        foreach (Boid otherBoid in boids)
        {
            if (IsBoidInView(otherBoid.transform.position) && otherBoid != this)
            {
                alignmentVelocity += otherBoid.GetDirection();
                count++;
            }
        }
        if (count > 0)
        {
            alignmentVelocity /= count;
            alignmentVelocity = alignmentVelocity.normalized;
        }
    }
    public void CohesionStep(List<Boid> boids)
    {
        cohesionVelocity = Vector3.zero;
        int count = 0;
        foreach(Boid otherBoid in boids)
        {
            if (IsBoidInView(otherBoid.transform.position) && otherBoid != this)
            {
                cohesionVelocity += otherBoid.transform.position;
                count++;
            }
        }
        if(count > 0)
        {
            cohesionVelocity /= count;
            cohesionVelocity = new Vector3(cohesionVelocity.x - this.transform.position.x, cohesionVelocity.y - this.transform.position.y, cohesionVelocity.z - this.transform.position.z);
            cohesionVelocity = cohesionVelocity.normalized;
        }
    }
    public void SeparationStep(List<Boid> boids)
    {
        separationVelocity = Vector3.zero;
        int count = 0;
        foreach (Boid otherBoid in boids)
        {
            if (IsBoidInView(otherBoid.transform.position) && otherBoid != this)
            {
                separationVelocity += (otherBoid.transform.position - this.transform.position);
                count++;
            }
        }
        if (count > 0)
        {
            separationVelocity /= count;
            separationVelocity *= -1;
            separationVelocity = separationVelocity.normalized;
        }
    }
}
