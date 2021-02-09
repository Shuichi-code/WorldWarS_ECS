using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public class MoveDataComponent : IComponentData
{
    public Vector3 movement;
}

