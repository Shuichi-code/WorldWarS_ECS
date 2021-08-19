using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Monobehaviours.Managers;
using Assets.Scripts.Tags;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.Scripts.Systems
{
    public class CapturedSystem : ParallelSystem
    {
        protected override void OnUpdate()
        {
            var ecb = EcbSystem.CreateCommandBuffer().AsParallelWriter();

            Entities
                .WithAll<CapturedComponent>()
                .ForEach((Entity e, int entityInQueryIndex, ref Translation translation) =>
                {
                    translation.Value = GameConstants.PrisonCoordinates;
                    ecb.RemoveComponent<CapturedComponent>(entityInQueryIndex, e);
                    ecb.AddComponent<PrisonerTag>(entityInQueryIndex, e);
                }).ScheduleParallel();
            EcbSystem.AddJobHandleForProducer(this.Dependency);
        }
    }
}
