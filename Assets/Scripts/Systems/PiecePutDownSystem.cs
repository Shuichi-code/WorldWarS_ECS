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

        EntityQuery pieceOnCellQuery = GetEntityQuery(typeof(PieceOnCellComponent), typeof(CellComponent));
        NativeArray<Entity> pieceOnCellArray = pieceOnCellQuery.ToEntityArray(Allocator.TempJob);
        ComponentDataFromEntity<Translation> cellTranslationDataArray = GetComponentDataFromEntity<Translation>();
        ComponentDataFromEntity<PieceComponent> pieceComponentDataArray = GetComponentDataFromEntity<PieceComponent>();

        EntityQuery allCellQuery = GetEntityQuery(typeof(CellComponent));
        NativeArray<Entity> cellEntityArray = allCellQuery.ToEntityArray(Allocator.TempJob);
        BufferFromEntity<CellNeighborBufferElement> cellNeighborBufferEntity = GetBufferFromEntity<CellNeighborBufferElement>();
        ComponentDataFromEntity<PieceOnCellComponent> pieceOnCellComponentDataArray = GetComponentDataFromEntity<PieceOnCellComponent>();

        EntityArchetype eventEntityArchetype = EntityManager.CreateArchetype(typeof(GameFinishedEventComponent));

        Entities.
            WithAll<SelectedTag>().
            ForEach((Entity pieceEntity, ref Translation translation, ref PieceComponent piece)=> {
                if (Input.GetMouseButtonUp(0))
                {
                    float pieceXCoordinate = (float)Math.Round(translation.Value.x);
                    float pieceYCoordinate = (float)Math.Round(translation.Value.y);

                    float originalPieceXCoordinate = (float)Math.Round(piece.originalCellPosition.x);
                    float originalPieceYCoordinate = (float)Math.Round(piece.originalCellPosition.y);

                    var foundMove = false;

                    Color teamColorToMove = gameManagerArray[gameManagerEntity].teamToMove;

                    //iterate on all of the possible cells
                    for (int cellIndex = 0; cellIndex < cellEntityArray.Length; cellIndex++)
                    {
                        //get the neighbor buffer of the original cell that the dragged piece was on
                        Entity currentCellEntity = cellEntityArray[cellIndex];
                        Translation currentCellTranslation = cellTranslationDataArray[currentCellEntity];
                        if (piece.originalCellPosition.x == currentCellTranslation.Value.x && piece.originalCellPosition.y == currentCellTranslation.Value.y )
                        {
                            //iterate on all neighbor cell of the original cell
                            DynamicBuffer<CellNeighborBufferElement> cellNeighborBuffer = cellNeighborBufferEntity[cellEntityArray[cellIndex]];
                            for (int cellNeighborIndex = 0; cellNeighborIndex < cellNeighborBuffer.Length; cellNeighborIndex++)
                            {
                                //if piece lands on a neighbor cell with no piece on it, the move is valid
                                Entity cellNeighborEntity = cellNeighborBuffer[cellNeighborIndex].cellNeighbor;
                                Translation cellNeighborTranslation = cellTranslationDataArray[cellNeighborEntity];

                                if (cellNeighborTranslation.Value.x == pieceXCoordinate &&
                                cellNeighborTranslation.Value.y == pieceYCoordinate &&
                                (!HasComponent<PieceOnCellComponent>(cellNeighborEntity)|| HasComponent<EnemyCellTag>(cellNeighborEntity)
                                ))
                                {
                                    //or if piece lands on an enemy cell then the move is valid and combat occurs
                                    if (HasComponent<EnemyCellTag>(cellNeighborEntity))
                                    {
                                        //pass the pieces to the arbiter entity to determine combat winner
                                        int cellNeighborPieceRank = pieceComponentDataArray[pieceOnCellComponentDataArray[cellNeighborEntity].pieceEntity].pieceRank;
                                        Entity arbiter = ecb.CreateEntity();
                                        ecb.AddComponent<ArbiterComponent>(arbiter);
                                        ecb.SetComponent(arbiter, new ArbiterComponent {
                                            attackingPieceEntity = pieceEntity,
                                            defendingPieceEntity = pieceOnCellComponentDataArray[cellNeighborEntity].pieceEntity,
                                            cellBattleGround = cellNeighborEntity
                                        });
                                    }

                                    foundMove = true;
                                    translation.Value.x = pieceXCoordinate;
                                    translation.Value.y = pieceYCoordinate;

                                    //Remove the PieceOnCellComponent from the original cell
                                    ecb.RemoveComponent<PieceOnCellComponent>(currentCellEntity);

                                    piece.originalCellPosition.x = pieceXCoordinate;
                                    piece.originalCellPosition.y = pieceYCoordinate;

                                    //Add the PieceOnCellComponent on the new cell and add this pieceentity as reference
                                    ecb.AddComponent<PieceOnCellComponent>(cellNeighborEntity);
                                    ecb.SetComponent(cellNeighborEntity, new PieceOnCellComponent
                                    {
                                        pieceEntity = pieceEntity
                                    });

                                    //if piece is a flag and lands on the other side of the board, player wins
                                    Color pieceOnCellColor = piece.teamColor;
                                    if ((HasComponent<LastCellsForBlackTag>(cellNeighborEntity) && pieceOnCellColor == Color.black) ||
                                    (HasComponent<LastCellsForWhiteTag>(cellNeighborEntity) && pieceOnCellColor == Color.white))
                                    {
                                        //player wins
                                        Entity eventEntity = ecb.CreateEntity(eventEntityArchetype);
                                        ecb.SetComponent<GameFinishedEventComponent>(eventEntity, new GameFinishedEventComponent { winningTeamColor = pieceOnCellColor });
                                    }

                                    //Change the teamcolor to the other team
                                    //Debug.Log("Changing team turn!");
                                    teamColorToMove = gameManagerArray[gameManagerEntity].teamToMove == Color.white ? Color.black : Color.white;
                                    break;
                                }
                                //if the piece lands in an invalid cell, return to original cell
                                if (!foundMove)
                                {
                                    translation.Value.x = piece.originalCellPosition.x;
                                    translation.Value.y = piece.originalCellPosition.y;
                                }
                            }
                        }
                    }

                    //Set the GameManager to let the other team move
                    ecb.SetComponent<GameManagerComponent>(gameManagerEntity, new GameManagerComponent
                    {
                        isDragging = false,
                        state = State.Playing,
                        teamToMove = teamColorToMove
                    });
                    ecb.RemoveComponent<SelectedTag>(pieceEntity);
                }
        }).Run();
        pieceOnCellArray.Dispose();
        cellEntityArray.Dispose();
    }
}
