using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

//[UpdateAfter(typeof(PieceMovementSystem))]
public class CheckCellStateSystem : SystemBase
{
    private EntityCommandBufferSystem entityCommandBuffer;

    protected override void OnCreate()
    {
        base.OnStartRunning();
        entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        #region Initializing Data
        EntityQuery gameManagerQuery = GetEntityQuery(typeof(GameManagerComponent));
        Entity gameManagerEntity = gameManagerQuery.GetSingletonEntity();
        ComponentDataFromEntity<GameManagerComponent> gameManagerArray = GetComponentDataFromEntity<GameManagerComponent>();

        //getarray of all Cells
        EntityQuery cellQuery = GetEntityQuery(ComponentType.ReadOnly<CellComponent>(),ComponentType.ReadOnly<Translation>());
        NativeArray<Entity> cellArray = cellQuery.ToEntityArray(Allocator.TempJob);
        NativeArray<Translation> cellArrayPositions = cellQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        BufferFromEntity<CellNeighborBufferElement> cellNeighborBufferEntity = GetBufferFromEntity<CellNeighborBufferElement>();

        EntityQuery pieceQuery = GetEntityQuery(typeof(PieceOnCellComponent));
        ComponentDataFromEntity<PieceOnCellComponent> pieceOnCellArray = GetComponentDataFromEntity<PieceOnCellComponent>();
        ComponentDataFromEntity<PieceComponent> pieceArray = GetComponentDataFromEntity<PieceComponent>();
        ComponentDataFromEntity<Translation> translationArray = GetComponentDataFromEntity<Translation>();

        var ecb = entityCommandBuffer.CreateCommandBuffer();
        #endregion
        Entities.
            ForEach((in SelectedTag selected, in PieceComponent dragPiece, in Translation dragPieceTranslation)=> {
                int cellIndex = 0;
                while (cellIndex < cellArrayPositions.Length)
                {
                    //Get the cell where the piece is currently staying
                    if(dragPiece.originalCellPosition.x == cellArrayPositions[cellIndex].Value.x && dragPiece.originalCellPosition.y == cellArrayPositions[cellIndex].Value.y)
                    //if (PiecePutDownSystem.IsFloatSameTranslation(dragPiece.originalCellPosition, cellArrayPositions[cellIndex]))
                    {
                        //Get the neighbors
                        DynamicBuffer<CellNeighborBufferElement> cellNeighborBuffer = cellNeighborBufferEntity[cellArray[cellIndex]];
                        //Check if the cell is empty, if it is add a highlightedtag
                        int cellNeighborIndex = 0;
                        while (cellNeighborIndex < cellNeighborBuffer.Length)
                        {
                            Entity cellNeighborEntity = cellNeighborBuffer[cellNeighborIndex].cellNeighbor;

                            if (!HasComponent<PieceOnCellComponent>(cellNeighborEntity))
                            {
                                //highlight the cells
                                ecb.AddComponent<HighlightedTag>(cellNeighborEntity);
                            }
                            else
                            {
                                //Check if the cell has an enemy piece, if it is add a selected enemy tag
                                Entity piece = pieceOnCellArray[cellNeighborEntity].pieceEntity;
                                if (pieceArray[piece].teamColor != dragPiece.teamColor)
                                {
                                    ecb.AddComponent<EnemyCellTag>(cellNeighborEntity);
                                }
                            }
                            cellNeighborIndex++;
                        }
                    }
                    cellIndex++;
                }
            }).Run();

        cellArray.Dispose();
        cellArrayPositions.Dispose();
    }
}

