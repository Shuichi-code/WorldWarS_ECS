using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using System;
using Assets.Scripts.Monobehaviours.Managers;
using Unity.Collections;
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
            var chargedPlayerQuery = GetEntityQuery(ComponentType.ReadOnly<ChargedAbilityTag>(),
                ComponentType.ReadOnly<ArmyComponent>(), ComponentType.ReadOnly<PlayerTag>());
            if (chargedPlayerQuery.CalculateEntityCount() == 0) return;
            var chargedPlayerEntity = chargedPlayerQuery.GetSingletonEntity();
            var playerArmy = GetComponent<ArmyComponent>(chargedPlayerEntity);
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
            //switch statement
            switch (playerArmy.army)
            {
                case Army.America:
                    break;
                case Army.Nazi:
                    Blitzkrieg(ecb);
                    break;
                case Army.Russia:
                    ActivateOneShotOneKill(ecb);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        private void Blitzkrieg(EntityCommandBuffer.ParallelWriter ecbParallelWriter)
        {
            var roundedWorldPos = Location.GetRoundedMousePosition();
            var mouseButtonPressed = Input.GetMouseButtonDown(0);

            HighlightFiveStarGeneral(ecbParallelWriter);
            HighlightColumn(ecbParallelWriter);
            SystemManager.SetPickupSystems(false);

            //RestoreNormalSystems();
        }

        private void HighlightColumn(EntityCommandBuffer.ParallelWriter ecbParallelWriter)
        {
            var chargedFiveStarGeneralQuery = GetEntityQuery(ComponentType.ReadOnly<ChargedFiveStarGeneralTag>(), ComponentType.ChunkComponentReadOnly<PlayerTag>());
            var chargedFiveStarGeneralTranslation = chargedFiveStarGeneralQuery.GetSingleton<Translation>();

            var playerPiecesQuery = GetEntityQuery(ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<PieceTag>(), typeof(Translation));
            var playerPiecesArray = playerPiecesQuery.ToComponentDataArray<Translation>(Allocator.Temp);

            var possibleValidLocationArray = SetPossibleValidLocation(chargedFiveStarGeneralTranslation.Value);

            var foundPieceEntitiesArray = new NativeArray<Entity>(3,Allocator.TempJob);
            foundPieceEntitiesArray = PopulateFoundPieceEntitiesArray(possibleValidLocationArray, foundPieceEntitiesArray);

            var blitzFormation = GetBlitzFormation(foundPieceEntitiesArray);

            if (blitzFormation != BlitzkriegFormation.InvalidBlitz)
            {
                //proceed with highlighting
                switch (blitzFormation)
                {
                    case BlitzkriegFormation.GeneralAtFront:
                        break;
                    case BlitzkriegFormation.GeneralInMiddle:
                        break;
                    case BlitzkriegFormation.GeneralAtBack:
                        break;
                    case BlitzkriegFormation.InvalidBlitz:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            possibleValidLocationArray.Dispose();
            foundPieceEntitiesArray.Dispose();
            //if there are no allies in front, highlight at most 2 allies behind the general
            //if there is one ally in front and no ally at the next space, highlight front and back of General

            //if there are 2 allies in front, check if there is ally 3 spaces in front of the general
        }

        private BlitzkriegFormation GetBlitzFormation(NativeArray<Entity> foundPieceEntitiesArray)
        {
            if (foundPieceEntitiesArray[0] == Entity.Null)
            {
                return BlitzkriegFormation.GeneralAtFront;
            }
            else if (foundPieceEntitiesArray[0] != Entity.Null && foundPieceEntitiesArray[1] == Entity.Null)
            {
                return BlitzkriegFormation.GeneralInMiddle;
            }
            else if (foundPieceEntitiesArray[0] != Entity.Null && foundPieceEntitiesArray[1] != Entity.Null &&
                     foundPieceEntitiesArray[2] == Entity.Null)
            {
                return BlitzkriegFormation.GeneralInMiddle;
            }

            return BlitzkriegFormation.InvalidBlitz;
        }

        private NativeArray<Entity> PopulateFoundPieceEntitiesArray(NativeArray<float3> possibleValidLocationArray,
            NativeArray<Entity> foundPieceEntitiesArray)
        {
            Entities.WithAll<PlayerTag, PieceTag>().ForEach((Entity e, in Translation translation) =>
            {
                for (var index = 0; index < possibleValidLocationArray.Length; index++)
                {
                    var possibleValidLocation = possibleValidLocationArray[index];
                    if (Location.IsMatchLocation(possibleValidLocation, translation.Value))
                    {
                        foundPieceEntitiesArray[index] = e;
                    }
                }
            }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(Dependency);
            return foundPieceEntitiesArray;
        }

        private static NativeArray<float3> SetPossibleValidLocation(float3 selectedCellTranslation)
        {
            NativeArray<float3> cellArrayPositions = new NativeArray<float3>(3, Allocator.TempJob);
            cellArrayPositions[0] = new float3(selectedCellTranslation.x, selectedCellTranslation.y + 1, selectedCellTranslation.z);
            cellArrayPositions[1] = new float3(selectedCellTranslation.x, selectedCellTranslation.y + 2, selectedCellTranslation.z);
            cellArrayPositions[2] = new float3(selectedCellTranslation.x, selectedCellTranslation.y+3, selectedCellTranslation.z);
            return cellArrayPositions;
        }

        private void HighlightFiveStarGeneral(EntityCommandBuffer.ParallelWriter ecbParallelWriter)
        {
            Entities.
                WithAll<PlayerTag, PieceTag>().
                ForEach((Entity e, int entityInQueryIndex, in RankComponent rankComponent) =>
                {
                    if (rankComponent.Rank != Piece.FiveStarGeneral) return;
                    ecbParallelWriter.AddComponent<HighlightedTag>(entityInQueryIndex, e);
                    ecbParallelWriter.AddComponent<ChargedFiveStarGeneralTag>(entityInQueryIndex, e);
                }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(Dependency);
        }

        private void ActivateOneShotOneKill(EntityCommandBuffer.ParallelWriter ecbParallelWriter)
        {
            var roundedWorldPos = Location.GetRoundedMousePosition();
            var mouseButtonPressed = Input.GetMouseButtonDown(0);
            //highlight all enemy pieces in red
            var enemyCellQuery = GetEntityQuery(ComponentType.ReadOnly<EnemyCellTag>());
            if (enemyCellQuery.CalculateEntityCount() == 0)
                HighlightEnemyPieces(ecbParallelWriter);

            HighlightClickedEntities(roundedWorldPos, mouseButtonPressed, ecbParallelWriter, Army.Russia);


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

        private void CheckBullets<T>() where T : struct, IComponentData
        {
            var bulletQuery = GetEntityQuery(ComponentType.ReadOnly<T>(),
                ComponentType.ReadOnly<BulletComponent>());

            if (bulletQuery.CalculateEntityCount() != 0) return;
            RemoveChargeFromPlayer<T>();
        }

        private void RemoveChargeFromPlayer<T>()
        {
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
        private void HighlightClickedEntities(float3 roundedWorldPos, bool mouseButtonPressed, EntityCommandBuffer.ParallelWriter ecb, Army army)
        {
            var playerTeam = GetPlayerComponent<TeamComponent>().myTeam;
            var chargedSpyQuery = GetEntityQuery(ComponentType.ReadOnly<BulletComponent>(),
                ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<HighlightedTag>());
            var chargedSpyCount = chargedSpyQuery.CalculateEntityCount();
            var chargedFiveStarGeneralQuery = GetEntityQuery(ComponentType.ReadOnly<BulletComponent>(),
                ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<HighlightedTag>());
            var highlightedTargetQuery = GetEntityQuery(ComponentType.ReadOnly<HighlightedTag>(),
                ComponentType.ReadOnly<EnemyTag>());
            var highlightedTargetCount = highlightedTargetQuery.CalculateEntityCount();

            Entities.WithAny<PieceTag, CellTag>().
                ForEach((Entity pieceEntity, int entityInQueryIndex, in Translation cellTranslation, in RankComponent rankComponent, in TeamComponent teamComponent, in Translation translation) =>
                            {
                                var pieceTeam = HasComponent<PieceTag>(pieceEntity) ? teamComponent.myTeam : Team.Null;
                                var cellTeam = HasComponent<CellTag>(pieceEntity) ? teamComponent.myTeam : Team.Null;

                                var pieceRoundedLocation = math.round(cellTranslation.Value);
                                if (!Location.IsMatchLocation(pieceRoundedLocation, roundedWorldPos) ||
                                    !mouseButtonPressed) return;
                                if ((playerTeam == pieceTeam || playerTeam == cellTeam))
                                {
                                    if (rankComponent.Rank != Piece.Spy || !HasComponent<BulletComponent>(pieceEntity)) return;
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
