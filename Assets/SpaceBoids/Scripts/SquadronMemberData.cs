using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
public struct SquadronMemberData
{
    public Matrix4x4 mat;
    public Vector3 velocity;

    public int team;
    public int targetId;
    public int targedByCount;
    public int dead;

    public float lastShotTime;

    public int id;

    public static int Size { get => (sizeof(float) * 16) + (sizeof(float) * 3) + (sizeof(int) * 5) + sizeof(float); }
}

public struct ProjectileData
{
    public Matrix4x4 mat;
    public Vector3 velocity;

    public int casterId;
    public int valid;
    public float spawnTime;

    public static int Size { get => (sizeof(float) * 16) + (sizeof(float) * 3) + (sizeof(int) * 2) + sizeof(float); }
}
