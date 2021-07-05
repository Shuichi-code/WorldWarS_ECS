using Assets.Scripts.Class;
using System;
using Unity.Entities;

namespace Assets.Scripts.Components
{
    [Serializable]
    public struct GameFinishedEventComponent : IComponentData
    {
        public Team winningTeam;
    };
}
