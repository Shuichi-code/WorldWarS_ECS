using System;
using System.Collections.Generic;
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
        #region Initializing Data
        var ecb = entityCommandBuffer.CreateCommandBuffer();
        EntityQuery gameManagerQuery = GetEntityQuery(typeof(GameManagerComponent));
        Entity gameManagerEntity = gameManagerQuery.GetSingletonEntity();
        ComponentDataFromEntity<GameManagerComponent> gameManagerArray = GetComponentDataFromEntity<GameManagerComponent>();

        EntityQuery pieceOnCellQuery = GetEntityQuery(typeof(PieceOnCellComponent), typeof(CellComponent));
        NativeArray<Entity> pieceOnCellArray = pieceOnCellQuery.ToEntityArray(Allocator.TempJob);
        ComponentDataFromEntity<Translation> cellTranslationDataArray = GetComponentDataFromEntity<Translation>();
        ComponentDataFromEntity<PieceComponent> pieceComponentDataArray = GetComponentDataFromEntity<PieceComponent>();

        EntityQuery allCellQuery = GetEntityQuery(typeof(CellComponent));
        NativeArray<Entity> cellEntityArray = allCellQuery.ToEntityArray(Allocator.TempJob);
        BufferFromEntity<CellNeighborBufferElement> cellNeighborBufferEntity = GetBufferFromEntity<CellNeighborBufferElement>();
        ComponentDataFromEntity<PieceOnCellComponent> pieceOnCellComponentDataArray = GetComponentDataFromEntity<PieceOnCellComponent>();

        EntityArchetype eventEntityArchetype = EntityManager.CreateArchetype(typeof(GameFinishedEventComponent));
        #endregion
        //TODO: Find a way to make this burst-safe. Cannot make this burst because burst does not support Math class in GetXandYCoordinate method
        Entities.
            WithoutBurst().
            WithAll<SelectedTag>().
            ForEach((Entity droppedPieceEntity, ref Translation droppedPieceTranslation, ref PieceComponent droppedPieceComponent)=> {
                if (Input.GetMouseButtonUp(0))
                {
                    float3 roundedDroppedPieceCoordinate = new float3((float)Math.Round(droppedPieceTranslation.Value.x), (float)Math.Round(droppedPieceTranslation.Value.y), 50);//ConvertToRoundedFloat3(droppedPieceTranslation.Value);

                    var foundMove = false;
                    Color teamColorToMove = gameManagerArray[gameManagerEntity].teamToMove;

                    //iterate on all of the possible cells
                    int cellIndex = 0;
                    while (cellIndex < cellEntityArray.Length)
                    {
                        //get the neighbor buffer of the original cell that the dragged piece was on
                        Entity currentCellEntity = cellEntityArray[cellIndex];
                        //Debug.Log("Iterating on all cells!");
                        if(IsFloatSameTranslation(droppedPieceComponent.originalCellPosition, cellTranslationDataArray[currentCellEntity]))
                        {
                            //iterate on all neighbor cell of the original cell
                            DynamicBuffer<CellNeighborBufferElement> cellNeighborBuffer = cellNeighborBufferEntity[currentCellEntity];
                            for (int cellNeighborIndex = 0; cellNeighborIndex < cellNeighborBuffer.Length; cellNeighborIndex++)
                            {
                                //if piece lands on a neighbor cell with no piece on it, the move is valid
                                Entity cellNeighborEntity = cellNeighborBuffer[cellNeighborIndex].cellNeighbor;
                                Translation cellNeighborTranslation = cellTranslationDataArray[cellNeighborEntity];

                                if (IsFloatSameTranslation(roundedDroppedPieceCoordinate, cellNeighborTranslation) &&
                                (!HasComponent<PieceOnCellComponent>(cellNeighborEntity) || HasComponent<EnemyCellTag>(cellNeighborEntity)))
                                {
                                    //or if piece lands on an enemy cell then the move is valid and combat occurs
                                    if (HasComponent<EnemyCellTag>(cellNeighborEntity))
                                    {
                                        //pass the pieces to the arbiter entity to determine combat winner
                                        int cellNeighborPieceRank = pieceComponentDataArray[pieceOnCellComponentDataArray[cellNeighborEntity].pieceEntity].pieceRank;
                                        Entity arbiter = ecb.CreateEntity();
                                        ecb.AddComponent<ArbiterComponent>(arbiter);
                                        ecb.SetComponent(arbiter, new ArbiterComponent
                                        {
                                            attackingPieceEntity = droppedPieceEntity,
                                            defendingPieceEntity = pieceOnCellComponentDataArray[cellNeighborEntity].pieceEntity,
                                            cellBattleGround = cellNeighborEntity
                                        });
                                    }
                                    Debug.Log("Found valid move!");
                                    foundMove = true;
                                    droppedPieceTranslation.Value = roundedDroppedPieceCoordinate;
                                    droppedPieceComponent.originalCellPosition = roundedDroppedPieceCoordinate;

                                    //Remove the PieceOnCellComponent from the original cell
                                    ecb.RemoveComponent<PieceOnCellComponent>(currentCellEntity);

                                    //Add the PieceOnCellComponent on the new cell and add this pieceentity as reference
                                    ecb.AddComponent<PieceOnCellComponent>(cellNeighborEntity);
                                    ecb.SetComponent(cellNeighborEntity, new PieceOnCellComponent
                                    {
                                        pieceEntity = droppedPieceEntity
                                    });

                                    //Change the teamcolor to the other team
                                    teamColorToMove = gameManagerArray[gameManagerEntity].teamToMove == Color.white ? Color.black : Color.white;
                                    break;
                                }
                                //if the piece lands in an invalid cell, return to original cell
                                if (!foundMove)
                                {
                                    Debug.Log("Could not find valid move");
                                    droppedPieceTranslation.Value = droppedPieceComponent.originalCellPosition;
                                }
                            }
                        }
                        cellIndex++;
                    }

                    //Set the GameManager to let the other team move
                    ecb.SetComponent<GameManagerComponent>(gameManagerEntity, new GameManagerComponent
                    {
                        isDragging = false,
                        state = State.Playing,
                        teamToMove = teamColorToMove
                    });
                    ecb.RemoveComponent<SelectedTag>(droppedPieceEntity);
                }
            }).Run();
        pieceOnCellArray.Dispose();
        cellEntityArray.Dispose();
    }

    /// <summary>
    /// Returns true if the translation of float3 and Translation match
    /// </summary>
    /// <param name="float3Data">Piece component of the dropped Piece Entity</param>
    /// <param name="cellTranslationDataArray">Array that allows to lookup the cell entity's translational data</param>
    /// <param name="cellEntityArray">Array that allow lookup of a cell entity</param>
    /// <param name="cellIndex"></param>
    /// <returns></returns>
    public static bool IsFloatSameTranslation(float3 float3Data, Translation translationData)
    {
        float[] float3CoordinateArray = GetXAndYCoordinates(float3Data);
        float[] translationCoordinateArray = GetXAndYCoordinates(translationData.Value);

        return ArraysEqual(float3CoordinateArray,translationCoordinateArray);
    }

    /// <summary>
    /// Returns an array of 2 rounded whole float values. The first is the x-coordinate, second is the y-coordinate
    /// </summary>
    /// <param name="float3Data"></param>
    /// <returns></returns>
    private static float[] GetXAndYCoordinates(float3 float3Data)
    {
        float[] XAndYArray = new float[2];
        XAndYArray[0] = (float)Math.Round(float3Data.x);
        XAndYArray[1] = (float)Math.Round(float3Data.y);

        return XAndYArray;
    }

    /// <summary>
    /// Returns true if all the values in each reference within the first array is equal to the value within the second array with respect to refence.
    /// </summary>
    /// <param name="firstArray"></param>
    /// <param name="secondArray"></param>
    /// <returns></returns>
    private static bool ArraysEqual(float[] firstArray, float[] secondArray)
    {
        return firstArray[0] == secondArray[0] && firstArray[1] == secondArray[1];
    }

    /// <summary>
    /// Returns a float3 with whole number values in the corresponding coordinates.
    /// </summary>
    /// <param name="float3Data"></param>
    /// <returns></returns>
    private static float3 ConvertToRoundedFloat3(float3 float3Data)
    {
        float[] roundedFloatArray = GetXAndYCoordinates(float3Data);
        float3 roundedFloat3Data = new float3(roundedFloatArray[0], roundedFloatArray[1], 50);

        return roundedFloat3Data;
    }
}
