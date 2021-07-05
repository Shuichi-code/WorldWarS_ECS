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
        bool mouseButtonUp = Input.GetMouseButtonUp(0);
        var pieceQuery = GetEntityQuery(ComponentType.ReadOnly<SelectedTag>(), ComponentType.ReadOnly<PieceComponent>());
        if (pieceQuery.CalculateEntityCount() == 0)
            return;

        float3 selectedPieceTranslation = pieceQuery.GetSingleton<PieceComponent>().originalCellPosition;
        Team selectedPieceTeam = pieceQuery.GetSingleton<PieceComponent>().team;

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
                            Tag.TagCellAsHighlighted(ecb, entityInQueryIndex, e);
                        else
                        {
                            Team cellPieceTeam = GetComponent<PieceComponent>(GetComponent<PieceOnCellComponent>(e).PieceEntity).team;
                            if (selectedPieceTeam != cellPieceTeam)
                                Tag.TagCellAsEnemy(ecb, entityInQueryIndex, e);
                        }
                    }
                    i++;
                }
                cellArrayPositions.Dispose();
            }).ScheduleParallel();
        //this.CompleteDependency();
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