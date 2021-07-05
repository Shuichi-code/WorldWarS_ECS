using Assets.Scripts.Components;
using Unity.Entities;

namespace Assets.Scripts.Systems
{
    public class CapturedSystem : SystemBase
    {
        EndSimulationEntityCommandBufferSystem ecbSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            // Find the ECB system once and store it for later usage
            ecbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();

            Entities
                .WithAll<CapturedComponent>()
                .ForEach((Entity e, int entityInQueryIndex) => {
                    ecb.DestroyEntity(entityInQueryIndex, e);

                    //TODO: develop capture algorithm that puts the pieces on their respective player's side
                }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(this.Dependency);
        }
    }
}
