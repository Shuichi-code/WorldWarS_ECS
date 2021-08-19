using Assets.Scripts.Class;
using Assets.Scripts.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Assets.Scripts.Systems
{
    public class CollideSystem : ParallelSystem
    {
        protected override void OnUpdate()
        {

            var playerPiecesQuery = GetEntityQuery(ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<PieceTag>(), typeof(Translation));
            var playerPiecesEntityArray = playerPiecesQuery.ToEntityArray(Allocator.TempJob);
            var playerPiecesTranslationArray = GetComponentDataFromEntity<Translation>();
            var ecbParallel = EcbSystem.CreateCommandBuffer().AsParallelWriter();

            foreach (var playerPieceEntity in playerPiecesEntityArray)
            {
                var playerPieceTranslation = playerPiecesTranslationArray[playerPieceEntity];
                Entities.
                    WithAll<PieceTag, EnemyTag>().
                    ForEach((Entity enemyPieceEntity, int entityInQueryIndex, ref Translation translation) =>
                    {
                        if (!Location.IsMatchLocation(playerPieceTranslation.Value, translation.Value)) return;
                        ecbParallel.AddComponent(entityInQueryIndex, enemyPieceEntity, new FighterTag());
                        ecbParallel.AddComponent(entityInQueryIndex, playerPieceEntity, new FighterTag());
                    }).Schedule();
                //EcbSystem.AddJobHandleForProducer(Dependency);
                CompleteDependency();
            }


            playerPiecesEntityArray.Dispose();
        }
    }
}
