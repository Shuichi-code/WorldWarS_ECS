using System;
using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Monobehaviours.Managers;
using Assets.Scripts.Systems.Special_Ability_Systems;
using Assets.Scripts.Tags;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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
                .ForEach((Entity e, int entityInQueryIndex, ref Translation translation, in RankComponent rankComponent) =>
                {
                    translation.Value = GameConstants.PrisonCoordinates;
                    ecb.RemoveComponent<CapturedComponent>(e);
                    ecb.AddComponent<PrisonerTag>(e);
                    Debug.Log("Removing capture component for rank");
                }).Run();
            CompleteDependency();
            //EcbSystem.AddJobHandleForProducer(this.Dependency);
        }
    }
}
