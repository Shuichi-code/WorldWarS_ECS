using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class PiecePickupSystem : SystemBase
{
    private EntityCommandBufferSystem entityCommandBuffer;

    protected override void OnStartRunning()
    {
        base.OnCreate();
        entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

    }

    protected override void OnUpdate()
    {
        //var ecb = new EntityCommandBuffer(Allocator.TempJob);//entityCommandBuffer.CreateCommandBuffer();
        var ecb = entityCommandBuffer.CreateCommandBuffer();
        //get the gamemanager Entity
        EntityQuery gameManagerQuery = GetEntityQuery(typeof(GameManagerComponent));
        Entity gameManagerEntity = gameManagerQuery.GetSingletonEntity();
        ComponentDataFromEntity<GameManagerComponent> gameManagerArray = GetComponentDataFromEntity<GameManagerComponent>();

        EntityQuery cellWithPieceQuery = GetEntityQuery(typeof(PieceOnCellComponent));
        ComponentDataFromEntity<PieceOnCellComponent> cellWithPieceArray = GetComponentDataFromEntity<PieceOnCellComponent>();

        //gets the mouseposition to check which piece is being dragged
        float3 mousePos = Input.mousePosition;
        float3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

        //job for iterating through all the pieces and puts a tag on the entity nearest the mouse when the left click is held down
        Entities.
            WithAll<PieceTag>().
            ForEach((Entity pieceEntity, ref Translation translation, in PieceComponent pieceComponent) =>
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Color teamColorToMove = gameManagerArray[gameManagerEntity].teamToMove;
                    if ((translation.Value.x == Math.Round(worldPos.x)) &&
                    (translation.Value.y == Math.Round(worldPos.y)) &&
                    teamColorToMove == pieceComponent.teamColor &&
                    !gameManagerArray[gameManagerEntity].isDragging)
                    {
                        if (!HasComponent<SelectedTag>(pieceEntity))
                        {
                            ecb.AddComponent<SelectedTag>(pieceEntity);
                        }

                        ecb.SetComponent(gameManagerEntity,
                            new GameManagerComponent
                            {
                                isDragging = true,
                                state = GameManagerComponent.State.Playing,
                                teamToMove = gameManagerArray[gameManagerEntity].teamToMove
                            }
                        );
                    }
                }
            }).Run();
    }
}
