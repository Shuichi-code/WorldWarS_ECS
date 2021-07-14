using System;
using Assets.Scripts.Class;
using Unity.Entities;

namespace Assets.Scripts.Components
{
    [Serializable]
    public struct ArmyComponent : IComponentData
    {
        public Army army;
    }
}
