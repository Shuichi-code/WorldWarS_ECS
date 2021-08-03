using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class HighlightCellSystem : SystemBase
{
    private EntityCommandBufferSystem ecbSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        // Find the ECB system once and store it for later usage
        ecbSystem = World
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var mouseButtonUp = Input.GetMouseButtonUp(0);
        var pieceQuery = GetEntityQuery(ComponentType.ReadOnly<SelectedTag>(), ComponentType.ReadOnly<OriginalLocationComponent>(),ComponentType.ReadOnly<TeamComponent>());
        if (pieceQuery.CalculateEntityCount() == 0)
            return;

        float3 selectedPieceTranslation = pieceQuery.GetSingleton<OriginalLocationComponent>().originalLocation;
        Team selectedPieceTeam = pieceQuery.GetSingleton<TeamComponent>().myTeam;

        var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();

        Entities
            .WithAll<CellTag>()
            .ForEach((Entity e, int entityInQueryIndex, in Translation cellTranslation) =>
            {
                NativeArray<float3> cellArrayPositions = new NativeArray<float3>(4, Allocator.Temp);
                cellArrayPositions = SetPossibleValidMoves(selectedPieceTranslation, cellArrayPositions);
                int i = 0;
                while (i < cellArrayPositions.Length)
                {
                    if (Location.IsMatchLocation(cellArrayPositions[i], cellTranslation.Value))
                    {
                        if (!HasComponent<PieceOnCellComponent>(e))
                            Tag.AddTag<HighlightedTag>(ecb, entityInQueryIndex, e);
                        else
                        {
                            var cellPieceTeam = GetComponent<TeamComponent>(GetComponent<PieceOnCellComponent>(e).PieceEntity).myTeam;
                            if (selectedPieceTeam != cellPieceTeam)
                                Tag.AddTag<EnemyCellTag>(ecb, entityInQueryIndex, e);
                        }
                    }
                    i++;
                }
                cellArrayPositions.Dispose();
            }).ScheduleParallel();
        ecbSystem.AddJobHandleForProducer(this.Dependency);
    }

    private static NativeArray<float3> SetPossibleValidMoves(float3 selectedCellTranslation, NativeArray<float3> cellArrayPositions)
    {
        cellArrayPositions[0] = new float3(selectedCellTranslation.x, selectedCellTranslation.y + 1, selectedCellTranslation.z);
        cellArrayPositions[1] = new float3(selectedCellTranslation.x, selectedCellTranslation.y - 1, selectedCellTranslation.z);
        cellArrayPositions[2] = new float3(selectedCellTranslation.x - 1, selectedCellTranslation.y, selectedCellTranslation.z);
        cellArrayPositions[3] = new float3(selectedCellTranslation.x + 1, selectedCellTranslation.y, selectedCellTranslation.z);
        return cellArrayPositions;
    }
}