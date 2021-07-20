using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Monobehaviours.Managers;
using Assets.Scripts.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class ArrangeArmySystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem ecbSystem;
        private EntityManager entityManager;

        protected override void OnCreate()
        {
            base.OnCreate();
            // Find the ECB system once and store it for later usage
            ecbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            entityManager = World.EntityManager;
        }

        protected override void OnUpdate()
        {
            var mouseButtonPressed = Input.GetMouseButtonDown(0);
            var roundedWorldPos = Location.GetRoundedMousePosition();
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();

            var highlightedCellQuery = GetEntityQuery(ComponentType.ReadOnly<CellTag>(), ComponentType.ReadOnly<HighlightedTag>(), ComponentType.ReadOnly<Translation>());
            var highlightedPieceQuery = GetEntityQuery(ComponentType.ReadOnly<PieceComponent>(), ComponentType.ReadOnly<HighlightedTag>(), typeof(Translation));

            HighlightClickedEntities(roundedWorldPos, mouseButtonPressed, ecb);

            if (highlightedCellQuery.CalculateEntityCount() <= 1) return;
            var cellTranslations = highlightedCellQuery.ToComponentDataArray<Translation>(Allocator.Temp);
            var pieceEntities = highlightedPieceQuery.ToEntityArray(Allocator.Temp);

            if (highlightedPieceQuery.CalculateEntityCount() != 0)
            {
                var firstTranslation = cellTranslations[0];
                var secondTranslation = cellTranslations[1];

                var firstEntity = pieceEntities[0];
                var secondEntity = highlightedPieceQuery.CalculateEntityCount() < 2 ? Entity.Null : pieceEntities[1];

                SwapPieces(firstTranslation, secondTranslation, ecb);
                UpdatePieceOnCellComponents(firstTranslation, secondEntity, firstEntity, ecb);
            }
            RemoveHighlightedEntities(ecb);
        }

        private void UpdatePieceOnCellComponents(Translation firstTranslation, Entity secondEntity, Entity firstEntity, EntityCommandBuffer.ParallelWriter ecb)
        {
            Entities.WithAll<CellTag, HighlightedTag>().ForEach(
                (Entity cellEntity, int entityInQueryIndex, in Translation cellTranslation) =>
                {
                    if (secondEntity != Entity.Null)
                    {
                        var pieceEntity = GetSwapEntity(cellTranslation, firstTranslation, secondEntity, firstEntity);

                        ecb.SetComponent<PieceOnCellComponent>(entityInQueryIndex, cellEntity, new PieceOnCellComponent
                        {
                            PieceEntity = pieceEntity
                        });
                    }
                    else
                    {
                        if (Location.IsMatchLocation(cellTranslation.Value, firstTranslation.Value))
                            ecb.RemoveComponent<PieceOnCellComponent>(entityInQueryIndex, cellEntity);

                        else
                            ecb.AddComponent<PieceOnCellComponent>(entityInQueryIndex, cellEntity, new PieceOnCellComponent
                            {
                                PieceEntity = firstEntity
                            });
                    }
                }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(this.Dependency);
        }


        private void HighlightClickedEntities(float3 roundedWorldPos, bool mouseButtonPressed, EntityCommandBuffer.ParallelWriter ecb)
        {
            var playerTeam = GetEntityQuery(ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<TeamComponent>()).GetSingleton<TeamComponent>().myTeam;
            Entities.WithAny<CellTag, PieceComponent, HomeCellComponent>().ForEach(
                (Entity cellEntity, int entityInQueryIndex, in Translation cellTranslation) =>
                {
                    var pieceTeam = HasComponent<PieceComponent>(cellEntity) ? GetComponent<TeamComponent>(cellEntity).myTeam : Team.Null;
                    var cellTeam = HasComponent<HomeCellComponent>(cellEntity)
                        ? GetComponent<HomeCellComponent>(cellEntity).homeTeam
                        : Team.Null;

                    var pieceRoundedLocation = math.round(cellTranslation.Value);
                    if (Location.IsMatchLocation(pieceRoundedLocation, roundedWorldPos) && mouseButtonPressed &&
                        !HasComponent<HighlightedTag>(cellEntity) && (playerTeam == pieceTeam || playerTeam == cellTeam))
                    {
                        Tag.TagCellAsHighlighted(ecb, entityInQueryIndex, cellEntity);
                    }
                }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(this.Dependency);
        }

        private void SwapPieces(Translation firstEntityTranslation, Translation secondEntityTranslation, EntityCommandBuffer.ParallelWriter ecb)
        {
            Entities.WithAll<PieceComponent, HighlightedTag>().ForEach(
                (Entity pieceEntity, int entityInQueryIndex, ref Translation pieceTranslation) =>
                {
                    var newPieceLocation = GetNewPieceLocation(firstEntityTranslation, secondEntityTranslation, pieceTranslation);

                    ecb.SetComponent(entityInQueryIndex, pieceEntity, new Translation {Value = newPieceLocation});
                    ecb.SetComponent(entityInQueryIndex, pieceEntity, new OriginalLocationComponent{ originalLocation = newPieceLocation});

                }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(this.Dependency);
        }

        private static float3 GetNewPieceLocation(Translation firstTranslation, Translation secondTranslation, Translation pieceTranslation)
        {
            return Location.IsMatchLocation(pieceTranslation.Value, firstTranslation.Value)
                ? secondTranslation.Value
                : firstTranslation.Value;
        }

        private static Entity GetSwapEntity(Translation cellTranslation, Translation firstTranslation, Entity secondEntity, Entity firstEntity)
        {
            return Location.IsMatchLocation(cellTranslation.Value, firstTranslation.Value)
                ? secondEntity
                : firstEntity;
        }

        private void RemoveHighlightedEntities(EntityCommandBuffer.ParallelWriter ecb)
        {
            Entities.
                WithAll<HighlightedTag>().
                ForEach((Entity highlightedEntity, int entityInQueryIndex) =>
            {
                Tag.RemoveHighlightedTag(ecb, entityInQueryIndex, highlightedEntity);
            }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(this.Dependency);
        }
    }
}
