using Unity.Entities;

namespace Assets.Scripts.Class
{
    public class Tag
    {
        public static void TagCellAsEnemy(EntityCommandBuffer.ParallelWriter ecb, int entityInQueryIndex, Entity e)
        {
            ecb.AddComponent<EnemyCellTag>(entityInQueryIndex, e);
        }

        public static void TagCellAsHighlighted(EntityCommandBuffer.ParallelWriter ecb, int entityInQueryIndex, Entity e)
        {
            ecb.AddComponent<HighlightedTag>(entityInQueryIndex, e);
        }

        public static void TagAsSelectedPiece(EntityCommandBuffer.ParallelWriter ecb, int entityInQueryIndex, Entity e)
        {
            ecb.AddComponent<SelectedTag>(entityInQueryIndex, e);
        }

        public static void TagAsPlayable(EntityCommandBuffer.ParallelWriter ecb, int entityInQueryIndex, Entity e)
        {
            ecb.AddComponent<PlayableTag>(entityInQueryIndex, e);
        }

        public static void RemovePlayableTag(EntityCommandBuffer.ParallelWriter ecb, int entityInQueryIndex, Entity e)
        {
            ecb.RemoveComponent<PlayableTag>(entityInQueryIndex, e);
        }
        public static void RemoveEnemyCellTag(EntityCommandBuffer.ParallelWriter ecb, int entityInQueryIndex, Entity e)
        {
            ecb.RemoveComponent<EnemyCellTag>(entityInQueryIndex, e);
        }

        public static void RemoveHighlightedTag(EntityCommandBuffer.ParallelWriter ecb, int entityInQueryIndex, Entity e)
        {
            ecb.RemoveComponent<HighlightedTag>(entityInQueryIndex, e);
        }
    }
}