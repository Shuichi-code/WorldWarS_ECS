using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct InputDataComponent : IComponentData
{
    public KeyCode leftClick;
}
