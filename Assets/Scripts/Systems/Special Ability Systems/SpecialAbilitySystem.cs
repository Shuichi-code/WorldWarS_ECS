using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

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
            #region initializingdata
            var specialAbilityPlayerQuery = GetPlayerEntitiesWithSpecialAbilities();
            if (specialAbilityPlayerQuery.CalculateEntityCount() == 0) return;
            var ecb = GetEcbParallelWriter();
            var specialEntityArray = specialAbilityPlayerQuery.ToEntityArray(Allocator.Temp);
            var armyArray = specialAbilityPlayerQuery.ToComponentDataArray<ArmyComponent>(Allocator.Temp);

            #endregion

            ApplySpecialAbilityToArmy(armyArray, specialEntityArray);
            armyArray.Dispose();
            RemoveSpecialAbilityComponents(ecb);
        }

        private EntityCommandBuffer.ParallelWriter GetEcbParallelWriter()
        {
            return ecbSystem.CreateCommandBuffer().AsParallelWriter();
        }

        private void ApplySpecialAbilityToArmy(NativeArray<ArmyComponent> armyArray, NativeArray<Entity> specialEntityArray)
        {
            for (var index = 0; index < armyArray.Length; index++)
            {
                var army = armyArray[index];
                var playerEntity = specialEntityArray[index];
                switch (army.army)
                {
                    case Army.America:
                        break;
                    case Army.Nazi:
                        EntityManager.AddComponentData(playerEntity, new ChargedAbilityTag());
                        break;
                    case Army.Russia:

                        if (HasComponent<PlayerTag>(playerEntity))
                        {
                            AddBulletToSpy<PlayerTag>(playerEntity);
                        }
                        else if (HasComponent<EnemyTag>(playerEntity))
                        {
                            AddBulletToSpy<EnemyTag>(playerEntity);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private EntityQuery GetPlayerEntitiesWithSpecialAbilities()
        {
            return GetEntityQuery(
                ComponentType.ReadOnly<SpecialAbilityComponent>(),
                ComponentType.ReadOnly<ArmyComponent>(),
                ComponentType.ReadOnly<TeamComponent>());
        }

        private void RemoveSpecialAbilityComponents(EntityCommandBuffer.ParallelWriter ecb)
        {
            Entities.WithAll<SpecialAbilityComponent>()
                .ForEach((Entity e, int entityInQueryIndex) => { ecb.RemoveComponent<SpecialAbilityComponent>(entityInQueryIndex, e); }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(Dependency);
        }

        private void AddBulletToSpy<T>(Entity playerEntity)
        {
            var pieceEntityQuery = GetPieceEntityQuery<T>();
            if (pieceEntityQuery.CalculateEntityCount() == 0) return;
            var pieceEntityArray = pieceEntityQuery.ToEntityArray(Allocator.Temp);
            var pieceRankArray = pieceEntityQuery.ToComponentDataArray<RankComponent>(Allocator.Temp);
            for (var index = 0; index < pieceEntityArray.Length; index++)
            {
                var pieceEntity = pieceEntityArray[index];
                var pieceRank = pieceRankArray[index].Rank;
                if (HasComponent<BulletComponent>(pieceEntity) || pieceRank != Piece.Spy) continue;
                EntityManager.AddComponentData(pieceEntity, new BulletComponent());
                EntityManager.AddComponentData(playerEntity, new ChargedAbilityTag());
                break;
            }
            pieceEntityArray.Dispose();
        }

        private EntityQuery GetPieceEntityQuery<T>()
        {
            return GetEntityQuery(
                ComponentType.ReadOnly<RankComponent>(),
                ComponentType.ReadOnly<TeamComponent>(),
                ComponentType.ReadOnly<PieceTag>(),
                ComponentType.ReadOnly<T>());
        }
    }

    public class ChargeAbilitySystem : SystemBase
    {
        public delegate void ChargeAbilityActivateDelegate(bool activateUI);
        public event ChargeAbilityActivateDelegate ChargedAbilityActiveEvent;
        private EntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            ecbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var gmQuery = GetEntityQuery(ComponentType.ReadOnly<GameManagerComponent>());
            var gm = gmQuery.GetSingleton<GameManagerComponent>();
            var teamToMove = gm.teamToMove;
            var ecb = ecbSystem.CreateCommandBuffer();

            Entities.WithAll<ChargedAbilityTag,PlayerTag>().
                ForEach((Entity e, in TeamComponent teamComponent) =>
                {
                    if (teamToMove == teamComponent.myTeam )
                    {
                        if (HasComponent<ChargeEventFiredTag>(e)) return;
                        ecb.AddComponent(e, new ChargeEventFiredTag());
                        BroadcastChargeAbilityEvent(ecb, true);
                    }
                    else
                    {
                        if (!HasComponent<ChargeEventFiredTag>(e)) return;
                        ecb.RemoveComponent<ChargeEventFiredTag>(e);
                        BroadcastChargeAbilityEvent(ecb, false);
                    }
                }).Schedule();
                this.CompleteDependency();

            InvokeChargeAbilityEventIfAvailable();
        }

        private void InvokeChargeAbilityEventIfAvailable()
        {
            Entities
                .ForEach((Entity e, in ChargedAbilityEventComponent eventComponent) =>
                {
                    ChargedAbilityActiveEvent?.Invoke(eventComponent.activateUI);
                })
                .WithoutBurst().Run();
            EntityManager.DestroyEntity(GetEntityQuery(typeof(ChargedAbilityEventComponent)));
        }

        private static void BroadcastChargeAbilityEvent(EntityCommandBuffer ecb, bool setUIActive)
        {
            var chargedEventEntity = ecb.CreateEntity();
            ecb.AddComponent(chargedEventEntity, new ChargedAbilityEventComponent() {activateUI = setUIActive});
        }
    }
}
