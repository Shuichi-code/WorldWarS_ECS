using System;
using Unity.Entities;

namespace Assets.Scripts.Components
{
    [Serializable]
    public struct ArbiterComponent : IComponentData
    {
        public Entity attackingPieceEntity;
        public Entity defendingPieceEntity;
        public Entity battlegroundCellEntity;
        public Entity originalCellEntity;
    }
}
