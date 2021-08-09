using System;
using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Scripts.Systems.Special_Ability_Systems
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
            var chargedPlayerEntity = GetEntityQuery(ComponentType.ReadOnly<ChargedAbilityTag>(), ComponentType.ReadOnly<ArmyComponent>(), ComponentType.ReadOnly<PlayerTag>()).GetSingletonEntity();
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

        private void ActivateOneShotOneKill(EntityCommandBuffer.ParallelWriter ecbParallelWriter)
        {
            var roundedWorldPos = Location.GetRoundedMousePosition();
            var mouseButtonPressed = Input.GetMouseButtonDown(0);
            //highlight all enemy pieces in red
            var enemyCellQuery = GetEntityQuery(ComponentType.ReadOnly<EnemyCellTag>());
            if (enemyCellQuery.CalculateEntityCount() == 0)
                HighlightEnemyPieces(ecbParallelWriter);

            SystemManager.SetPickupSystems(false);

            HighlightClickedEntities(roundedWorldPos, mouseButtonPressed, ecbParallelWriter);


            var fightingEntitiesQuery = GetEntityQuery(ComponentType.ReadOnly<HighlightedTag>(), ComponentType.ReadOnly<PieceTag>());
            if (fightingEntitiesQuery.CalculateEntityCount() != 2) return;
            var spyQuery = GetHighlightedPieceQuery<PlayerTag>();
            var targetQuery = GetHighlightedPieceQuery<EnemyTag>();
            var spyEntity = spyQuery.GetSingletonEntity();
            var targetEntity = targetQuery.GetSingletonEntity();
            var targetRank = targetQuery.GetSingleton<RankComponent>().Rank;
            var playerQuery = GetEntityQuery(ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.ReadOnly<TimeComponent>(), ComponentType.ReadOnly<TeamComponent>());

            var fightResult = FightCalculator.DetermineFightResult(Piece.Spy, targetRank);
            switch (fightResult)
            {
                case FightResult.AttackerWins:
                    //destroy the target
                    EntityManager.AddComponent<CapturedComponent>(targetEntity);
                    //remove the pieceoncellcomponent where the target was standing
                    RemovePieceOnCellComponentUnderEntity(ecbParallelWriter, targetEntity);
                    break;
                case FightResult.DefenderWins:
                    EntityManager.AddComponent<RevealedTag>(spyEntity);
                    break;
                case FightResult.BothLose:
                    EntityManager.AddComponent<RevealedTag>(spyEntity);
                    EntityManager.AddComponent<RevealedTag>(targetEntity);
                    break;
                case FightResult.FlagDestroyed:
                    var ecb = ecbSystem.CreateCommandBuffer();
                    var gameFinishEventArchetype = EntityManager.CreateArchetype(typeof(GameFinishedEventComponent));

                    var playerTeam = playerQuery.GetSingleton<TeamComponent>()
                        .myTeam;
                    ArbiterCheckingSystem.DeclareWinner(ecb, gameFinishEventArchetype, playerTeam);
                    break;
                case FightResult.NoFight:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            //remove one bullet from the spy piece
            EntityManager.RemoveComponent<BulletComponent>(spyEntity);
            EntityManager.RemoveComponent<ChargeEventFiredTag>(playerQuery.GetSingletonEntity());
            //if there are no more bullet in the player's army, remove the charge ability tag
            CheckBullets<PlayerTag>();

            RestoreNormalSystems();
            ChangeTurn(spyQuery);
        }

        private void CheckBullets<T>()
        {
            var bulletQuery = GetEntityQuery(ComponentType.ReadOnly<T>(),
                ComponentType.ReadOnly<BulletComponent>());

            if (bulletQuery.CalculateEntityCount() != 0) return;
            var playerEntity =
                GetEntityQuery(ComponentType.ReadOnly<T>(), ComponentType.ReadOnly<TimeComponent>())
                    .GetSingletonEntity();
            EntityManager.RemoveComponent<ChargedAbilityTag>(playerEntity);
        }

        private static void ChangeTurn(EntityQuery spyQuery)
        {
            var spyTeam = spyQuery.GetSingleton<TeamComponent>().myTeam;
            ArbiterCheckingSystem.ChangeTurn(spyTeam);
        }

        private void RestoreNormalSystems()
        {
            SystemManager.SetPickupSystems(true);
            this.Enabled = false;
            EntityManager.CreateEntity(typeof(ChargedAbilityEventComponent));
        }

        private void RemovePieceOnCellComponentUnderEntity(EntityCommandBuffer.ParallelWriter ecbParallelWriter, Entity targetEntity)
        {
            Entities.ForEach((Entity e, int entityInQueryIndex, ref PieceOnCellComponent pieceOnCellComponent) =>
            {
                //compare the target entity to the entity within pieceOnCellComponent.
                if (pieceOnCellComponent.PieceEntity.Equals(targetEntity))
                {
                    Tag.RemoveTag<PieceOnCellComponent>(ecbParallelWriter, entityInQueryIndex, e);
                }
            }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(Dependency);
        }

        private EntityQuery GetHighlightedPieceQuery<T>()
        {
            return GetEntityQuery(ComponentType.ReadOnly<HighlightedTag>(),
                ComponentType.ReadOnly<T>(), ComponentType.ReadOnly<RankComponent>(), ComponentType.ReadOnly<PieceTag>(), ComponentType.ReadOnly<TeamComponent>());
        }
        private EntityQuery GetHighlightedCellQuery<T>()
        {
            return GetEntityQuery(ComponentType.ReadOnly<HighlightedTag>(),
                ComponentType.ReadOnly<T>(), ComponentType.ReadOnly<CellTag>());
        }

        private void HighlightEnemyPieces(EntityCommandBuffer.ParallelWriter ecb)
        {
            Entities.WithAll<EnemyTag, PieceTag>().ForEach((Entity e, int entityInQueryIndex) =>
            {
                Tag.AddTag<EnemyCellTag>(ecb, entityInQueryIndex, e);
            }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(Dependency);
        }
        private void HighlightClickedEntities(float3 roundedWorldPos, bool mouseButtonPressed, EntityCommandBuffer.ParallelWriter ecb)
        {
            var playerTeam = GetPlayerComponent<TeamComponent>().myTeam;
            var chargedSpyQuery = GetEntityQuery(ComponentType.ReadOnly<BulletComponent>(),
                ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<HighlightedTag>());
            var chargedSpyCount = chargedSpyQuery.CalculateEntityCount();
            var highlightedTargetQuery = GetEntityQuery(ComponentType.ReadOnly<HighlightedTag>(),
                ComponentType.ReadOnly<EnemyTag>());
            var highlightedTargetCount = highlightedTargetQuery.CalculateEntityCount();

            Entities.WithAny<PieceTag, CellTag>().ForEach(
                            (Entity pieceEntity, int entityInQueryIndex, in Translation cellTranslation, in RankComponent rankComponent, in TeamComponent teamComponent) =>
                            {
                                var pieceTeam = HasComponent<PieceTag>(pieceEntity) ? teamComponent.myTeam : Team.Null;
                                var cellTeam = HasComponent<CellTag>(pieceEntity) ? teamComponent.myTeam : Team.Null;

                                var pieceRoundedLocation = math.round(cellTranslation.Value);
                                if (Location.IsMatchLocation(pieceRoundedLocation, roundedWorldPos) && mouseButtonPressed)
                                {
                                    if ((playerTeam == pieceTeam || playerTeam == cellTeam))
                                    {
                                        if (rankComponent.Rank == Piece.Spy && HasComponent<BulletComponent>(pieceEntity))
                                        {
                                            if (!HasComponent<HighlightedTag>(pieceEntity))
                                            {
                                                if (chargedSpyCount != 0) return;
                                                Tag.AddTag<HighlightedTag>(ecb, entityInQueryIndex, pieceEntity);
                                            }
                                            else
                                            {
                                                Tag.RemoveTag<HighlightedTag>(ecb, entityInQueryIndex, pieceEntity);
                                            }
                                        }

                                    }
                                    else
                                    {
                                        if (HasComponent<HighlightedTag>(pieceEntity))
                                        {
                                            Tag.RemoveTag<HighlightedTag>(ecb, entityInQueryIndex, pieceEntity);
                                            Tag.AddTag<EnemyCellTag>(ecb, entityInQueryIndex, pieceEntity);
                                        }
                                        else
                                        {
                                            if (highlightedTargetCount != 0) return;
                                            Tag.RemoveTag<EnemyCellTag>(ecb, entityInQueryIndex, pieceEntity);
                                            Tag.AddTag<HighlightedTag>(ecb, entityInQueryIndex, pieceEntity);
                                        }
                                    }

                                }
                            }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(this.Dependency);
        }

        private T GetPlayerComponent<T>() where T : struct, IComponentData
        {
            return GetPlayerEntityQuery<PlayerTag>().GetSingleton<T>();
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

    }
}
