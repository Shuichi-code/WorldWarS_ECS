using System;
using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class ActivateAbilitySystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            SystemManager.SetSystemStatus<ActivateAbilitySystem>(false);
        }
        protected override void OnUpdate()
        {
            //get the player with the charge ability that has an army
            var chargedPlayerEntity = GetEntityQuery(ComponentType.ReadOnly<ChargedAbilityTag>(), ComponentType.ReadOnly<ArmyComponent>()).GetSingletonEntity();
            var playerArmy = GetComponent<ArmyComponent>(chargedPlayerEntity);
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
            //switch statement
            switch (playerArmy.army)
            {
                case Army.America:
                    break;
                case Army.Nazi:
                    break;
                case Army.Russia:
                    ActivateOneShotOneKill(ecb);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        private void ActivateOneShotOneKill(EntityCommandBuffer.ParallelWriter ecb)
        {
            //highlight all enemy pieces in red
            HighlightEnemyPieces(ecb);
            //disable the pickupsystem
            SystemManager.SetPickupSystems(false);
            //get the piece that the player clicks

            //create special arbiter for the fight, if player wins, the piece is destroyed, if he loses the spy is revealed.

            //remove one bullet from the spy piece

            //if there are no more bullet in the player's army, remove the charge ability tag

        }

        private void HighlightEnemyPieces(EntityCommandBuffer.ParallelWriter ecb)
        {
            Entities.WithAll<EnemyTag, PieceTag>().ForEach((Entity e, int entityInQueryIndex) =>
            {
                Tag.AddTag<EnemyCellTag>(ecb, entityInQueryIndex, e);
            }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
