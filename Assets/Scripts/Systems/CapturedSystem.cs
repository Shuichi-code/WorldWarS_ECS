using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Monobehaviours.Managers;
using Assets.Scripts.Tags;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.Scripts.Systems
{
    public class CapturedSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem ecbSystem;

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
                .ForEach((Entity e, int entityInQueryIndex, ref Translation translation) =>
                {
                    translation.Value = GameConstants.PrisonCoordinates;
                    ecb.RemoveComponent<CapturedComponent>(entityInQueryIndex, e);
                    ecb.AddComponent<PrisonerTag>(entityInQueryIndex, e);
                }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(this.Dependency);
        }
    }
}
