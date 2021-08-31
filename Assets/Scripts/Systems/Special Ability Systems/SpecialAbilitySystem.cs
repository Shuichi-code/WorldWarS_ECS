using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using System;
using Unity.Collections;
using Unity.Entities;

namespace Assets.Scripts.Systems
{
    public class SpecialAbilitySystem : ParallelSystem
    {
        protected override void OnUpdate()
        {
            #region initializingdata
            var specialAbilityPlayerQuery = GetPlayerEntitiesWithSpecialAbilities();
            if (specialAbilityPlayerQuery.CalculateEntityCount() == 0) return;
            var ecb = EcbSystem.CreateCommandBuffer();
            var playerEntitiesWithSpecialAbilitiesArray = specialAbilityPlayerQuery.ToEntityArray(Allocator.Temp);
            var armyArray = specialAbilityPlayerQuery.ToComponentDataArray<ArmyComponent>(Allocator.Temp);

            #endregion

            ApplySpecialAbilityToArmy(armyArray, playerEntitiesWithSpecialAbilitiesArray);
            armyArray.Dispose();
            RemoveSpecialAbilityComponents(ecb);
        }

        private void ApplySpecialAbilityToArmy(NativeArray<ArmyComponent> armyArray, NativeArray<Entity> playerEntitiesWithSpecialAbilitiesArray)
        {
            for (var index = 0; index < armyArray.Length; index++)
            {
                var army = armyArray[index];
                var playerEntity = playerEntitiesWithSpecialAbilitiesArray[index];
                switch (army.army)
                {
                    case Army.America:
                        break;
                    case Army.Nazi:
                        EntityManager.AddComponentData(playerEntity, new ChargedAbilityTag());
                        break;
                    case Army.Russia:
                        AddBulletToSpy(playerEntity);
                        //if (HasComponent<PlayerTag>(playerEntity))
                        //{
                        //    AddBulletToSpy<PlayerTag>(playerEntity);
                        //}
                        //else if (HasComponent<EnemyTag>(playerEntity))
                        //{
                        //    AddBulletToSpy<EnemyTag>(playerEntity);
                        //}

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

        private void RemoveSpecialAbilityComponents(EntityCommandBuffer ecb)
        {
            Entities.WithAll<SpecialAbilityComponent>()
                .ForEach((Entity e, int entityInQueryIndex) =>
                {
                    ecb.RemoveComponent<SpecialAbilityComponent>(e);
                }).Schedule();
            CompleteDependency();
            //EcbSystem.AddJobHandleForProducer(Dependency);
        }

        private void AddBulletToSpy(Entity playerEntity)
        {
            var ecb = EcbSystem.CreateCommandBuffer();
            var spyToAddBulletArray = new NativeArray<Entity>(1, Allocator.TempJob) {[0] = Entity.Null};
            spyToAddBulletArray = GetSpyToAddBullet(playerEntity, spyToAddBulletArray, ecb);

            if (spyToAddBulletArray[0] != Entity.Null)
            {
                EntityManager.AddComponent<BulletComponent>(spyToAddBulletArray[0]);
            }
            spyToAddBulletArray.Dispose();
        }

        private NativeArray<Entity> GetSpyToAddBullet(Entity playerEntity, NativeArray<Entity> spyToAddBulletArray, EntityCommandBuffer ecb)
        {

            Entities.WithAll<PieceTag>().WithNone<PrisonerTag, BulletComponent>().ForEach(
                (Entity pieceEntity, in RankComponent pieceRankComponent) =>
                {
                    if (pieceRankComponent.Rank != Piece.Spy ||
                        (((!HasComponent<PlayerTag>(playerEntity) || !HasComponent<PlayerTag>(pieceEntity))) &&
                         (!HasComponent<EnemyTag>(playerEntity) || !HasComponent<EnemyTag>(pieceEntity)))) return;
                    spyToAddBulletArray[0] = pieceEntity;
                    if (!HasComponent<ChargedAbilityTag>(playerEntity))
                    {
                        ecb.AddComponent<ChargedAbilityTag>(playerEntity);
                    }
                }).Schedule();
            CompleteDependency();
            return spyToAddBulletArray;
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
}
