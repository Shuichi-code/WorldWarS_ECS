using System;
using Assets.Scripts.Class;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct TeamComponent : IComponentData
{
    public Team myTeam;
}
