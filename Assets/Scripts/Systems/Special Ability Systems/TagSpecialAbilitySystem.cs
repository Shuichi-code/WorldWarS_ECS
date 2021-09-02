using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
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

            RemoveSpecialAbilityForNazi(ecb);

            if (HasComponent<ChargedAbilityTag>(playerEntity) && HasComponent<ChargedAbilityTag>(enemyEntity)) return;

            TagPlayerWithSpecialAbilityForNazi(ecb, playerEntity, enemyEntity);
            TagPlayerWithSpecialAbilityForRussia(ecb, playerEntity, enemyEntity);
        }

        private void RemoveSpecialAbilityForNazi(EntityCommandBuffer ecb)
        {
            var teamToMove = GetTeamToMove();

            var armyOfTeamToMove = GetArmyOfTeamToMove(teamToMove);
            if (armyOfTeamToMove != Army.Nazi) return;
            if (IsBatallionAtBorder(teamToMove)  ||
                IsFiveStarGeneralDead(teamToMove))
            {
                RemoveChargedAbility(teamToMove, ecb);
            }
        }

        private bool IsFiveStarGeneralDead(Team teamToMove)
        {
            var fiveStarEntityArray = new NativeArray<Entity>(1, Allocator.TempJob){[0]=Entity.Null};
            Entities.
                WithAll<PrisonerTag>().
                ForEach((Entity pieceEntity, in RankComponent rankComponent, in TeamComponent teamComponent, in ArmyComponent armyComponent) =>
            {
                if (teamComponent.myTeam == teamToMove && rankComponent.Rank == Piece.FiveStarGeneral &&
                    armyComponent.army == Army.Nazi)
                {
                    fiveStarEntityArray[0] = pieceEntity;
                }
            }).Schedule();
            CompleteDependency();
            var result = fiveStarEntityArray[0] != Entity.Null;
            fiveStarEntityArray.Dispose();
            return result;
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
            var teamToMove = GetTeamToMove();
            Entities.
                WithAll<PieceTag>().
                ForEach((Entity pieceEntity, in ArmyComponent armyComponent, in RankComponent rankComponent, in TeamComponent teamComponent) =>
                {
                    if (armyComponent.army != Army.Nazi || rankComponent.Rank != Piece.FiveStarGeneral ||
                        teamComponent.myTeam != teamToMove) return;
                    if (!HasComponent<PrisonerTag>(pieceEntity))
                    {
                        ecb.AddComponent<ChargedFiveStarGeneralTag>(pieceEntity);
                        ecb.AddComponent(HasComponent<PlayerTag>(pieceEntity) ? playerEntity : enemyEntity,
                            new SpecialAbilityComponent());
                    }
                    else if ((HasComponent<PrisonerTag>(pieceEntity) && (HasComponent<ChargedAbilityTag>(playerEntity) ||
                                                                         HasComponent<ChargedAbilityTag>(enemyEntity))))
                    {
                        ecb.RemoveComponent<ChargedFiveStarGeneralTag>(pieceEntity);
                        ecb.RemoveComponent<ChargedAbilityTag>(HasComponent<PlayerTag>(pieceEntity)
                            ? playerEntity
                            : enemyEntity);
                    }
                }).Schedule();
            Dependency.Complete();
        }

        private Army GetArmyOfTeamToMove(Team teamToMove)
        {
            var armyArray = new NativeArray<Army>(1, Allocator.TempJob);
            Entities
                .WithAll<TimeComponent>().
                ForEach((Entity e, in ArmyComponent armyComponent, in TeamComponent teamComponent) =>
            {
                if (teamComponent.myTeam == teamToMove)
                {
                    armyArray[0] = armyComponent.army;
                }
            }).Schedule();
            CompleteDependency();

            var armyToMove = armyArray[0];
            armyArray.Dispose();

            return armyToMove;
        }

        private void RemoveChargedAbility(Team teamToMove, EntityCommandBuffer ecb)
        {
            Entities.
                WithAll<TimeComponent>(). //the defining component for players
                ForEach((Entity playerEntity, in TeamComponent teamComponent) =>
                {
                    if ((teamComponent.myTeam != teamToMove)) return;
                    if(HasComponent<ChargedAbilityTag>(playerEntity))
                        ecb.RemoveComponent<ChargedAbilityTag>(playerEntity);
                    if(HasComponent<ChargeEventFiredTag>(playerEntity))
                        ecb.RemoveComponent<ChargeEventFiredTag>(playerEntity);
                    var chargeUIEntity = ecb.CreateEntity();
                    ecb.AddComponent(chargeUIEntity, new ChargedAbilityEventComponent(){activateUI = false});
                }).Schedule();
            CompleteDependency();
        }

        private Team GetTeamToMove()
        {
            var gmQuery = GetEntityQuery(ComponentType.ReadOnly<GameManagerComponent>());
            return gmQuery.GetSingleton<GameManagerComponent>().teamToMove;
        }

        private bool IsBatallionAtBorder(Team teamToMove)
        {
            var fiveStarGeneralLocation = GetFiveStarGeneralLocation(teamToMove);
            var fiveStarGeneralEntity = GetFiveStarGeneralEntity(teamToMove);
            var cellPositionArray = new NativeArray<Translation>(3, Allocator.TempJob);
            cellPositionArray = SetPossibleValidLocation(cellPositionArray, fiveStarGeneralLocation.Value);
            var foundPieceEntitiesArray = new NativeArray<Entity>(3, Allocator.TempJob);
            foundPieceEntitiesArray = PopulateFoundEntitiesArray(cellPositionArray, foundPieceEntitiesArray);
            var blitzFormation = GetBlitzFormation(foundPieceEntitiesArray);

            var result = false;
            var soldierInFrontLocation = new Translation();
            var soldierEntity = new Entity();
            switch (blitzFormation)
            {
                case BlitzkriegFormation.GeneralAtFront:
                    //General must not be on a border
                    result = IsSoldierAtBorder(fiveStarGeneralEntity, teamToMove);
                    break;
                case BlitzkriegFormation.GeneralInMiddle:
                    //get the soldier location

                    soldierInFrontLocation.Value = new float3(fiveStarGeneralLocation.Value.x,
                        fiveStarGeneralLocation.Value.y + 1, fiveStarGeneralLocation.Value.z);
                    soldierEntity = GetSoldierEntity(teamToMove, soldierInFrontLocation);
                    result = IsSoldierAtBorder(soldierEntity, teamToMove);
                    break;
                case BlitzkriegFormation.GeneralAtBack:
                    //The soldier in front must not be on a border cell
                    soldierInFrontLocation.Value = new float3(fiveStarGeneralLocation.Value.x,
                        fiveStarGeneralLocation.Value.y + 2, fiveStarGeneralLocation.Value.z);
                    soldierEntity = GetSoldierEntity(teamToMove, soldierInFrontLocation);
                    result = IsSoldierAtBorder(soldierEntity, teamToMove);
                    break;
                case BlitzkriegFormation.InvalidBlitz:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            cellPositionArray.Dispose();
            foundPieceEntitiesArray.Dispose();
            return result;
        }

        private bool IsSoldierAtBorder(Entity soldierEntity, Team teamToMove)
        {
            var cellMatchArray = new NativeArray<Entity>(1, Allocator.TempJob) { [0] = Entity.Null };
            Entities.
                WithAll<CellTag,LastCellsTag>().
                ForEach((Entity cellEntity, in Translation cellTranslation, in HomeCellComponent homeCellComponent, in PieceOnCellComponent pieceOnCellComponent) =>
            {
                if (pieceOnCellComponent.PieceEntity.Equals(soldierEntity) && homeCellComponent.homeTeam != teamToMove)
                {
                    cellMatchArray[0] = cellEntity;
                }
            }).Schedule();
            CompleteDependency();
            var result = cellMatchArray[0] != Entity.Null;
            cellMatchArray.Dispose();
            return result;
        }

        private NativeArray<Entity> PopulateFoundEntitiesArray(NativeArray<Translation> possibleValidLocationArray, NativeArray<Entity> foundPieceEntitiesArray)
        {
            SetEntityArrayToNull(foundPieceEntitiesArray);

            //WARNING: This needs to be scheduled only so as to not create race condition due to the for loop inside the job.
            Entities.WithAll<PlayerTag, PieceTag>().ForEach((Entity pieceEntity, in Translation pieceTranslation) =>
            {
                for (var index = 0; index < possibleValidLocationArray.Length; index++)
                {
                    var possibleValidLocation = possibleValidLocationArray[index].Value;
                    if (Location.IsMatchLocation(possibleValidLocation, pieceTranslation.Value))
                    {
                        foundPieceEntitiesArray[index] = pieceEntity;
                    }
                }

            }).Schedule();
            this.CompleteDependency();
            return foundPieceEntitiesArray;
        }

        public static void SetEntityArrayToNull(NativeArray<Entity> foundPieceEntitiesArray)
        {
            for (var index = 0; index < foundPieceEntitiesArray.Length; index++)
            {
                foundPieceEntitiesArray[index] = Entity.Null;
            }
        }

        private Translation GetFiveStarGeneralLocation(Team currentTeam)
        {
            var translationArray = new NativeArray<Translation>(1, Allocator.TempJob);
            Entities.
                WithAll<PieceTag>().
                WithNone<PrisonerTag, CapturedComponent>().
                ForEach((Entity pieceEntity, in RankComponent rankComponent, in TeamComponent teamComponent, in Translation translation) =>
            {
                if (teamComponent.myTeam == currentTeam && rankComponent.Rank == Piece.FiveStarGeneral)
                {
                    translationArray[0] = translation;
                }
            }).Schedule();
            CompleteDependency();
            var generalLocation = translationArray[0];
            //Debug.Log("Five-Star y-Location is: " + generalLocation.Value.y);
            translationArray.Dispose();
            return generalLocation;
        }

        private Entity GetFiveStarGeneralEntity(Team currentTeam)
        {
            var entityArray = new NativeArray<Entity>(1, Allocator.TempJob);
            Entities.
                WithAll<PieceTag>().
                WithNone<PrisonerTag, CapturedComponent>().
                ForEach((Entity pieceEntity, in RankComponent rankComponent, in TeamComponent teamComponent) =>
                {
                    if (teamComponent.myTeam == currentTeam && rankComponent.Rank == Piece.FiveStarGeneral)
                    {
                        entityArray[0] = pieceEntity;
                    }
                }).Schedule();
            CompleteDependency();
            var generalEntity = entityArray[0];
            //Debug.Log("Five-Star y-Location is: " + generalLocation.Value.y);
            entityArray.Dispose();
            return generalEntity;
        }

        private Entity GetSoldierEntity(Team currentTeam, Translation soldierTranslation)
        {
            var entityArray = new NativeArray<Entity>(1, Allocator.TempJob);
            Entities.
                WithAll<PieceTag>().
                WithNone<PrisonerTag, CapturedComponent>().
                ForEach((Entity pieceEntity, in TeamComponent teamComponent, in Translation pieceTranslation) =>
                {
                    if (teamComponent.myTeam == currentTeam && Location.IsMatchLocation(pieceTranslation.Value, soldierTranslation.Value))
                    {
                        entityArray[0] = pieceEntity;
                    }
                }).Schedule();
            CompleteDependency();
            var generalEntity = entityArray[0];
            //Debug.Log("Five-Star y-Location is: " + generalLocation.Value.y);
            entityArray.Dispose();
            return generalEntity;
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

        public static NativeArray<Translation> SetPossibleValidLocation(NativeArray<Translation> cellPositionArray, float3 fiveStarGeneralTranslation)
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

        public static BlitzkriegFormation GetBlitzFormation(NativeArray<Entity> foundPieceEntitiesArray)
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
    }
}
