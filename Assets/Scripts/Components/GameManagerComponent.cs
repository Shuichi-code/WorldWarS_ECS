using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct GameManagerComponent : IComponentData
{
    public enum State
    {
        WaitingToStart,
        Playing,
        Dead
    }

    public Color teamToMove;

    public bool isDragging;

    public State state;
}
