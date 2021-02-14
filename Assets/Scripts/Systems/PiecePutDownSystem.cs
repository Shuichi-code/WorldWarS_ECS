using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(CheckCellStateSystem))]
public class PiecePutDownSystem : SystemBase
{
    private EntityCommandBufferSystem entityCommandBuffer;

    protected override void OnStartRunning()
    {
        base.OnCreate();
        entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        var ecb = entityCommandBuffer.CreateCommandBuffer();
        EntityQuery gameManagerQuery = GetEntityQuery(typeof(GameManagerComponent));
        Entity gameManagerEntity = gameManagerQuery.GetSingletonEntity();
        ComponentDataFromEntity<GameManagerComponent> gameManagerArray = GetComponentDataFromEntity<GameManagerComponent>();

        EntityQuery highlightedTagQuery = GetEntityQuery(ComponentType.ReadOnly<HighlightedTag>());
        EntityQuery enemyCellQuery = GetEntityQuery(ComponentType.ReadOnly<EnemyCellTag>());

        NativeArray<Entity> highLightedCells = highlightedTagQuery.ToEntityArray(Allocator.TempJob);
        NativeArray<Entity> enemyCells = enemyCellQuery.ToEntityArray(Allocator.TempJob);

        EntityQuery pieceOnCellQuery = GetEntityQuery(typeof(PieceOnCellComponent), typeof(CellComponent));
        NativeArray<Entity> pieceOnCellArray = pieceOnCellQuery.ToEntityArray(Allocator.TempJob);
        ComponentDataFromEntity<Translation> cellTranslationDataArray = GetComponentDataFromEntity<Translation>();

        EntityQuery allCellQuery = GetEntityQuery(typeof(CellComponent), typeof(Translation));
        NativeArray<Entity> cellEntityArray = allCellQuery.ToEntityArray(Allocator.TempJob);
        NativeArray<Translation> cellTranslationArray = allCellQuery.ToComponentDataArray<Translation>(Allocator.TempJob);

        //TODO: Add functionality to change the piececomponent reference of the cell to the new one
        Entities.
            WithAll<SelectedTag, PieceTag>().
            ForEach((Entity pieceEntity, ref Translation translation, ref PieceComponent piece)=> {
                if (Input.GetMouseButtonUp(0))
                {
                    float pieceXCoordinate = (float)Math.Round(translation.Value.x);
                    float pieceYCoordinate = (float)Math.Round(translation.Value.y);

                    float originalPieceXCoordinate = (float)Math.Round(piece.originalCellPosition.x);
                    float originalPieceYCoordinate = (float)Math.Round(piece.originalCellPosition.y);

                    //Declare the list of allowable moves
                    NativeArray<float3> allowableCellArray = new NativeArray<float3>(4, Allocator.Temp);
                    //surroundCellList.Add(new float3(dragPiecePosition.Value.x - 1, dragPiecePosition.Value.y + 1, dragPiecePosition.Value.z));
                    allowableCellArray[0] = new float3(originalPieceXCoordinate, originalPieceYCoordinate + 1f, translation.Value.z);
                    //surroundCellList.Add(new float3(dragPiecePosition.Value.x + 1, dragPiecePosition.Value.y + 1, dragPiecePosition.Value.z));
                    allowableCellArray[1] = new float3(originalPieceXCoordinate + 1f, originalPieceYCoordinate, translation.Value.z);
                    //surroundCellList.Add(new float3(dragPiecePosition.Value.x + 1, dragPiecePosition.Value.y - 1, dragPiecePosition.Value.z));
                    allowableCellArray[2] = new float3(originalPieceXCoordinate, originalPieceYCoordinate - 1f, translation.Value.z);
                    //surroundCellList.Add(new float3(dragPiecePosition.Value.x - 1, dragPiecePosition.Value.y - 1, dragPiecePosition.Value.z));
                    allowableCellArray[3] = new float3(originalPieceXCoordinate - 1f, originalPieceYCoordinate, translation.Value.z);

                    for (int i = 0; i < allowableCellArray.Length; i++)
                    {
                        //if the piece lands in a valid cell, place it on the valid cell
                        if (allowableCellArray[i].x == pieceXCoordinate && allowableCellArray[i].y == pieceYCoordinate)
                        {
                            translation.Value.x = pieceXCoordinate;
                            translation.Value.y = pieceYCoordinate;

                            //Remove the PieceOnCellComponent from the original cell
                            for (int pieceOnCellIndex = 0; pieceOnCellIndex < pieceOnCellArray.Length; pieceOnCellIndex++)
                            {
                                Translation cellPosition = cellTranslationDataArray[pieceOnCellArray[pieceOnCellIndex]];
                                if (piece.originalCellPosition.x == cellPosition.Value.x && piece.originalCellPosition.y == cellPosition.Value.y)
                                {
                                    ecb.RemoveComponent<PieceOnCellComponent>(pieceOnCellArray[pieceOnCellIndex]);
                                    break;
                                }
                            }

                            piece.originalCellPosition.x = pieceXCoordinate;
                            piece.originalCellPosition.y = pieceYCoordinate;

                            //Add the PieceOnCellComponent on the new cell and add this pieceentity as reference
                            for (int cellIndex = 0; cellIndex < cellTranslationArray.Length; cellIndex++)
                            {
                                if(translation.Value.x == cellTranslationArray[cellIndex].Value.x && translation.Value.y == cellTranslationArray[cellIndex].Value.y)
                                {
                                    Entity cellEntity = cellEntityArray[cellIndex];
                                    ecb.AddComponent<PieceOnCellComponent>(cellEntity);
                                    ecb.SetComponent(cellEntity, new PieceOnCellComponent
                                    {
                                        piece = pieceEntity
                                    });
                                }
                            }

                            //Change the teamcolor to the other team
                            ecb.SetComponent<GameManagerComponent>(gameManagerEntity, new GameManagerComponent {
                                isDragging = false,
                                state = GameManagerComponent.State.Playing,
                                teamToMove = gameManagerArray[gameManagerEntity].teamToMove == Color.black ? Color.white : Color.black
                             });
                            break;
                        }
                        //if the piece lands in an invalid cell, return to original cell
                        else
                        {
                            translation.Value.x = piece.originalCellPosition.x;
                            translation.Value.y = piece.originalCellPosition.y;
                            ecb.SetComponent<GameManagerComponent>(gameManagerEntity, new GameManagerComponent
                            {
                                isDragging = false,
                                state = GameManagerComponent.State.Playing,
                                teamToMove = gameManagerArray[gameManagerEntity].teamToMove
                            });
                        }
                    }
                    ecb.RemoveComponent<SelectedTag>(pieceEntity);
                }
        }).Run();
        highLightedCells.Dispose();
        enemyCells.Dispose();
        pieceOnCellArray.Dispose();
        cellEntityArray.Dispose();
        cellTranslationArray.Dispose();
    }
}
