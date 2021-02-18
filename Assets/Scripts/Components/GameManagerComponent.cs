using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public enum State
{
    WaitingToStart,
    Playing,
    Dead
}

[Serializable]
public struct GameManagerComponent : IComponentData
{
    public Color teamToMove;

    public bool isDragging;

    public State state;
}
