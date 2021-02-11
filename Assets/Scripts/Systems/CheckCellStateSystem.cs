using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(PieceMovementSystem))]
public class CheckCellStateSystem : SystemBase
{
    private EntityCommandBufferSystem entityCommandBuffer;
    protected override void OnCreate()
    {
        base.OnStartRunning();
        entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<PieceMovementSystem>().dragPieceEvent += OnDragPiece;
    }

    private void OnDragPiece(object sender, PieceMovementSystem.OnDragPressedEventArgs e)
    {

    }

    protected override void OnUpdate()
    {
            //Insert Code for checking which cells can be highlighted

            var ecb = entityCommandBuffer.CreateCommandBuffer();
            Entities.WithoutBurst().WithAll<CellComponent>().
                ForEach((Entity cell, in Translation translation) => {
                    //Debug.Log("Checking each cell!");
                    if (Input.GetMouseButtonDown(0) && BoardManager.GetInstance().isSelecting)
                    {
                        //Debug.Log("Left click is being held!");
                        EntityQuery pieceQuery = GetEntityQuery(typeof(SelectedTag));

                        Entity dragPiece = pieceQuery.GetSingletonEntity();
                        Translation dragPiecePosition = GetComponent<Translation>(dragPiece);
                        //Debug.Log("Checking!");
                        List<float3> surroundCellList = new List<float3>();
                        surroundCellList.Add(new float3(dragPiecePosition.Value.x - 1, dragPiecePosition.Value.y + 1, dragPiecePosition.Value.z));
                        surroundCellList.Add(new float3(dragPiecePosition.Value.x, dragPiecePosition.Value.y + 1, dragPiecePosition.Value.z));
                        surroundCellList.Add(new float3(dragPiecePosition.Value.x + 1, dragPiecePosition.Value.y + 1, dragPiecePosition.Value.z));
                        surroundCellList.Add(new float3(dragPiecePosition.Value.x + 1, dragPiecePosition.Value.y, dragPiecePosition.Value.z));
                        surroundCellList.Add(new float3(dragPiecePosition.Value.x + 1, dragPiecePosition.Value.y - 1, dragPiecePosition.Value.z));
                        surroundCellList.Add(new float3(dragPiecePosition.Value.x, dragPiecePosition.Value.y - 1, dragPiecePosition.Value.z));
                        surroundCellList.Add(new float3(dragPiecePosition.Value.x - 1, dragPiecePosition.Value.y - 1, dragPiecePosition.Value.z));
                        surroundCellList.Add(new float3(dragPiecePosition.Value.x - 1, dragPiecePosition.Value.y, dragPiecePosition.Value.z));

                        //Validate if the surrounding cells
                        if (surroundCellList.Contains(translation.Value))
                        {
                            Debug.Log("Found matching surrounding cells!");
                            ecb.AddComponent<HighlightedTag>(cell);
                        }
                    }
                }).Run();
        /*
        Entities.WithoutBurst().
            WithAny<PieceComponent>().
                ForEach((Entity entity, ref Translation translation) => {
                    Translation pieceTranslation = GetComponent<Translation>(entity);
                    float3 piecePosition = pieceTranslation.Value;

                    EntityQuery cellQuery = GetEntityQuery(typeof(CellComponent));
                    NativeArray<Entity> cellArray = cellQuery.ToEntityArray(Allocator.Temp);

                    EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

                    for (int i = 0; i < cellArray.Length; i++)
                    {
                        Translation cellTranslation = GetComponent<Translation>(cellArray[i]);
                        float3 cellPosition = cellTranslation.Value;

                        if (piecePosition.x == cellPosition.x && piecePosition.y == cellPosition.y)
                        {
                            entityManager.SetComponentData(cellArray[i], new CellComponent
                            {
                                cellState = CellState.None
                            });
                        }
                    }
                }).Run();
        */
    }
}

