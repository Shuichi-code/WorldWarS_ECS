using System;
using Unity.Entities;

namespace Assets.Scripts.Components
{
    [Serializable]
    public struct ChargedAbilityEventComponent : IComponentData
    {
        public bool activateUI;
    }
}
