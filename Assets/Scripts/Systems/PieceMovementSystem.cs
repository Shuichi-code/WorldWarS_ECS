using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class PieceMovementSystem : SystemBase
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

        //gets the mouseposition to check which piece is being dragged
        float3 mousePos = Input.mousePosition;
        float3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

        //job for iterating through all the pieces and puts a tag on the entity nearest the mouse when the left click is held down
        Entities.
            WithAll<PieceTag>().
            ForEach((Entity entity, int entityInQueryIndex, ref Translation translation, ref PieceComponent piece) =>
            {
                if (Input.GetMouseButtonDown(0))
                {
                    float3 originalPosition = new float3();

                    if ((translation.Value.x == Math.Round(worldPos.x)) &&
                    (translation.Value.y == Math.Round(worldPos.y)) &&
                    !gameManagerArray[gameManagerEntity].isDragging)
                    {
                        originalPosition = translation.Value;
                        if (!HasComponent<SelectedTag>(entity))
                        {
                            ecb.AddComponent<SelectedTag>(entity);
                        }
                            ecb.SetComponent(gameManagerEntity,
                                new GameManagerComponent
                                {
                                    isDragging = true,
                                    state = GameManagerComponent.State.Playing
                                }
                            );
                    }
                }

                if (Input.GetMouseButtonUp(0))
                {
                    //add code to set the piece down
                    if (HasComponent<SelectedTag>(entity))
                    {
                        //sets the piece down to the nearest cell of the mouse
                        translation.Value.x = (float)Math.Round(translation.Value.x);
                        translation.Value.y = (float)Math.Round(translation.Value.y);

                        piece.originalCellPosition.x = (float)Math.Round(translation.Value.x);
                        piece.originalCellPosition.y = (float)Math.Round(translation.Value.y);
                        ecb.RemoveComponent<SelectedTag>(entity);
                    }
                    ecb.SetComponent(gameManagerEntity,
                        new GameManagerComponent
                        {
                            isDragging = false,
                            state = GameManagerComponent.State.Playing
                        }
                    );
                }
            }).Run();
        //ecb.Playback(EntityManager);
        //ecb.Dispose();
    }
}
