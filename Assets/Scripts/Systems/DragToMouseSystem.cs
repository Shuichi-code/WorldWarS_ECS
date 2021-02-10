using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class DragToMouseSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.WithoutBurst().WithAll<SelectedTag, PieceTag>().ForEach((ref Translation translation) => {
            float3 mousePos = Input.mousePosition;
            float3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

            translation.Value.x = worldPos.x;
            translation.Value.y = worldPos.y;
        }).Run();
    }
}
