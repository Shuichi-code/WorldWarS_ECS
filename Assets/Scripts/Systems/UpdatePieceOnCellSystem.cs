using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Systems.Special_Ability_Systems;
using Assets.Scripts.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace Assets.Scripts.Systems
{
    /// <summary>
    /// System responsible for updating the pieceoncell component after every fight/move. Activates everytime a PieceOnCellUpdaterTag entity exists.
    /// </summary>
    [UpdateAfter(typeof(ActivateAbilitySystem))]
    [UpdateAfter(typeof(ArbiterCheckingSystem))]
    public class UpdatePieceOnCellSystem : ParallelSystem
    {
        [ReadOnly] private ComponentDataFromEntity<Translation> translationArray;
        protected override void OnUpdate()
        {
            var pieceOnCellUpdateQuery = GetEntityQuery(ComponentType.ReadOnly<PieceOnCellUpdaterTag>());
            if (pieceOnCellUpdateQuery.CalculateEntityCount() == 0) return;
            var ecb = EcbSystem.CreateCommandBuffer();
            var ecbParallel = EcbSystem.CreateCommandBuffer().AsParallelWriter();
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
            var removeAllPieceOnCellComponentsJob = RemoveAllPieceOnCellComponents(ecb);

            var pieceEntitiesQuery = GetEntityQuery(ComponentType.ReadOnly<PieceTag>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<RankComponent>(), ComponentType.ReadOnly<TeamComponent>());
            translationArray = GetComponentDataFromEntity<Translation>(true);
            var piecesEntityArray = pieceEntitiesQuery.ToEntityArray(Allocator.TempJob);
            foreach (var pieceEntity in piecesEntityArray)
            {
                var pieceTranslation = translationArray[pieceEntity];

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
                }).Schedule(removeAllPieceOnCellComponentsJob).Complete();
                //CompleteDependency();
                //EcbSystem.AddJobHandleForProducer(Dependency);
            }
            piecesEntityArray.Dispose();
        }

        private JobHandle RemoveAllPieceOnCellComponents(EntityCommandBuffer ecb)
        {
            return Entities.WithAll<PieceOnCellComponent>()
                .ForEach((Entity e, int entityInQueryIndex) =>
                {
                    ecb.RemoveComponent<PieceOnCellComponent>(e);
                }).Schedule(Dependency);
            //CompleteDependency();
            //EcbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
