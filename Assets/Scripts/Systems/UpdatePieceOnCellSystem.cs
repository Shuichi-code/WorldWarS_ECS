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
    [UpdateAfter(typeof(ArbiterCheckingSystem))]
    [UpdateAfter(typeof(DragToMouseSystem))]
    public class UpdatePieceOnCellSystem : ParallelSystem
    {

        protected override void OnUpdate()
        {
            var pieceOnCellUpdateQuery = GetEntityQuery(ComponentType.ReadOnly<PieceOnCellUpdaterTag>());
            if (pieceOnCellUpdateQuery.CalculateEntityCount() == 0) return;
            var ecb = EcbSystem.CreateCommandBuffer();
            UpdatePieceOnCellComponents(ecb);
            DeletePieceOnCellUpdaterEntity(ecb);
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
            //Ecb.AddJobHandleForProducer(Dependency);
        }

        private void UpdatePieceOnCellComponents(EntityCommandBuffer ecb)
        {
            RemoveAllPieceOnCellComponents(ecb);

            var pieceEntitiesQuery = GetEntityQuery(ComponentType.ReadOnly<PieceTag>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<RankComponent>(), ComponentType.ReadOnly<TeamComponent>());
            var pieceTranslationArray = GetComponentDataFromEntity<Translation>(true);
            var piecesEntityArray = pieceEntitiesQuery.ToEntityArray(Allocator.TempJob);
            foreach (var pieceEntity in piecesEntityArray)
            {
                var pieceTranslation = pieceTranslationArray[pieceEntity];

                //WARNING! This can only be run with Schedule() due to it is inside a for loop.
                Entities.WithAll<CellTag>().ForEach((Entity cellEntity, int entityInQueryIndex, in Translation cellTranslation) =>
                {
                    if (Location.IsMatchLocation(pieceTranslation.Value, cellTranslation.Value))
                    {
                        ecb.AddComponent(cellEntity,
                            new PieceOnCellComponent()
                            {
                                PieceEntity = pieceEntity
                            });
                    }
                }).Schedule();
                CompleteDependency();
                //EcbSystem.AddJobHandleForProducer(Dependency);
            }
            piecesEntityArray.Dispose();
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
