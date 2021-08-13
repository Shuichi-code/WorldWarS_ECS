using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags.Single_Turn_Event_Tag;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class CheckFlagPassedSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var checkFlagPassedQuery = GetEntityQuery(ComponentType.ReadOnly<CheckFlagPassedTag>());
            if (checkFlagPassedQuery.CalculateEntityCount() == 0) return;

            var ecbParallel = ecbSystem.CreateCommandBuffer().AsParallelWriter();
            var flagPassingQuery = GetEntityQuery(ComponentType.ReadOnly<FlagPassingTag>());
            if (flagPassingQuery.CalculateEntityCount() == 0)
            {
                TagFlagsThatArePassing(ecbParallel);
                ecbSystem.AddJobHandleForProducer(Dependency);
            }
            else
            {
                var currentTurnTeam = GetEntityQuery(ComponentType.ReadOnly<GameManagerComponent>())
                    .GetSingleton<GameManagerComponent>().teamToMove;

                CheckWhichFlagPassed(currentTurnTeam, ecbParallel);
            }
            DestroyCheckFlagPassedQueryEntity(ecbParallel);
        }
        /// <summary>
        /// Checks all of the last line cells if there are any flag entities associated with them
        /// </summary>
        /// <param name="ecbParallel"></param>
        private void TagFlagsThatArePassing(EntityCommandBuffer.ParallelWriter ecbParallel)
        {
            Entities.WithAll<PieceOnCellComponent, LastCellsTag>().ForEach(
                (int entityInQueryIndex, in PieceOnCellComponent pieceOnCellComponent,
                    in HomeCellComponent homeCellComponent) =>
                {
                    var pieceEntity = pieceOnCellComponent.PieceEntity;
                    var pieceRank = pieceOnCellComponent.PieceRank;
                    var pieceTeam = pieceOnCellComponent.PieceTeam;

                    if (pieceRank != Piece.Flag) return;
                    if (pieceTeam == homeCellComponent.homeTeam) return;
                    ecbParallel.AddComponent(entityInQueryIndex, pieceEntity, new FlagPassingTag());
                }).ScheduleParallel();
        }

        private void CheckWhichFlagPassed(Team currentTurnTeam, EntityCommandBuffer.ParallelWriter ecbParallel)
        {
            Entities.WithAll<FlagPassingTag>().ForEach((Entity e, int entityInQueryIndex, in TeamComponent teamComponent) =>
            {
                if (teamComponent.myTeam != currentTurnTeam) return;
                var declareWinnerEntity = ecbParallel.CreateEntity(entityInQueryIndex);
                ecbParallel.AddComponent(entityInQueryIndex, declareWinnerEntity, new GameFinishedEventComponent()
                {
                    winningTeam = teamComponent.myTeam
                });
            }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(Dependency);
        }

        private void DestroyCheckFlagPassedQueryEntity(EntityCommandBuffer.ParallelWriter ecbParallel)
        {
            Entities.
                WithAll<CheckFlagPassedTag>().
                ForEach((Entity e, int entityInQueryIndex) => {
                    ecbParallel.DestroyEntity(entityInQueryIndex,e);
            }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
