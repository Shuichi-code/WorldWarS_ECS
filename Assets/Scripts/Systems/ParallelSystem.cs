using Unity.Entities;

namespace Assets.Scripts.Systems
{
    public abstract class ParallelSystem : SystemBase
    {

        public EntityCommandBufferSystem EcbSystem { get; private set; }
        protected override void OnStartRunning()
        {
            EcbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {

        }
    }
}