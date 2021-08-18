using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using Unity.Collections;
using Unity.Entities;

namespace Assets.Scripts.Systems.Special_Ability_Systems.ArmySystems
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
            var ecbParallel = ecbSystem.CreateCommandBuffer().AsParallelWriter();
            var playerEntity = GetPlayerEntity<PlayerTag>();
            var enemyEntity = GetPlayerEntity<EnemyTag>();

            RemoveSpecialAbilityForNazi(playerEntity, enemyEntity);

            if (HasComponent<ChargedAbilityTag>(playerEntity) && HasComponent<ChargedAbilityTag>(enemyEntity)) return;

            TagPlayerWithSpecialAbilityForNazi(ecb, playerEntity, enemyEntity);


            var entities = GetEntityQuery(ComponentType.ReadOnly<CapturedComponent>());
            if (entities.CalculateEntityCount() == 0) return;
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
