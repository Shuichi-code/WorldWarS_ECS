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
    private EntityQuery selectedPieceQuery;
    private Entity dragPieceEntity;
    PieceComponent dragPiece;
    Translation dragPiecePosition;

    protected override void OnCreate()
    {
        base.OnStartRunning();
        entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityQuery gameManagerQuery = GetEntityQuery(typeof(GameManagerComponent));
        Entity gameManagerEntity = gameManagerQuery.GetSingletonEntity();
        ComponentDataFromEntity<GameManagerComponent> gameManagerArray = GetComponentDataFromEntity<GameManagerComponent>();

        EntityQuery selectedPieceQuery = GetEntityQuery(typeof(SelectedTag));
        Entities.ForEach((ref Translation translation)=> {
            if (gameManagerArray[gameManagerEntity].isDragging)
            {
            //get entity that is being dragged

            }
        }).Run();

    }
    /*
    EntityQuery gameManagerQuery = GetEntityQuery(typeof(GameManagerComponent));
    Entity gameManagerEntity = gameManagerQuery.GetSingletonEntity();
    ComponentDataFromEntity<GameManagerComponent> gameManagerArray = GetComponentDataFromEntity<GameManagerComponent>();
    var ecb = entityCommandBuffer.CreateCommandBuffer();
    //var ecb = new EntityCommandBuffer(Allocator.TempJob);
    selectedPieceQuery = GetEntityQuery(typeof(SelectedTag));
        if (gameManagerArray[gameManagerEntity].isDragging)
        {
            Entity dragPieceEntity = selectedPieceQuery.GetSingletonEntity();
    ComponentDataFromEntity<Translation> dragPiecePositionArray = GetComponentDataFromEntity<Translation>();
    ComponentDataFromEntity<PieceComponent> dragPieceArray = GetComponentDataFromEntity<PieceComponent>();
    Translation dragPiecePosition = dragPiecePositionArray[dragPieceEntity];
    PieceComponent dragPiece = dragPieceArray[dragPieceEntity];
}

Entities.WithoutBurst().WithAll<CellComponent>().
    ForEach((Entity cell, in Translation translation) =>
    {
                //Debug.Log("Checking each cell!");
                if (gameManagerArray[gameManagerEntity].isDragging)
        {
                    //Debug.Log("Translation data is: "+dragPiecePosition);
                    float3[] surroundCellArray = new float3[4];
                    //surroundCellList.Add(new float3(dragPiecePosition.Value.x - 1, dragPiecePosition.Value.y + 1, dragPiecePosition.Value.z));
                    surroundCellArray[0] = new float3(dragPiecePosition.Value.x, dragPiecePosition.Value.y + 1, dragPiecePosition.Value.z);
                    //surroundCellList.Add(new float3(dragPiecePosition.Value.x + 1, dragPiecePosition.Value.y + 1, dragPiecePosition.Value.z));
                    surroundCellArray[1] = new float3(dragPiecePosition.Value.x + 1, dragPiecePosition.Value.y, dragPiecePosition.Value.z);
                    //surroundCellList.Add(new float3(dragPiecePosition.Value.x + 1, dragPiecePosition.Value.y - 1, dragPiecePosition.Value.z));
                    surroundCellArray[2] = new float3(dragPiecePosition.Value.x, dragPiecePosition.Value.y - 1, dragPiecePosition.Value.z);
                    //surroundCellList.Add(new float3(dragPiecePosition.Value.x - 1, dragPiecePosition.Value.y - 1, dragPiecePosition.Value.z));
                    surroundCellArray[3] = new float3(dragPiecePosition.Value.x - 1, dragPiecePosition.Value.y, dragPiecePosition.Value.z);

                    //NativeArray<float3> surroundCellNativeArray = new NativeArray<float3>(surroundCellArray, Allocator.Temp);
                    CellComponent cellComponent = GetComponent<CellComponent>(cell);

                    //Validate if the surrounding cells
                    foreach (float3 cellPos in surroundCellArray)
            {
                        //remove the highlighted tag if there is a ally cell next to the piece
                        if (cellPos.x == translation.Value.x && cellPos.y == translation.Value.y)
                {
                    if (cellComponent.pieceColor == dragPiece.teamColor)
                    {
                        return;
                    }
                    else if (cellComponent.pieceColor == Color.clear)
                    {
                        if (!HasComponent<HighlightedTag>(cell))
                        {
                            ecb.AddComponent<HighlightedTag>(cell);
                        }
                    }
                    else
                    {
                        if (!HasComponent<EnemyCellTag>(cell))
                        {
                            ecb.AddComponent<EnemyCellTag>(cell);
                        }
                    }
                }
                else
                {
                            //Debug.Log("No longer holding the mousebutton.");
                            if (HasComponent<HighlightedTag>(cell))
                    {
                        ecb.RemoveComponent<HighlightedTag>(cell);
                    }
                    if (HasComponent<EnemyCellTag>(cell))
                    {
                        ecb.RemoveComponent<EnemyCellTag>(cell);
                    }
                }
                        //surroundCellNativeArray.Dispose();
                    }
        }
    }).Run();
        //ecb.Playback(EntityManager);
        //ecb.Dispose();*/
}

