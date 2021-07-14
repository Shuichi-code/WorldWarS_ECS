using Assets.Scripts.Class;
using System;
using Unity.Entities;

namespace Assets.Scripts.Components
{
    [Serializable]
    public struct CountdownEventComponent : IComponentData
    {
        public Team winningTeam;
        public float Time;
    };
}
