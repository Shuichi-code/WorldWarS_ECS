using Assets.Scripts.Class;
using System;
using Unity.Entities;

namespace Assets.Scripts.Components
{
    [Serializable]
    public struct GameManagerComponent : IComponentData
    {
        public Team teamToMove;
        public GameState gameState;
    }
}