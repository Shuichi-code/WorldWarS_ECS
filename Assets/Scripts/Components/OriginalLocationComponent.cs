using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct OriginalLocationComponent : IComponentData
{
    public float3 originalLocation;
}
