using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags.Single_Turn_Event_Tag;
using Unity.Entities;
using Unity.Jobs;

namespace Assets.Scripts.Systems
{
    public class ChangeTurnSystem : ParallelSystem
    {
        protected override void OnUpdate()
        {
            var changeTurnQuery = GetEntityQuery(ComponentType.ReadOnly <ChangeTurnComponent>());
            if (changeTurnQuery.CalculateEntityCount() == 0) return;
            var currentTurnTeam = changeTurnQuery.GetSingleton<ChangeTurnComponent>().currentTurnTeam;
            var ecb = EcbSystem.CreateCommandBuffer();

            var changeTurnJob = ChangeTurn(ecb, currentTurnTeam);

            DestroyChangeTurnEntity(ecb, changeTurnJob);
        }
        /// <summary>
        /// Method for creating the entity that will trigger the system for checking if a flag has passed.
        /// </summary>
        /// <param name="entityInQueryIndex"></param>
        /// <param name="ecbParallel"></param>
        private static void CreateFlagPassedCheckerEntity(EntityCommandBuffer ecbParallel)
        {
            var checkFlagPassedCheckerEntity = ecbParallel.CreateEntity();
            ecbParallel.AddComponent( checkFlagPassedCheckerEntity, new CheckFlagPassedTag());
        }

        private JobHandle ChangeTurn(EntityCommandBuffer ecbParallel, Team currentTurnTeam)
        {
            var changeTurnJob = Entities.
                ForEach((ref GameManagerComponent gameManagerComponent) =>
                {
                    gameManagerComponent.teamToMove = SwapTeam(currentTurnTeam);
                    CreateFlagPassedCheckerEntity( ecbParallel);
                }).Schedule(Dependency);
            return changeTurnJob;
        }

        private static Team SwapTeam(Team currentTurnTeam)
        {
            return currentTurnTeam == Team.Invader ? Team.Defender : Team.Invader;
        }

        private void DestroyChangeTurnEntity(EntityCommandBuffer ecb, JobHandle changeTurnJob)
        {
            var destroyChangeTurnEntityJob = Entities.
                WithAll<ChangeTurnComponent>().
                ForEach((Entity e, int entityInQueryIndex) => {
                    ecb.DestroyEntity(e);
                }).Schedule(changeTurnJob);

            destroyChangeTurnEntityJob.Complete();
            //EcbSystem.AddJobHandleForProducer(Dependency);

        }
    }
}
