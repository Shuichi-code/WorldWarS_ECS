using Assets.Scripts.Components;
using Assets.Scripts.Monobehaviours.Managers;
using Assets.Scripts.Systems.Special_Ability_Systems;
using Assets.Scripts.Tags;
using Unity.Entities;
using Unity.Transforms;

namespace Assets.Scripts.Systems
{
    [UpdateAfter(typeof(ActivateAbilitySystem))]
    public class CapturedSystem : ParallelSystem
    {
        protected override void OnUpdate()
        {
            var ecb = EcbSystem.CreateCommandBuffer();

            Entities
                .WithAll<CapturedComponent>()
                .ForEach((Entity e, int entityInQueryIndex, ref Translation translation, ref OriginalLocationComponent originalLocationComponent, in RankComponent rankComponent) =>
                {
                    translation.Value = GameConstants.PrisonCoordinates;
                    originalLocationComponent.originalLocation = translation.Value;
                    ecb.RemoveComponent<CapturedComponent>(e);
                    ecb.AddComponent<PrisonerTag>(e);
                    //Debug.Log("Removing capture component for rank");
                }).Schedule();
            CompleteDependency();
            //EcbSystem.AddJobHandleForProducer(this.Dependency);
        }
    }
}
