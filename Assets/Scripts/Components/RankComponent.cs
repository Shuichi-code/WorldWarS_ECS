using System;
using Assets.Scripts.Class;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct RankComponent : IComponentData
{
    public int Rank;
}
