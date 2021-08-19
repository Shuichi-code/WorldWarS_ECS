using Unity.Entities;
using UnityEngine;
using Assets.Scripts.Class;

namespace Assets.Scripts.Systems
{

    public class RemoveTagsSystem : ParallelSystem
    {
        protected override void OnUpdate()
        {
            bool mouseButtonHeld = Input.GetKey(KeyCode.Mouse0);
            var ecb = EcbSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
                .WithAny<HighlightedTag, EnemyCellTag>()
                .ForEach((Entity e, int entityInQueryIndex) =>
                {
                    if (mouseButtonHeld) return;
                    if (HasComponent<HighlightedTag>(e))
                        Tag.RemoveTag<HighlightedTag>(ecb, entityInQueryIndex, e);

                    else if (HasComponent<EnemyCellTag>(e))
                        Tag.RemoveTag<EnemyCellTag>(ecb, entityInQueryIndex, e);
                }).ScheduleParallel();
            EcbSystem.AddJobHandleForProducer(this.Dependency);
        }
    }
}
