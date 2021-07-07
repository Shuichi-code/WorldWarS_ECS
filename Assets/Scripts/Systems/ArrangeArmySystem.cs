using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Monobehaviours.Managers;
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
            Team playerTeam = GameManager.GetInstance().Player.Team;
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
            var ecbRun = ecbSystem.CreateCommandBuffer();
            Entities.
                WithAll<PieceComponent>().
                ForEach((Entity pieceEntity, int entityInQueryIndex, in Translation pieceTranslation, in PieceComponent piece) =>
                {
                    var pieceRoundedLocation = math.round(pieceTranslation.Value);
                    if (Location.IsMatchLocation(pieceRoundedLocation, roundedWorldPos) && piece.team == playerTeam && mouseButtonPressed && !HasComponent<HighlightedTag>(pieceEntity))
                    {
                        ecbRun.AddComponent<HighlightedTag>(pieceEntity);
                    }
                }).Run();

            var highlightedPiecesQuery = GetEntityQuery(ComponentType.ReadOnly<PieceComponent>(),
                ComponentType.ReadOnly<HighlightedTag>(), ComponentType.ReadOnly<Translation>());
            if (highlightedPiecesQuery.CalculateEntityCount() > 1)
            {
                SwapPiecePlaces(highlightedPiecesQuery);
                RemoveHighlightedPieces(ecb);
            }
        }

        private void SwapPiecePlaces(EntityQuery highlightedPiecesQuery)
        {
            var highlightedPieceTranslation = highlightedPiecesQuery.ToComponentDataArray<Translation>(Allocator.Temp);
            var highlightedEntityArray = highlightedPiecesQuery.ToEntityArray(Allocator.Temp);

            entityManager.SetComponentData(highlightedEntityArray[0], new Translation
            {
                Value = highlightedPieceTranslation[1].Value
            });
            entityManager.SetComponentData(highlightedEntityArray[1], new Translation
            {
                Value = highlightedPieceTranslation[0].Value
            });
        }

        private void RemoveHighlightedPieces(EntityCommandBuffer.ParallelWriter ecb)
        {
            Entities.WithAll<HighlightedTag>().ForEach((Entity pieceEntity, int entityInQueryIndex) =>
            {
                Tag.RemoveHighlightedTag(ecb, entityInQueryIndex, pieceEntity);
            }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(this.Dependency);
        }
    }
}
