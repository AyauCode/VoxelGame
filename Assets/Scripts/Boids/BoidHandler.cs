using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidHandler : MonoBehaviour
{
    public GameObject boundingBox;
    Bounds boidBounds;
    List<Boid> boids = new List<Boid>();
    public GameObject boidPrefab;
    public int boidCount;

    public float alignment, cohesion, separation;
    private void Start()
    {
        InitBoids();
    }
    private void InitBoids()
    {
        boidBounds = boundingBox.GetComponent<MeshRenderer>().bounds;
        for(int i = 0; i < boidCount; i++)
        {
            GameObject boidObject = Instantiate(boidPrefab);
            boidObject.transform.position = new Vector3(boundingBox.transform.position.x + (boidBounds.size.x * UnityEngine.Random.value - boidBounds.size.x/2f), boundingBox.transform.position.y + (boidBounds.size.y * UnityEngine.Random.value - boidBounds.size.y / 2f), boundingBox.transform.position.z + (boidBounds.size.z * UnityEngine.Random.value - boidBounds.size.z / 2f));
            boidObject.transform.SetParent(this.transform, true);

            boidObject.GetComponent<Boid>().SetVelocity(new Vector3(UnityEngine.Random.value * 2 - 1, UnityEngine.Random.value * 2 - 1, UnityEngine.Random.value * 2 - 1).normalized);

            boids.Add(boidObject.GetComponent<Boid>());
        }
    }
    public void ResetBoids()
    {
        foreach(Boid boid in boids)
        {
            Destroy(boid.gameObject);
        }
        boids.Clear();
        InitBoids();
    }
    void Update()
    {
        foreach(Boid b in boids)
        {
            b.AlignmentStep(boids);
            b.CohesionStep(boids);
            b.SeparationStep(boids);

            b.UpdateBoid(alignment,cohesion,separation);
        }
    }
}
