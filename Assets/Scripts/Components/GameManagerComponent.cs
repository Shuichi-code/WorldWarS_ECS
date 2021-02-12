using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct GameManagerComponent : IComponentData
{
    public enum State
    {
        WaitingToStart,
        Playing,
        Dead
    }

    public bool isDragging;

    public State state;
}
