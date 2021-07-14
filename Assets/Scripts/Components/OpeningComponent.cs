using System;
using Assets.Scripts.Class;
using Unity.Collections;
using Unity.Entities;

namespace Assets.Scripts.Components
{
    [Serializable]
    public struct OpeningComponent : IComponentData
    {
        public FixedString32 chosenOpening;
    }
}
