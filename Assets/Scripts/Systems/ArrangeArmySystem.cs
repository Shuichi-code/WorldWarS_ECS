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
            bool mouseButtonPressed = Input.GetMouseButtonDown(0);
            float3 roundedWorldPos = Location.GetRoundedMousePosition();
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
            var ecbRun = ecbSystem.CreateCommandBuffer();

            var highlightedCellQuery = GetEntityQuery(ComponentType.ReadOnly<CellTag>(),
                ComponentType.ReadOnly<HighlightedTag>(), ComponentType.ReadOnly<Translation>());
            var highlightedPieceQuery = GetEntityQuery(ComponentType.ReadOnly<PieceComponent>(),
                ComponentType.ReadOnly<HighlightedTag>(), typeof(Translation));

            Entities.
                WithAny<CellTag, PieceComponent>().
                ForEach((Entity cellEntity, int entityInQueryIndex, in Translation cellTranslation) =>
                {
                    var pieceRoundedLocation = math.round(cellTranslation.Value);
                    if (Location.IsMatchLocation(pieceRoundedLocation, roundedWorldPos) && mouseButtonPressed && !HasComponent<HighlightedTag>(cellEntity))
                    {
                        Tag.TagCellAsHighlighted(ecb, entityInQueryIndex, cellEntity);
                    }
                }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(this.Dependency);

            if (highlightedCellQuery.CalculateEntityCount() <= 1) return;
            var cellTranslations = highlightedCellQuery.ToComponentDataArray<Translation>(Allocator.Temp);
            var pieceTranslations = highlightedPieceQuery.ToComponentDataArray<Translation>(Allocator.Temp);

            if (highlightedPieceQuery.CalculateEntityCount() != 0)
            {
                var firstTranslation = highlightedPieceQuery.CalculateEntityCount() < 2 ? cellTranslations[0] : pieceTranslations[0];
                var secondTranslation = highlightedPieceQuery.CalculateEntityCount() < 2 ? cellTranslations[1] : pieceTranslations[1];

                SwapPieces(firstTranslation, secondTranslation, ecb);
            }
            RemoveHighlightedEntities(ecb);
        }

        private void SwapPieces(Translation firstCellTranslation, Translation secondCellTranslation, EntityCommandBuffer.ParallelWriter ecb)
        {
            Entities.WithAll<PieceComponent, HighlightedTag>().ForEach(
                (Entity pieceEntity, int entityInQueryIndex, ref Translation pieceTranslation) =>
                {
                    var newPieceLocation = GetNewPieceLocation(firstCellTranslation, secondCellTranslation, pieceTranslation);

                    ecb.SetComponent(entityInQueryIndex, pieceEntity, new Translation
                    {
                        Value = newPieceLocation
                    });
                }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(this.Dependency);
        }

        private static float3 GetNewPieceLocation(Translation firstTranslation, Translation secondTranslation, Translation pieceTranslation)
        {
            return Location.IsMatchLocation(pieceTranslation.Value, firstTranslation.Value)
                ? secondTranslation.Value
                : firstTranslation.Value;
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
