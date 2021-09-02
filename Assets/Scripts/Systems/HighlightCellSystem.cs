using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Systems;
using Assets.Scripts.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class HighlightCellSystem : ParallelSystem
{

    protected override void OnUpdate()
    {
        var pieceQuery = GetEntityQuery(ComponentType.ReadOnly<SelectedTag>(), ComponentType.ReadOnly<OriginalLocationComponent>(), ComponentType.ReadOnly<TeamComponent>());
        if (pieceQuery.CalculateEntityCount() == 0) return;

        var selectedPieceTranslation = pieceQuery.GetSingleton<OriginalLocationComponent>().originalLocation;
        var selectedPieceTeam = pieceQuery.GetSingleton<TeamComponent>().myTeam;


        var ecb = EcbSystem.CreateCommandBuffer().AsParallelWriter();
        var cellArrayPositions = new NativeArray<float3>(4, Allocator.TempJob);
        cellArrayPositions = SetPossibleValidMoves(selectedPieceTranslation, cellArrayPositions);


        Entities
            .WithAll<CellTag>()
            .ForEach((Entity e, int entityInQueryIndex, in Translation cellTranslation) =>
            {

                var i = 0;
                while (i < cellArrayPositions.Length)
                {
                    if (Location.IsMatchLocation(cellArrayPositions[i], cellTranslation.Value))
                    {
                        if (!HasComponent<PieceOnCellComponent>(e))
                        {
                            if (!HasComponent<HighlightedTag>(e))
                            {
                                Tag.AddTag<HighlightedTag>(ecb, entityInQueryIndex, e);
                            }
                        }

                        else
                        {
                            var pieceOnCellArray = GetComponentDataFromEntity<PieceOnCellComponent>(true);
                            var teamArray = GetComponentDataFromEntity<TeamComponent>(true);
                            var cellPieceTeam = teamArray[pieceOnCellArray[e].PieceEntity].myTeam;
                            if (selectedPieceTeam != cellPieceTeam)
                            {
                                if (!HasComponent<EnemyCellTag>(e))
                                {
                                    Tag.AddTag<EnemyCellTag>(ecb, entityInQueryIndex, e);
                                }
                            }
                        }
                    }
                    i++;
                }
            }).WithDisposeOnCompletion(cellArrayPositions).Schedule();
            CompleteDependency();
            //EcbSystem.AddJobHandleForProducer(Dependency);
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