using Assets.Scripts.Class;
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.Components
{
    [Serializable]
    public struct PieceComponent : IComponentData
    {
        public int pieceRank;
        public Team team;
        public float3 originalCellPosition;
    }
}
