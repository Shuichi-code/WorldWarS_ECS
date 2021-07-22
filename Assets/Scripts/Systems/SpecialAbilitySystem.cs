using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using System;
using Unity.Collections;
using Unity.Entities;

namespace Assets.Scripts.Systems
{
    public class SpecialAbilitySystem : SystemBase
    {
        private EntityCommandBufferSystem ecbSystem;
        protected override void OnCreate()
        {
            // Find the ECB system once and store it for later usage
            base.OnCreate();
            ecbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var specialAbilityPlayerQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpecialAbilityComponent>(),
                ComponentType.ReadOnly<ArmyComponent>(),
                ComponentType.ReadOnly<TeamComponent>());
            if (specialAbilityPlayerQuery.CalculateEntityCount() == 0) return;
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
            var specialEntityArray = specialAbilityPlayerQuery.ToEntityArray(Allocator.Temp);
            var armyArray = specialAbilityPlayerQuery.ToComponentDataArray<ArmyComponent>(Allocator.Temp);
            var teamArray = specialAbilityPlayerQuery.ToComponentDataArray<TeamComponent>(Allocator.Temp);

            for (var index = 0; index < armyArray.Length; index++)
            {
                var army = armyArray[index];
                switch (army.army)
                {
                    case Army.America:
                        break;
                    case Army.Nazi:
                        break;
                    case Army.Russia:
                        if (HasComponent<PlayerTag>(specialEntityArray[index]))
                        {
                            AddBulletToSpy<PlayerTag>();
                        }
                        else
                        {
                            AddBulletToSpy<EnemyTag>();
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            armyArray.Dispose();
            teamArray.Dispose();
            RemoveSpecialAbilityComponents(ecb);
        }

        private void RemoveSpecialAbilityComponents(EntityCommandBuffer.ParallelWriter ecb)
        {
            Entities.WithAll<SpecialAbilityComponent>()
                .ForEach((Entity e, int entityInQueryIndex) => { ecb.RemoveComponent<SpecialAbilityComponent>(entityInQueryIndex, e); }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(Dependency);
        }

        private void AddBulletToSpy<T>()
        {
            var pieceEntityQuery = GetEntityQuery(
                ComponentType.ReadOnly<RankComponent>(),
                ComponentType.ReadOnly<TeamComponent>(),
                ComponentType.ReadOnly<PieceTag>(),
                ComponentType.ReadOnly<T>());
            if (pieceEntityQuery.CalculateEntityCount() == 0) return;
            var pieceEntityArray = pieceEntityQuery.ToEntityArray(Allocator.Temp);
            var pieceRankArray = pieceEntityQuery.ToComponentDataArray<RankComponent>(Allocator.Temp);
            for (var index = 0; index < pieceEntityArray.Length; index++)
            {
                var pieceEntity = pieceEntityArray[index];
                var pieceRank = pieceRankArray[index].Rank;
                if (HasComponent<BulletComponent>(pieceEntity) || pieceRank != Piece.Spy) continue;
                EntityManager.AddComponentData(pieceEntity, new BulletComponent());
                break;
            }
            pieceEntityArray.Dispose();
        }
    }
}
