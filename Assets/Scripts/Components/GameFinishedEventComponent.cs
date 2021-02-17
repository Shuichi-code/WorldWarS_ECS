using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct GameFinishedEventComponent : IComponentData
{
    public Color winningTeamColor;
};
