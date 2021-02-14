using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class DragToMouseSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float3 mousePos = Input.mousePosition;
        float3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        Entities.
            WithAll<SelectedTag>().
            ForEach((ref Translation translation) => {
                translation.Value.x = worldPos.x;
                translation.Value.y = worldPos.y;
        }).Schedule();
    }
}
