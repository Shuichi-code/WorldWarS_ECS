using Assets.Scripts.Class;
using System;
using Unity.Entities;

namespace Assets.Scripts.Components
{
    [Serializable]
    public struct HomeCellComponent : IComponentData
    {
        public Team homeTeam;
    };
}
