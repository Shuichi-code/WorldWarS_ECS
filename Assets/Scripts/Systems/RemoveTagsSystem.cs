using Unity.Entities;
using UnityEngine;
using Assets.Scripts.Class;

namespace Assets.Scripts.Systems
{
    [UpdateAfter(typeof(ArbiterCheckingSystem))]
    public class RemoveTagsSystem : SystemBase
    {
        EntityCommandBufferSystem ecbSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            // Find the ECB system once and store it for later usage
            ecbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            bool mouseButtonHeld = Input.GetKey(KeyCode.Mouse0);
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
                .WithAny<HighlightedTag, EnemyCellTag>()
                .ForEach((Entity e, int entityInQueryIndex) =>
                {
                    if (mouseButtonHeld) return;
                    if (HasComponent<HighlightedTag>(e))
                        Tag.RemoveHighlightedTag(ecb, entityInQueryIndex, e);

                    else if (HasComponent<EnemyCellTag>(e))
                        Tag.RemoveEnemyCellTag(ecb, entityInQueryIndex, e);
                }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(this.Dependency);
        }
    }
}
