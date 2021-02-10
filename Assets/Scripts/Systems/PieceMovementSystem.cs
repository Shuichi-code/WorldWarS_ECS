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

    protected override void OnCreate()
    {
        base.OnCreate();
        entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        var ecb = entityCommandBuffer.CreateCommandBuffer();

        Entities.WithoutBurst().WithAll<PieceTag>()
            .ForEach((Entity entity, ref Translation translation) =>
            {
                if (Input.GetMouseButton(0))
                {
                    float3 mousePos = Input.mousePosition;
                    float3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

                    if ((translation.Value.x == Math.Ceiling(worldPos.x)) && (translation.Value.y == Math.Ceiling(worldPos.y)))
                    {
                        //UnityEngine.Debug.Log("Found Match!");
                        if (!HasComponent<SelectedTag>(entity))
                        {
                            ecb.AddComponent<SelectedTag>(entity);
                        }
                    }
                }
        }).Run();
    }
}
