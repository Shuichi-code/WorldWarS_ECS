using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Systems.Special_Ability_Systems
{
    public class TagSpecialAbilitySystem : ParallelSystem
    {
        protected override void OnUpdate()
        {
            var ecb = EcbSystem.CreateCommandBuffer();
            var playerEntity = GetPlayerEntity<PlayerTag>();
            var enemyEntity = GetPlayerEntity<EnemyTag>();

            RemoveSpecialAbilityForNazi(playerEntity, enemyEntity);

            if (HasComponent<ChargedAbilityTag>(playerEntity) && HasComponent<ChargedAbilityTag>(enemyEntity)) return;

            TagPlayerWithSpecialAbilityForNazi(ecb, playerEntity, enemyEntity);

            //Debug.Log("Number of Captured pieces: " + entities.CalculateEntityCount().ToString());
            TagPlayerWithSpecialAbilityForRussia(ecb, playerEntity, enemyEntity);
        }

        private void RemoveSpecialAbilityForNazi(Entity playerEntity, Entity enemyEntity)
        {
            //if player's five star general has prisoner, remove player's charged ability
            if (CheckFiveStar<PlayerTag>())
            {
                EntityManager.RemoveComponent<ChargedAbilityTag>(playerEntity);
            }
            else if (CheckFiveStar<EnemyTag>())
            {
                EntityManager.RemoveComponent<ChargedAbilityTag>(enemyEntity);
            }

        }

        private bool CheckFiveStar<T>()
        {
            var playerPiecesQuery = GetEntityQuery(ComponentType.ReadOnly<T>(), ComponentType.ReadOnly<RankComponent>(), ComponentType.ReadOnly<PieceTag>());
            var playerPieceRankArray = playerPiecesQuery.ToComponentDataArray<RankComponent>(Allocator.Temp);
            var playerPieceEntityArray = playerPiecesQuery.ToEntityArray(Allocator.Temp);

            for (var index = 0; index < playerPieceRankArray.Length; index++)
            {
                var rankComponent = playerPieceRankArray[index].Rank;
                if (rankComponent == Piece.FiveStarGeneral && HasComponent<PrisonerTag>(playerPieceEntityArray[index]))
                {
                    return true;
                }
            }

            return false;
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

            //get the pieces of both a player. if player is russia, has no more spies, and has charged ability tag, remove it.
            CheckIfAllSpiesAreDead(ecb, playerEntity, enemyEntity);

            var entities = GetEntityQuery(ComponentType.ReadOnly<CapturedComponent>());
            if (entities.CalculateEntityCount() == 0) return;

            Entities.WithAll<CapturedComponent, PieceTag>().
                //WithAny<PlayerTag, EnemyTag>().
                ForEach((Entity e, int entityInQueryIndex, in ArmyComponent armyComponent) =>
                {
                    //Debug.Log("Adding Russia special ability tag!");
                    ecb.AddComponent(HasComponent<PlayerTag>(e) ? playerEntity : enemyEntity, new SpecialAbilityComponent());
                }).Schedule();
            Dependency.Complete();
        }

        private void CheckIfAllSpiesAreDead(EntityCommandBuffer ecb, Entity playerEntity, Entity enemyEntity)
        {
            //check if he still has spies
            var spyWithBulletsArray = new NativeArray<Entity>(2, Allocator.TempJob) { [0] = Entity.Null, [1] = Entity.Null };
            //Debug.Log("Bullets remaining: " + GetEntityQuery(ComponentType.ReadOnly<BulletComponent>(), ComponentType.ReadOnly<PlayerTag>()).CalculateEntityCount().ToString());

            var checkAliveSpiesJob = Entities.WithAll<PieceTag, BulletComponent>().WithNone<CapturedComponent>().ForEach(
                (Entity pieceEntity, in TeamComponent teamComponent, in RankComponent rankComponent, in ArmyComponent armyComponent) =>
                {
                    if (rankComponent.Rank == Piece.Spy && !HasComponent<PrisonerTag>(pieceEntity) && armyComponent.army == Army.Russia)
                    {
                        if (HasComponent<PlayerTag>(pieceEntity))
                        {
                            spyWithBulletsArray[0] = pieceEntity;
                        }
                        else if (HasComponent<EnemyTag>(pieceEntity))
                        {
                            spyWithBulletsArray[1] = pieceEntity;
                        }
                    }
                }).Schedule(Dependency);
            CompleteDependency();

            //var armyArray = GetComponentDataFromEntity<ArmyComponent>(true);

            Entities.WithAll<TimeComponent>().
                WithAny<PlayerTag, EnemyTag>().
                ForEach((Entity e, in ArmyComponent armyComponent) =>
            {
                if (((HasComponent<PlayerTag>(e) && spyWithBulletsArray[0] == Entity.Null) || (HasComponent<EnemyTag>(e) && spyWithBulletsArray[1] == Entity.Null)) && armyComponent.army == Army.Russia &&
                    HasComponent<ChargedAbilityTag>(e))
                {
                    ecb.RemoveComponent<ChargedAbilityTag>(e);
                    DeactivateChargeAbilityUI(ecb);
                }
            }).Schedule(checkAliveSpiesJob).Complete();

            spyWithBulletsArray.Dispose();
        }

        private static void DeactivateChargeAbilityUI(EntityCommandBuffer ecb)
        {
            var chargedEventEntity = ecb.CreateEntity();
            ecb.AddComponent(chargedEventEntity, new ChargedAbilityEventComponent() { activateUI = false });
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
