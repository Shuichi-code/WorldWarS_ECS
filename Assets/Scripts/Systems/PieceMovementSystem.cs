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
    public event EventHandler<OnDragPressedEventArgs> dragPieceEvent;
    public class OnDragPressedEventArgs : EventArgs
    {
        public bool isDragged;
    }
    public struct EventComponent : IComponentData { }
    private EntityCommandBufferSystem entityCommandBuffer;

    protected override void OnStartRunning()
    {
        base.OnCreate();
        entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

}

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);//entityCommandBuffer.CreateCommandBuffer();
        EntityArchetype eventEntityArchetype = EntityManager.CreateArchetype(typeof(EventComponent));

        Entities.WithoutBurst().WithAll<PieceTag>()
            .ForEach((Entity entity, int entityInQueryIndex, ref Translation translation) =>
            {
                bool isSelecting = BoardManager.GetInstance().isSelecting;
                if (Input.GetMouseButtonDown(0))
                {
                    float3 mousePos = Input.mousePosition;
                    float3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
                    float3 originalPosition = new float3();

                    if ((translation.Value.x == Math.Round(worldPos.x)) &&
                    (translation.Value.y == Math.Round(worldPos.y)) &&
                    !isSelecting)
                    {
                        originalPosition = translation.Value;
                        if (!HasComponent<SelectedTag>(entity))
                        {
                            ecb.AddComponent<SelectedTag>(entity);
                            ecb.CreateEntity(eventEntityArchetype);
                        }
                        isSelecting = true;
                        dragPieceEvent?.Invoke(this, new OnDragPressedEventArgs { isDragged = true });
                    }

                    //TODO: add code to highlight the possible cells that the piece can move
                }

                if (Input.GetMouseButtonUp(0))
                {
                    //add code to set the piece down
                    if (HasComponent<SelectedTag>(entity))
                    {
                        ecb.RemoveComponent<SelectedTag>(entity);
                    }
                    //sets the piece down to the nearest cell of the mouse
                    translation.Value.x = (float)Math.Round(translation.Value.x);
                    translation.Value.y = (float)Math.Round(translation.Value.y);

                    isSelecting = (isSelecting) ? false : true;
                    dragPieceEvent?.Invoke(this, new OnDragPressedEventArgs { isDragged = false });
                }
            }).Run();
        ecb.Playback(EntityManager);
        ecb.Dispose();

        Entities.WithoutBurst().ForEach((ref EventComponent eventComponent) => {
            dragPieceEvent?.Invoke(this, new OnDragPressedEventArgs { isDragged = true });
        }).Run();
        EntityManager.DestroyEntity(GetEntityQuery(typeof(EventComponent)));
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        //nativeQueue.Dispose();
    }
}
