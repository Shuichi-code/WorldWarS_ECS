using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Assets.Scripts.Systems
{
    /// <summary>
    /// System responsible for updating the pieceoncell component after every fight/move.
    /// </summary>
    public class UpdatePieceOnCellSystem : ParallelSystem
    {
        protected override void OnUpdate()
        {
            var pieceOnCellUpdateQuery = GetEntityQuery(ComponentType.ReadOnly<PieceOnCellUpdaterTag>());
            if (pieceOnCellUpdateQuery.CalculateEntityCount() == 0) return;

            var ecbParallel = EcbSystem.CreateCommandBuffer().AsParallelWriter();
            UpdatePieceOnCellComponents(ecbParallel);
            DeletePieceOnCellUpdaterEntity(ecbParallel);
        }

        private void DeletePieceOnCellUpdaterEntity(EntityCommandBuffer.ParallelWriter ecbParallel)
        {
            Entities.
                WithAll<PieceOnCellUpdaterTag>().
                ForEach((Entity e, int entityInQueryIndex) =>
                {
                    ecbParallel.DestroyEntity(entityInQueryIndex, e);
                }).ScheduleParallel();
            EcbSystem.AddJobHandleForProducer(Dependency);
        }

        private void UpdatePieceOnCellComponents(EntityCommandBuffer.ParallelWriter ecbParallelWriter)
        {
            RemoveAllPieceOnCellComponents(ecbParallelWriter);

            var pieceEntitiesQuery = GetEntityQuery(ComponentType.ReadOnly<PieceTag>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<RankComponent>(), ComponentType.ReadOnly<TeamComponent>());
            var pieceTranslationArray = pieceEntitiesQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
            var pieceRankArray = pieceEntitiesQuery.ToComponentDataArray<RankComponent>(Allocator.TempJob);
            var piecesTeamArray = pieceEntitiesQuery.ToComponentDataArray<TeamComponent>(Allocator.TempJob);
            var piecesEntityArray = pieceEntitiesQuery.ToEntityArray(Allocator.TempJob);
            for (var index = 0; index < pieceTranslationArray.Length; index++)
            {
                var pieceTranslation = pieceTranslationArray[index];
                Entities.WithAll<CellTag>().ForEach((Entity cellEntity, int entityInQueryIndex, in Translation cellTranslation) =>
                {
                    if (Location.IsMatchLocation(pieceTranslation.Value, cellTranslation.Value))
                    {
                        ecbParallelWriter.AddComponent(entityInQueryIndex, cellEntity,
                            new PieceOnCellComponent()
                            {
                                PieceEntity = piecesEntityArray[index],
                                PieceRank = pieceRankArray[index].Rank,
                                PieceTeam = piecesTeamArray[index].myTeam
                            });
                    }
                }).Schedule();
                CompleteDependency();
            }
            pieceTranslationArray.Dispose();
            piecesEntityArray.Dispose();
            pieceRankArray.Dispose();
            piecesTeamArray.Dispose();
        }

        private void RemoveAllPieceOnCellComponents(EntityCommandBuffer.ParallelWriter ecbParallelWriter)
        {
            Entities.WithAll<PieceOnCellComponent>()
                .ForEach((Entity e, int entityInQueryIndex) =>
                {
                    ecbParallelWriter.RemoveComponent<PieceOnCellComponent>(entityInQueryIndex, e);
                }).ScheduleParallel();
            EcbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
