using System.ComponentModel;
using System.Data;
using Unity.Entities;

namespace Assets.Scripts.Class
{
    public class Tag
    {
        public static void AddTag<T>(EntityCommandBuffer.ParallelWriter ecb, int entityInQueryIndex, Entity e) where T : struct, IComponentData
        {
            ecb.AddComponent<T>(entityInQueryIndex, e);
        }
        public static void RemoveTag<T>(EntityCommandBuffer.ParallelWriter ecb, int entityInQueryIndex, Entity e) where T : struct, IComponentData
        {
            ecb.RemoveComponent<T>(entityInQueryIndex, e);
        }
    }
}