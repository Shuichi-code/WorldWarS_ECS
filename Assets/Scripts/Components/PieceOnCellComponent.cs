using System;
using Assets.Scripts.Class;
using Unity.Entities;

namespace Assets.Scripts.Components
{
    [Serializable]
    public struct PieceOnCellComponent : IComponentData
    {
        public Entity PieceEntity { get; set; }
        public Team PieceTeam { get; set; }
        public int PieceRank { get; set; }
    }
}
