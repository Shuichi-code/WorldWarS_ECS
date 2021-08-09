using System.Text.RegularExpressions;
using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor.U2D.Path.GUIFramework;
using UnityEngine;

namespace Assets.Scripts.Systems.ArmySystems
{
    public class TagSpecialAbilitySystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            // Find the ECB system once and store it for later usage
            base.OnCreate();
            ecbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var ecb = ecbSystem.CreateCommandBuffer();
            var playerEntity = GetPlayerEntity<PlayerTag>();
            var enemyEntity = GetPlayerEntity<EnemyTag>();

            if (HasComponent<ChargedAbilityTag>(playerEntity) && HasComponent<ChargedAbilityTag>(enemyEntity)) return;

            TagPlayerWithSpecialAbilityForNazi(ecb, playerEntity, enemyEntity);

            var entities = GetEntityQuery(ComponentType.ReadOnly<CapturedComponent>());
            if (entities.CalculateEntityCount() == 0) return;
            TagPlayerWithSpecialAbilityForRussia(ecb, playerEntity, enemyEntity);
        }

        private void TagPlayerWithSpecialAbilityForNazi(EntityCommandBuffer ecb, Entity playerEntity, Entity enemyEntity)
        {
            Entities.WithAll<PieceTag>().ForEach(
                (Entity e, in ArmyComponent armyComponent, in RankComponent rankComponent) =>
                {
                    if (armyComponent.army != Army.Nazi) return;
                    if (rankComponent.Rank != Piece.FiveStarGeneral) return;
                    if (!HasComponent<PrisonerTag>(e))
                    {
                        ecb.AddComponent<ChargedFiveStarGeneralTag>(e);
                        ecb.AddComponent(HasComponent<PlayerTag>(e) ? playerEntity : enemyEntity,
                            new SpecialAbilityComponent());
                    }
                    else
                    {
                        if (!HasComponent<ChargedAbilityTag>(playerEntity) &&
                            !HasComponent<ChargedAbilityTag>(enemyEntity)) return;
                        ecb.RemoveComponent<ChargedFiveStarGeneralTag>(e);
                        ecb.RemoveComponent<ChargedAbilityTag>(HasComponent<PlayerTag>(e) ? playerEntity : enemyEntity);
                    }

                }).Schedule();
            Dependency.Complete();

            //if player no longer has five star general in prison but has charged ability component, remove charge ability component
        }

        private void TagPlayerWithSpecialAbilityForRussia(EntityCommandBuffer ecb, Entity playerEntity, Entity enemyEntity)
        {
            Entities.WithAll<CapturedComponent>().WithAny<PlayerTag, EnemyTag>().ForEach(
                (Entity e, in ArmyComponent armyComponent) =>
                {
                    if (armyComponent.army != Army.Russia) return;
                    ecb.AddComponent(HasComponent<PlayerTag>(e) ? playerEntity : enemyEntity, new SpecialAbilityComponent());
                }).Schedule();
            Dependency.Complete();
        }

        private Entity GetPlayerEntity<T>()
        {
            return GetPlayerEntityQuery<T>().GetSingletonEntity();
        }

        private EntityQuery GetPlayerEntityQuery<T>()
        {
            return GetEntityQuery(ComponentType.ReadOnly<T>(),
                ComponentType.ReadOnly<TimeComponent>(), ComponentType.ReadOnly<TeamComponent>(), ComponentType.ReadOnly<ArmyComponent>());
        }
        private T GetPlayerComponent<T>() where T : struct, IComponentData
        {
            return GetPlayerEntityQuery<PlayerTag>().GetSingleton<T>();
        }
    }
}
