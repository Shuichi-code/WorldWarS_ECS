using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags.Single_Turn_Event_Tag;
using Unity.Collections;
using Unity.Entities;

namespace Assets.Scripts.Systems
{
    public class CheckFlagPassedSystem : ParallelSystem
    {
        protected override void OnUpdate()
        {
            var checkFlagPassedQuery = GetEntityQuery(ComponentType.ReadOnly<CheckFlagPassedTag>());
            if (checkFlagPassedQuery.CalculateEntityCount() == 0) return;

            var ecbParallel = EcbSystem.CreateCommandBuffer().AsParallelWriter();
            var ecb = EcbSystem.CreateCommandBuffer();
            var flagPassingQuery = GetEntityQuery(ComponentType.ReadOnly<FlagPassingTag>());
            if (flagPassingQuery.CalculateEntityCount() == 0)
            {
                TagFlagsThatArePassing(ecbParallel);
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
        private void TagFlagsThatArePassing(EntityCommandBuffer.ParallelWriter ecb)
        {
            var rankArray = GetComponentDataFromEntity<RankComponent>();
            var teamArray = GetComponentDataFromEntity<TeamComponent>();

            Entities.WithAll<PieceOnCellComponent, LastCellsTag>().ForEach(
                (int entityInQueryIndex, in PieceOnCellComponent pieceOnCellComponent,
                    in HomeCellComponent homeCellComponent) =>
                {
                    var pieceEntity = pieceOnCellComponent.PieceEntity;
                    var pieceRank = rankArray[pieceEntity].Rank;
                    var pieceTeam = teamArray[pieceEntity].myTeam;

                    if (pieceRank != Piece.Flag) return;
                    if (pieceTeam == homeCellComponent.homeTeam) return;
                    ecb.AddComponent(entityInQueryIndex, pieceEntity, new FlagPassingTag());
                }).Schedule();
            //EcbSystem.AddJobHandleForProducer(Dependency);
            CompleteDependency();
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
            EcbSystem.AddJobHandleForProducer(Dependency);
        }

        private void DestroyCheckFlagPassedQueryEntity(EntityCommandBuffer.ParallelWriter ecbParallel)
        {
            Entities.
                WithAll<CheckFlagPassedTag>().
                ForEach((Entity e, int entityInQueryIndex) =>
                {
                    ecbParallel.DestroyEntity(entityInQueryIndex, e);
                }).ScheduleParallel();
            EcbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
