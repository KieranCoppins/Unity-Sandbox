using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

public class SquadronManager : MonoBehaviour
{
    const int threadGroupSize = 256;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 60f;
    [SerializeField] private int spawnCount = 500;
    [SerializeField] private int maxProjectilesCount = 5000;


    [Header("Squadron Settings")]
    [SerializeField] public float minSpeed = 10f;
    [SerializeField] public float maxSpeed = 30f;
    [SerializeField] public float rotationSpeed = 5f;


    [Header("Projectile Settings")]
    [SerializeField] public float bulletSpeed = 40f;
    [SerializeField] public float rateOfFire = 1;
    [SerializeField] public float projectileAliveTime = 2;


    [Header("Target Picking Weights")]
    [SerializeField, Tooltip("Weight for distance from ship")] public float targetDistanceWeight = 1;
    [SerializeField, Tooltip("Weight for direction of other ship relative to ship")] public float targetDotWeight = 1.5f;


    [Header("Steering Weights")]
    [SerializeField, Tooltip("Weight for steering away from nearby ships")] public float collisionAvoidanceWeight = 2f;
    [SerializeField, Tooltip("How close to ships need to be to steer away from them")] public float collisionAvoidanceRadius = 3f;
    [SerializeField, Tooltip("Weight for steering towards their target to obtain a firing solution")] public float targetSteerWeight = 1.6f;
    [SerializeField, Tooltip("Weight for steering to avoid an enemy targeting them")] public float enemyEvadeWeight = 1f;
    [SerializeField, Tooltip("What is our range to look for enemies")] public float enemyEvadeDistance = 15f;
    [SerializeField, Tooltip("Weight based on distance from origin to make ships not fly too far away")] public float boundsSteeringWeight = 0.05f;


    [Header("Rendering")]
    public Mesh shipMesh;
    public Material shipMaterial;
    public Mesh projectileMesh;
    public Material projectileMaterial;

    [Header("Compute Shaders")]
    public ComputeShader compute;
    private GPUSort GPUSort;
    private ProjectileGPUSort ProjectileGPUSort;
    private ShipGPUSort ShipGPUSort;

    // Buffers
    private GraphicsBuffer squadMemberBuffer;
    private GraphicsBuffer projectileBuffer;
    private ComputeBuffer lastIndicesBuffer;
    private int[] lastIndices;
    private ComputeBuffer spatialIndiciesBuffer;
    private ComputeBuffer spatialOffsetsBuffer;
    private GraphicsBuffer shipRenderArgsBuffer;
    private GraphicsBuffer projectileRenderArgsBuffer;
    private RenderParams shipRenderParams;
    private RenderParams projectileRenderParams;
    private ComputeBuffer originalIndicesBuffer;


    // Start is called before the first frame update
    void Start()
    {
        SetBuffers();
    }

    public void Update()
    {
        ComputeShips();
    }

    private void SetBuffers()
    {
        // Set squadron buffer with initial values
        squadMemberBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, spawnCount, SquadronMemberData.Size);
        var squadMemberData = new SquadronMemberData[spawnCount];
        uint[] originalIndices = new uint[squadMemberBuffer.count];
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 dir = Random.insideUnitSphere;
            Vector3 pos = transform.position + (Random.insideUnitSphere * spawnRadius);
            squadMemberData[i] = new SquadronMemberData
            {
                mat = Matrix4x4.TRS(pos, Quaternion.LookRotation(dir), Vector3.one),
                velocity = dir * 4f,

                team = (i % 2) + 1,
                targetId = 0,
                targedByCount = 0,
                dead = 0,
                lastShotTime = 0,
                id = i,
            };

            originalIndices[i] = (uint)i;
        }
        squadMemberBuffer.SetData(squadMemberData);


        projectileBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxProjectilesCount, ProjectileData.Size);
        var projectileData = new ProjectileData[maxProjectilesCount];
        projectileBuffer.SetData(projectileData);

        compute.SetBuffer(0, "members", squadMemberBuffer);
        compute.SetBuffer(1, "members", squadMemberBuffer);
        compute.SetBuffer(2, "members", squadMemberBuffer);
        shipMaterial.SetBuffer("members", squadMemberBuffer);
        projectileMaterial.SetBuffer("data", projectileBuffer);

        compute.SetBuffer(0, "projectiles", projectileBuffer);
        compute.SetBuffer(1, "projectiles", projectileBuffer);

        lastIndices = new int[] { spawnCount - 1, 0 };
        lastIndicesBuffer = new ComputeBuffer(2, sizeof(int));
        lastIndicesBuffer.SetData(lastIndices);
        compute.SetBuffer(0, "lastIndices", lastIndicesBuffer);

        // Initialise Spatial Buffers
        spatialIndiciesBuffer = new ComputeBuffer(spawnCount, sizeof(uint) * 3);
        spatialOffsetsBuffer = new ComputeBuffer(spawnCount, sizeof(uint));

        compute.SetBuffer(0, "SpatialIndices", spatialIndiciesBuffer);
        compute.SetBuffer(0, "SpatialOffsets", spatialOffsetsBuffer);
        compute.SetBuffer(2, "SpatialIndices", spatialIndiciesBuffer);
        compute.SetBuffer(2, "SpatialOffsets", spatialOffsetsBuffer);

        GPUSort = new GPUSort();
        GPUSort.SetBuffers(spatialIndiciesBuffer, spatialOffsetsBuffer);

        ProjectileGPUSort = new ProjectileGPUSort();
        ProjectileGPUSort.SetBuffers(projectileBuffer, lastIndicesBuffer);

        originalIndicesBuffer = new ComputeBuffer(squadMemberBuffer.count, sizeof(uint));
        originalIndicesBuffer.SetData(originalIndices);

        compute.SetBuffer(0, "originalIndices", originalIndicesBuffer);
        compute.SetBuffer(1, "originalIndices", originalIndicesBuffer);

        ShipGPUSort = new ShipGPUSort();
        ShipGPUSort.SetBuffers(squadMemberBuffer, lastIndicesBuffer, originalIndicesBuffer);

        // Set graphics params
        shipRenderParams = new RenderParams(shipMaterial)
        {
            worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000)
        };

        projectileRenderParams = new RenderParams(projectileMaterial)
        {
            worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000)
        };

        shipRenderArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        projectileRenderArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);

        // Set Constant Compute Values
        compute.SetInt("squadronMembersCount", spawnCount);
        compute.SetFloat("enemyEvadeDistance", enemyEvadeDistance);
        compute.SetFloat("collisionAvoidanceRadius", collisionAvoidanceRadius);
        compute.SetFloat("bulletSpeed", bulletSpeed);
        compute.SetFloat("targetDistanceWeight", targetDistanceWeight);
        compute.SetFloat("targetDotWeight", targetDotWeight);
        compute.SetFloat("collisionAvoidanceWeight", collisionAvoidanceWeight);
        compute.SetFloat("targetSteerWeight", targetSteerWeight);
        compute.SetFloat("enemyEvadeWeight", enemyEvadeWeight);
        compute.SetFloat("boundsSteeringWeight", boundsSteeringWeight);
        compute.SetFloat("minSpeed", minSpeed);
        compute.SetFloat("maxSpeed", maxSpeed);
        compute.SetFloat("rotationSpeed", rotationSpeed);
        compute.SetFloat("shipRateOfFire", rateOfFire);
        compute.SetFloat("projectileAliveTime", projectileAliveTime);
    }

    private void ComputeShips()
    {
        // Set frame constants
        compute.SetFloat("deltaTime", Time.deltaTime);
        compute.SetFloat("time", Time.time);
        int squadronThreadGroups = Mathf.CeilToInt(spawnCount / (float)threadGroupSize);
        int projectileThreadGroups = Mathf.CeilToInt(maxProjectilesCount / (float)threadGroupSize);

        // Handle projectiles
        compute.Dispatch(1, projectileThreadGroups, 1, 1);

        // Sort projectiles
        ProjectileGPUSort.Sort();

        // Sort members
        ShipGPUSort.Sort();

        // Spatial sorts
        compute.Dispatch(2, squadronThreadGroups, 1, 1);
        GPUSort.SortAndCalculateOffsets();

        // Handle members
        compute.Dispatch(0, squadronThreadGroups, 1, 1);

        // -------------------------------------
        // Rendering
        // -------------------------------------
        lastIndicesBuffer.GetData(lastIndices);

        var shipCommandData = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        shipCommandData[0] = new GraphicsBuffer.IndirectDrawIndexedArgs
        {
            indexCountPerInstance = shipMesh.GetIndexCount(0),
            instanceCount = (uint)(lastIndices[0] + 1),
            baseVertexIndex = shipMesh.GetBaseVertex(0),
            startIndex = shipMesh.GetIndexStart(0),
        };
        shipRenderArgsBuffer.SetData(shipCommandData);
        Graphics.RenderMeshIndirect(shipRenderParams, shipMesh, shipRenderArgsBuffer);

        var projectileCommandData = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        projectileCommandData[0] = new GraphicsBuffer.IndirectDrawIndexedArgs
        {
            indexCountPerInstance = projectileMesh.GetIndexCount(0),
            instanceCount = (uint)(lastIndices[1] + 1),
            baseVertexIndex = projectileMesh.GetBaseVertex(0),
            startIndex = projectileMesh.GetIndexStart(0),
        };
        projectileRenderArgsBuffer.SetData(projectileCommandData);
        Graphics.RenderMeshIndirect(projectileRenderParams, projectileMesh, projectileRenderArgsBuffer);
    }
    private void OnDisable()
    {
        squadMemberBuffer?.Release();
        projectileBuffer?.Release();
        lastIndicesBuffer?.Release();
        spatialIndiciesBuffer?.Release();
        spatialOffsetsBuffer?.Release();
        shipRenderArgsBuffer?.Release();
        projectileRenderArgsBuffer?.Release();
        originalIndicesBuffer?.Release();
    }
}
