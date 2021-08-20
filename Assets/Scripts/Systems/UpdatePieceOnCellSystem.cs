using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Assets.Scripts.Systems
{
    [UpdateAfter(typeof(FightSystem))]
    /// <summary>
    /// System responsible for updating the pieceoncell component after every fight/move.
    /// </summary>
    public class UpdatePieceOnCellSystem : ParallelSystem
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            this.Enabled = false;
        }

        protected override void OnUpdate()
        {
            //var pieceOnCellUpdateQuery = GetEntityQuery(ComponentType.ReadOnly<PieceOnCellUpdaterTag>());
            //if (pieceOnCellUpdateQuery.CalculateEntityCount() == 0) return;

            var ecb = EcbSystem.CreateCommandBuffer();
            UpdatePieceOnCellComponents(ecb);
            this.Enabled = false;
            //DeletePieceOnCellUpdaterEntity(ecb);
        }

        private void DeletePieceOnCellUpdaterEntity(EntityCommandBuffer ecb)
        {
            Entities.
                WithAll<PieceOnCellUpdaterTag>().
                ForEach((Entity e, int entityInQueryIndex) =>
                {
                    ecb.DestroyEntity(e);
                }).Schedule();
            CompleteDependency();
            //EcbSystem.AddJobHandleForProducer(Dependency);
        }

        private void UpdatePieceOnCellComponents(EntityCommandBuffer ecb)
        {
            RemoveAllPieceOnCellComponents(ecb);

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
                        ecb.AddComponent( cellEntity,
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

        private void RemoveAllPieceOnCellComponents(EntityCommandBuffer ecb)
        {
            Entities.WithAll<PieceOnCellComponent>()
                .ForEach((Entity e, int entityInQueryIndex) =>
                {
                    ecb.RemoveComponent<PieceOnCellComponent>(e);
                }).Schedule();
            CompleteDependency();
            //EcbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
