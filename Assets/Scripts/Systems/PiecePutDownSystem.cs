using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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

        //TODO: Add functionality to change the piececomponent reference of the cell to the new one
        Entities.
            WithAll<SelectedTag, PieceTag>().
            ForEach((Entity entity, ref Translation translation, ref PieceComponent piece)=> {
                if (Input.GetMouseButtonUp(0))
                {
                    //Debug.Log(translation.Value);
                    float pieceXCoordinate = (float)Math.Round(translation.Value.x);
                    float pieceYCoordinate = (float)Math.Round(translation.Value.y);

                    float originalPieceXCoordinate = (float)Math.Round(piece.originalCellPosition.x);
                    float originalPieceYCoordinate = (float)Math.Round(piece.originalCellPosition.y);

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
                        if (allowableCellArray[i].x == pieceXCoordinate && allowableCellArray[i].y == pieceYCoordinate)
                        {
                            //Debug.Log(allowableCellArray[i]);
                            translation.Value.x = pieceXCoordinate;
                            translation.Value.y = pieceYCoordinate;

                            piece.originalCellPosition.x = pieceXCoordinate;
                            piece.originalCellPosition.y = pieceYCoordinate;
                            break;
                        }
                        else
                        {
                            translation.Value.x = piece.originalCellPosition.x;
                            translation.Value.y = piece.originalCellPosition.y;
                        }
                    }

                    ecb.RemoveComponent<SelectedTag>(entity);

                    ecb.SetComponent(gameManagerEntity,
                        new GameManagerComponent
                        {
                            isDragging = false,
                            state = GameManagerComponent.State.Playing
                        }
                    );
                }
        }).Run();
    }
}
