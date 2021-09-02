using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Assets.Scripts.Systems
{
    [UpdateAfter(typeof(DragToMouseSystem))]
    public class CollideSystem : ParallelSystem
    {
        protected override void OnUpdate()
        {
            var collideEventQuery = GetEntityQuery(ComponentType.ReadOnly<PieceCollisionCheckerTag>());
            if (collideEventQuery.CalculateEntityCount() == 0) return;
            var playerPiecesQuery = GetEntityQuery(ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<PieceTag>(), ComponentType.ReadOnly<Translation>());
            var playerPiecesEntityArray = playerPiecesQuery.ToEntityArray(Allocator.TempJob);
            var playerPiecesTranslationArray = GetComponentDataFromEntity<Translation>(true);
            var ecb = EcbSystem.CreateCommandBuffer().AsParallelWriter();

            foreach (var playerPieceEntity in playerPiecesEntityArray)
            {
                var playerPieceTranslation = playerPiecesTranslationArray[playerPieceEntity];

                //WARNING! This needs to be run on Schedule() only due to the use of the translation component data array
                Entities.
                    WithAll<PieceTag, EnemyTag>().
                    WithNone<PrisonerTag>().
                    ForEach((Entity enemyPieceEntity, int entityInQueryIndex, ref Translation translation) =>
                    {
                        if (!Location.IsMatchLocation(playerPieceTranslation.Value, translation.Value)) return;
                        //Debug.Log("Collision detected!");
                        CreateArbiter(ecb, enemyPieceEntity, playerPieceEntity, entityInQueryIndex);
                    }).Schedule();
                //EcbSystem.AddJobHandleForProducer(Dependency);
                CompleteDependency();
            }
            playerPiecesEntityArray.Dispose();
            DestroyCollideEventComponent();
        }

        private void DestroyCollideEventComponent()
        {
            var ecb = EcbSystem.CreateCommandBuffer();
            Entities.ForEach((Entity e, int entityInQueryIndex, in PieceCollisionCheckerTag collide) =>
            {
                ecb.DestroyEntity(e);
            }).Schedule();
            CompleteDependency();

        }

        private static void CreateArbiter(EntityCommandBuffer.ParallelWriter ecbParallel, Entity enemyPieceEntity,
            Entity playerPieceEntity, int entityInQueryIndex)
        {
            var arbiterEntity = ecbParallel.CreateEntity(entityInQueryIndex);
            ecbParallel.AddComponent(entityInQueryIndex, arbiterEntity, new ArbiterComponent()
            {
                attackingPieceEntity = enemyPieceEntity,
                defendingPieceEntity = playerPieceEntity
            });
        }
    }
}
