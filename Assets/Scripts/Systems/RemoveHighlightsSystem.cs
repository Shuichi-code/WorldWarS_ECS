using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

//[DisableAutoCreation]
[UpdateAfter(typeof(PiecePutDownSystem))]
public class RemoveHighlightsSystem : SystemBase
{
    private EntityCommandBufferSystem entityCommandBuffer;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        EntityQuery gameManagerQuery = GetEntityQuery(ComponentType.ReadOnly<GameManagerComponent>());
        Entity gameManagerEntity = gameManagerQuery.GetSingletonEntity();
        ComponentDataFromEntity<GameManagerComponent> gameManagerArray = GetComponentDataFromEntity<GameManagerComponent>();

        EntityCommandBuffer ecb = entityCommandBuffer.CreateCommandBuffer();
        Entities.WithAny<HighlightedTag, EnemyCellTag>().
            ForEach((Entity cell) => {
                if (!gameManagerArray[gameManagerEntity].isDragging)
                {
                    if (HasComponent<HighlightedTag>(cell))
                    {
                        ecb.RemoveComponent<HighlightedTag>(cell);
                    }
                    if (HasComponent<EnemyCellTag>(cell))
                    {
                        ecb.RemoveComponent<EnemyCellTag>(cell);
                    }
                }
        }).Run();
    }
}
