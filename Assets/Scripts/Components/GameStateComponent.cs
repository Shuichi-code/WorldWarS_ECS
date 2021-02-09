using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct GameStateComponent : IComponentData
{
    public enum State
    {
        WaitingToStart,
        Playing,
        Dead
    }

    public State state;
}
