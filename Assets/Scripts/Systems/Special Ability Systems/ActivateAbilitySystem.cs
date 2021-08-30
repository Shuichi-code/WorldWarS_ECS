using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using System;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Scripts.Systems.Special_Ability_Systems
{
    /// <summary>
    /// System that handles the activation of their army's superpower
    /// </summary>
    [UpdateAfter(typeof(ChargeAbilitySystem))]
    public class ActivateAbilitySystem : ParallelSystem
    {
        protected override void OnCreate()
        {
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
            var ecb = EcbSystem.CreateCommandBuffer().AsParallelWriter();
            //switch statement
            switch (playerArmy.army)
            {
                case Army.America:
                    break;
                case Army.Nazi:
                    CommenceBlitzkrieg(ecb);
                    break;
                case Army.Russia:
                    ActivateOneShotOneKill(ecb);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private void CommenceBlitzkrieg(EntityCommandBuffer.ParallelWriter ecbParallelWriter)
        {
            var roundedWorldPos = Location.GetRoundedMousePosition();
            var mouseButtonPressed = Input.GetMouseButtonDown(0);

            SystemManager.SetPickupSystems(false);
            var highlightedQuery = GetEntityQuery(ComponentType.ReadOnly<HighlightedTag>());
            if (highlightedQuery.CalculateEntityCount() == 0)
            {
                HighlightFiveStarGeneral(ecbParallelWriter);
                HighlightBatallion(ecbParallelWriter);
            }

            //check if player pressed on the five star general
            if (!mouseButtonPressed) return;
            var chargedFiveStarQuery = GetEntityQuery(ComponentType.ReadOnly<ChargedFiveStarGeneralTag>(),
                ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<TeamComponent>());
            if (chargedFiveStarQuery.CalculateEntityCount() == 0) return;
            var chargedFiveStarTranslation = chargedFiveStarQuery.GetSingleton<Translation>();
            var chargedFiveStarTeam = chargedFiveStarQuery.GetSingleton<TeamComponent>().myTeam;
            if (!Location.IsMatchLocation(chargedFiveStarTranslation.Value, roundedWorldPos)) return;
            ChargeBatallion(ecbParallelWriter);
            var spearTipQuery = GetEntityQuery(ComponentType.ReadOnly<SpearTipTag>(),
                ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<RankComponent>(), ComponentType.ReadOnly<TeamComponent>());
            if (spearTipQuery.CalculateEntityCount() == 0) return;


            CreateSingleEvent<PieceCollisionCheckerTag>();
            ChangeTurn(chargedFiveStarTeam);
            RemoveNaziSpecialAbilityTags(ecbParallelWriter);
            CreateSingleEvent<PieceOnCellUpdaterTag>();
            RestoreNormalSystems();
        }
        private void CreateSingleEvent<T>()
        {
            EntityManager.CreateEntity(typeof(T));
        }

        private void CheckIfBatallionIsInBattle(EntityCommandBuffer.ParallelWriter ecbParallelWriter, EntityQuery spearTipQuery)
        {
            var spearTipLocation = spearTipQuery.GetSingleton<Translation>().Value;
            var spearTipEntity = spearTipQuery.GetSingletonEntity();
            Entities.
                WithAll<PieceTag, EnemyTag>().
                WithNone<PrisonerTag>().
                ForEach((Entity e, int entityInQueryIndex, in Translation translation) =>
                {
                    if (!Location.IsMatchLocation(spearTipLocation, translation.Value)) return;
                    Debug.Log("Commencing Battle!");
                    ecbParallelWriter.AddComponent(entityInQueryIndex, e, new FighterTag());
                    ecbParallelWriter.AddComponent(entityInQueryIndex, spearTipEntity, new FighterTag());
                }).ScheduleParallel();
            EcbSystem.AddJobHandleForProducer(Dependency);
        }

        private void RemoveNaziSpecialAbilityTags(EntityCommandBuffer.ParallelWriter ecbParallelWriter)
        {
            Entities.
                WithAny<SpearTipTag, ChargedFiveStarGeneralTag>().
                ForEach((Entity e, int entityInQueryIndex) =>
            {
                if (HasComponent<SpearTipTag>(e))
                {
                    Tag.RemoveTag<SpearTipTag>(ecbParallelWriter, entityInQueryIndex, e);
                }
                else if (HasComponent<ChargedFiveStarGeneralTag>(e))
                {
                    Tag.RemoveTag<ChargedFiveStarGeneralTag>(ecbParallelWriter, entityInQueryIndex, e);
                }
            }).ScheduleParallel();
            EcbSystem.AddJobHandleForProducer(Dependency);
        }

        private void ChargeBatallion(EntityCommandBuffer.ParallelWriter ecbParallelWriter)
        {
            Entities.WithAll<HighlightedTag, PieceTag, PlayerTag>()
                .ForEach((Entity e, int entityInQueryIndex, ref Translation translation) =>
                {
                    translation.Value.y++;
                    ecbParallelWriter.SetComponent(entityInQueryIndex, e, new OriginalLocationComponent()
                    {
                        originalLocation = translation.Value
                    });
                }).ScheduleParallel();
            EcbSystem.AddJobHandleForProducer(Dependency);
        }

        private void HighlightBatallion(EntityCommandBuffer.ParallelWriter ecbParallelWriter)
        {
            var chargedFiveStarGeneralQuery = GetEntityQuery(ComponentType.ReadOnly<ChargedFiveStarGeneralTag>(), ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<Translation>());
            if (chargedFiveStarGeneralQuery.CalculateEntityCount() == 0) return;
            var chargedFiveStarGeneralTranslation = chargedFiveStarGeneralQuery.GetSingleton<Translation>();

            var cellArrayPositions = new NativeArray<Translation>(3, Allocator.TempJob);
            cellArrayPositions = SetPossibleValidLocation(cellArrayPositions, chargedFiveStarGeneralTranslation.Value);

            var foundPieceEntitiesArray = new NativeArray<Entity>(3, Allocator.TempJob);
            foundPieceEntitiesArray = PopulateFoundPieceEntitiesArray(cellArrayPositions, foundPieceEntitiesArray);

            var blitzFormation = GetBlitzFormation(foundPieceEntitiesArray);
            if (blitzFormation != BlitzkriegFormation.InvalidBlitz)
            {
                var frontUnitPosition = new Translation();
                var rearUnitPosition = new float3(float.MaxValue);
                var playerPiecesArray = GetEntityQuery(ComponentType.ReadOnly<PieceTag>(),
                    ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<Translation>()).ToComponentDataArray<Translation>(Allocator.Temp);
                var isGeneralAtFront = false;
                //proceed with highlighting
                switch (blitzFormation)
                {
                    case BlitzkriegFormation.GeneralAtFront:
                        //get at most two pieces behind the general and highlight them
                        frontUnitPosition.Value = new float3(chargedFiveStarGeneralTranslation.Value.x,
                            chargedFiveStarGeneralTranslation.Value.y - 1,
                            chargedFiveStarGeneralTranslation.Value.z);
                        if (Location.HasMatch(playerPiecesArray, frontUnitPosition))
                            rearUnitPosition = new float3(chargedFiveStarGeneralTranslation.Value.x,
                            chargedFiveStarGeneralTranslation.Value.y - 2, chargedFiveStarGeneralTranslation.Value.z);
                        isGeneralAtFront = true;
                        //TagGeneralAsSpearTip(ecbParallelWriter);
                        break;
                    case BlitzkriegFormation.GeneralInMiddle:
                        frontUnitPosition.Value = new float3(chargedFiveStarGeneralTranslation.Value.x,
                            chargedFiveStarGeneralTranslation.Value.y + 1, chargedFiveStarGeneralTranslation.Value.z);
                        rearUnitPosition = new float3(chargedFiveStarGeneralTranslation.Value.x,
                            chargedFiveStarGeneralTranslation.Value.y - 1, chargedFiveStarGeneralTranslation.Value.z);
                        break;
                    case BlitzkriegFormation.GeneralAtBack:
                        frontUnitPosition.Value = new float3(chargedFiveStarGeneralTranslation.Value.x,
                            chargedFiveStarGeneralTranslation.Value.y + 2, chargedFiveStarGeneralTranslation.Value.z);
                        if (Location.HasMatch(playerPiecesArray, frontUnitPosition))
                            rearUnitPosition = new float3(chargedFiveStarGeneralTranslation.Value.x,
                            chargedFiveStarGeneralTranslation.Value.y + 1, chargedFiveStarGeneralTranslation.Value.z);
                        break;
                    case BlitzkriegFormation.InvalidBlitz:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                HighlightSoldiersAroundGeneral(ecbParallelWriter, frontUnitPosition.Value, rearUnitPosition, isGeneralAtFront);
            }
            foundPieceEntitiesArray.Dispose();
            cellArrayPositions.Dispose();
        }

        private void HighlightSoldiersAroundGeneral(EntityCommandBuffer.ParallelWriter ecbParallelWriter,
            float3 validPosition1,
            float3 validPosition2, bool isGeneralAtFront = false)
        {
            Entities.WithAll<PlayerTag, PieceTag>().ForEach((Entity e, int entityInQueryIndex, in Translation translation) =>
            {
                //if (!Location.IsMatchLocation(translation.Value, validPosition1) &&
                //    !Location.IsMatchLocation(translation.Value, validPosition2)) return;
                if (Location.IsMatchLocation(translation.Value, validPosition1) ||
                    Location.IsMatchLocation(translation.Value, validPosition2) ||
                    HasComponent<ChargedFiveStarGeneralTag>(e))
                {
                    ecbParallelWriter.AddComponent<HighlightedTag>(entityInQueryIndex, e);
                }

                if ((Location.IsMatchLocation(translation.Value, validPosition1) && !isGeneralAtFront) || (isGeneralAtFront && HasComponent<ChargedFiveStarGeneralTag>(e)))
                {
                    ecbParallelWriter.AddComponent<SpearTipTag>(entityInQueryIndex, e);
                }
            }).ScheduleParallel();
            EcbSystem.AddJobHandleForProducer(Dependency);
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
                return BlitzkriegFormation.GeneralAtBack;
            }

            return BlitzkriegFormation.InvalidBlitz;
        }

        /// <summary>
        /// Populates the entities array that indicates if there are pieces in front of the Five Star General, possibly blocking the blitz
        /// </summary>
        /// <param name="possibleValidLocationArray"></param>
        /// <param name="foundPieceEntitiesArray"></param>
        /// <returns></returns>
        private NativeArray<Entity> PopulateFoundPieceEntitiesArray(NativeArray<Translation> possibleValidLocationArray,
            NativeArray<Entity> foundPieceEntitiesArray)
        {
            SetEntityArrayToNull(foundPieceEntitiesArray);

            //WARNING: This needs to be scheduled only so as to not create race condition due to the for loop inside the job.
            Entities.WithAll<PlayerTag, PieceTag>().ForEach((Entity e, in Translation translation) =>
            {
                for (var index = 0; index < possibleValidLocationArray.Length; index++)
                {
                    var possibleValidLocation = possibleValidLocationArray[index].Value;
                    if (Location.IsMatchLocation(possibleValidLocation, translation.Value))
                    {
                        foundPieceEntitiesArray[index] = e;
                    }
                }

            }).Schedule();
            this.CompleteDependency();
            return foundPieceEntitiesArray;
        }

        private void SetEntityArrayToNull(NativeArray<Entity> foundPieceEntitiesArray)
        {
            for (var index = 0; index < foundPieceEntitiesArray.Length; index++)
            {
                foundPieceEntitiesArray[index] = Entity.Null;
            }
        }

        /// <summary>
        /// Sets the Possible location of the pieces in front of the five star general
        /// </summary>
        /// <param name="cellPositionArray"></param>
        /// <param name="fiveStarGeneralTranslation"></param>
        /// <returns></returns>
        private static NativeArray<Translation> SetPossibleValidLocation(NativeArray<Translation> cellPositionArray, float3 fiveStarGeneralTranslation)
        {
            for (var i = 0; i < cellPositionArray.Length; i++)
            {
                cellPositionArray[i] = new Translation()
                {
                    Value = new float3(fiveStarGeneralTranslation.x, fiveStarGeneralTranslation.y + (i + 1), fiveStarGeneralTranslation.z)
                };
            }
            return cellPositionArray;
        }

        private void HighlightFiveStarGeneral(EntityCommandBuffer.ParallelWriter ecbParallelWriter)
        {
            Entities.
                WithAll<PlayerTag, PieceTag>().
                ForEach((Entity e, int entityInQueryIndex, in RankComponent rankComponent) =>
                {
                    if (rankComponent.Rank != Piece.FiveStarGeneral) return;
                    ecbParallelWriter.AddComponent<ChargedFiveStarGeneralTag>(entityInQueryIndex, e);
                    if (!HasComponent<BulletComponent>(e))
                        ecbParallelWriter.AddComponent<BulletComponent>(entityInQueryIndex, e);
                }).ScheduleParallel();
            EcbSystem.AddJobHandleForProducer(Dependency);
        }

        private void ActivateOneShotOneKill(EntityCommandBuffer.ParallelWriter ecbParallelWriter)
        {
            var ecb = EcbSystem.CreateCommandBuffer();
            var roundedWorldPos = Location.GetRoundedMousePosition();
            var mouseButtonPressed = Input.GetMouseButtonDown(0);
            //highlight all enemy pieces in red
            var enemyCellQuery = GetEntityQuery(ComponentType.ReadOnly<EnemyCellTag>());
            if (enemyCellQuery.CalculateEntityCount() == 0)
                HighlightEnemyPieces(ecbParallelWriter);

            HighlightClickedEntities(roundedWorldPos, mouseButtonPressed, ecbParallelWriter, Army.Russia);


            var fightingEntitiesQuery = GetEntityQuery(ComponentType.ReadOnly<HighlightedTag>(), ComponentType.ReadOnly<PieceTag>());
            if (fightingEntitiesQuery.CalculateEntityCount() != 2) return;
            //get the spy entity of this round
            var fightingEntityArray = fightingEntitiesQuery.ToEntityArray(Allocator.Temp);
            var rankArray = GetComponentDataFromEntity<RankComponent>(true);
            var teamArray = GetComponentDataFromEntity<TeamComponent>(true);
            var spyEntity = new Entity();
            var targetEntity = new Entity();
            var gmQuery = GetEntityQuery(ComponentType.ReadOnly<GameManagerComponent>());
            var teamToMove = gmQuery.GetSingleton<GameManagerComponent>().teamToMove;

            foreach (var entity in fightingEntityArray)
            {
                if (rankArray[entity].Rank == Piece.Spy && teamArray[entity].myTeam == teamToMove)
                {
                    spyEntity = entity;
                }
                else
                {
                    targetEntity = entity;
                }
            }

            var spyTeam = teamArray[spyEntity].myTeam;
            var targetRank = rankArray[targetEntity].Rank;
            var playerQuery = GetEntityQuery(ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.ReadOnly<TimeComponent>(), ComponentType.ReadOnly<TeamComponent>());
            var enemyQuery = GetEntityQuery(ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<TimeComponent>(), ComponentType.ReadOnly<TeamComponent>());
            var playerTeam = teamArray[playerQuery.GetSingletonEntity()].myTeam;
            var enemyTeam = teamArray[enemyQuery.GetSingletonEntity()].myTeam;

            var fightResult = FightCalculator.DetermineFightResult(Piece.Spy, targetRank);
            switch (fightResult)
            {
                case FightResult.AttackerWins:
                    //destroy the target
                    EntityManager.AddComponent<CapturedComponent>(targetEntity);
                    break;
                case FightResult.DefenderWins:
                    EntityManager.AddComponent<RevealedTag>(spyEntity);
                    break;
                case FightResult.BothLose:
                    EntityManager.AddComponent<RevealedTag>(spyEntity);
                    EntityManager.AddComponent<RevealedTag>(targetEntity);
                    break;
                case FightResult.FlagDestroyed:

                    var attackingTeam = teamArray[spyEntity]
                        .myTeam;
                    ArbiterCheckingSystem.DeclareWinner(ecb, attackingTeam);
                    break;
                case FightResult.NoFight:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            //remove one bullet from the spy piece
            //EntityManager.RemoveComponent<BulletComponent>(spyEntity);
            RemoveBulletFromAttackingSpy(ecb, spyEntity);
            var removeChargeEventJob = RemoveChargeEventFiredTagFromPlayer(ecb, spyTeam);
            //EntityManager.RemoveComponent<ChargeEventFiredTag>(playerQuery.GetSingletonEntity());

            CheckPlayerBullets(playerTeam, removeChargeEventJob);
            //CheckPlayerBullets<EnemyTag>(enemyTeam);

            RestoreNormalSystems();
            CreateSingleEvent<PieceOnCellUpdaterTag>();
            ChangeTurn(spyTeam);
        }

        private void RemoveBulletFromAttackingSpy(EntityCommandBuffer ecb, Entity spyEntity)
        {
            EntityManager.RemoveComponent<BulletComponent>(spyEntity);
        }

        private JobHandle RemoveChargeEventFiredTagFromPlayer(EntityCommandBuffer ecbParallelWriter, Team currentTeam)
        {
            var removeChargeEventJob = Entities.WithAll<TimeComponent>().ForEach((Entity e, int entityInQueryIndex, in TeamComponent teamComponent) =>
            {
                if (teamComponent.myTeam == currentTeam)
                {
                    ecbParallelWriter.RemoveComponent<ChargeEventFiredTag>(e);
                }
            }).Schedule(Dependency);
            //removeChargeEventJob.Complete();

            return removeChargeEventJob;
        }

        /// <summary>
        /// Method for removing the chargeability from player if the russian army has no more bullets
        /// </summary>
        /// <typeparam name="T">Type of Player</typeparam>
        private void CheckPlayerBullets(Team currentTeam, JobHandle removeBulletJob)
        {
            var spyWithBulletsArray = new NativeArray<Entity>(1, Allocator.TempJob) {[0] = Entity.Null};
            //Debug.Log("Bullets remaining: " + GetEntityQuery(ComponentType.ReadOnly<BulletComponent>(), ComponentType.ReadOnly<PlayerTag>()).CalculateEntityCount().ToString());

            var checkSpyWithBulletsJob = Entities.
                WithAll<PieceTag, BulletComponent, PlayerTag>().
                WithNone<CapturedComponent>().
                ForEach((Entity e, in TeamComponent teamComponent, in RankComponent rankComponent) =>
            {
                if (teamComponent.myTeam == currentTeam && rankComponent.Rank == Piece.Spy && (!HasComponent<PrisonerTag>(e)))
                {
                    spyWithBulletsArray[0] = e;
                }
            }).
                Schedule(removeBulletJob);
            checkSpyWithBulletsJob.Complete();
            if (spyWithBulletsArray[0] == Entity.Null)
            {
                //Debug.Log("Removing Charge from Player!");
                RemoveChargeFromTeam(currentTeam, checkSpyWithBulletsJob);
            }
            else
            {
                Debug.Log("Player still has bullets!");
            }


            spyWithBulletsArray.Dispose();
        }

        private void RemoveChargeFromTeam(Team currentTeam, JobHandle removeBulletJob)
        {
            var ecb = EcbSystem.CreateCommandBuffer();
            Entities.
                WithAll<TimeComponent>().
                ForEach((Entity e, in TeamComponent teamComponent) =>
            {
                if (teamComponent.myTeam == currentTeam)
                {
                    ecb.RemoveComponent<ChargedAbilityTag>(e);
                }
            }).Schedule(removeBulletJob).Complete();
            //Dependency.Complete();
        }

        private void ChangeTurn(Team currentTurnTeam)
        {
            var changeTurnEntity = EntityManager.CreateEntity();
            EntityManager.AddComponent<ChangeTurnComponent>(changeTurnEntity);
            EntityManager.SetComponentData(changeTurnEntity, new ChangeTurnComponent()
            {
                currentTurnTeam = currentTurnTeam
            });
        }

        private void RestoreNormalSystems()
        {
            SystemManager.SetPickupSystems(true);
            SystemManager.SetSystemStatus<ActivateAbilitySystem>(false);
            //this.Enabled = false;
            var chargedAbilityEventEntity = EntityManager.CreateEntity(typeof(ChargedAbilityEventComponent));
            EntityManager.SetComponentData(chargedAbilityEventEntity, new ChargedAbilityEventComponent()
            {
                activateUI = false
            });
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
            EcbSystem.AddJobHandleForProducer(Dependency);
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
            EcbSystem.AddJobHandleForProducer(Dependency);
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
            EcbSystem.AddJobHandleForProducer(this.Dependency);
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
