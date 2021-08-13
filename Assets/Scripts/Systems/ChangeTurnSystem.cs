using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags.Single_Turn_Event_Tag;
using Unity.Entities;

namespace Assets.Scripts.Systems
{
    public class ChangeTurnSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var changeTurnQuery = GetEntityQuery(ComponentType.ReadOnly <ChangeTurnComponent>());
            if (changeTurnQuery.CalculateEntityCount() == 0) return;
            var currentTurnTeam = changeTurnQuery.GetSingleton<ChangeTurnComponent>().currentTurnTeam;
            var ecbParallel = ecbSystem.CreateCommandBuffer().AsParallelWriter();

            ChangeTurn(ecbParallel, currentTurnTeam);

            DestroyChangeTurnEntity(ecbParallel);
        }
        /// <summary>
        /// Method for creating the entity that will trigger the system for checking if a flag has passed.
        /// </summary>
        /// <param name="entityInQueryIndex"></param>
        /// <param name="ecbParallel"></param>
        private static void CreateFlagPassedCheckerEntity(int entityInQueryIndex,
            EntityCommandBuffer.ParallelWriter ecbParallel)
        {
            var checkFlagPassedCheckerEntity = ecbParallel.CreateEntity(entityInQueryIndex);
            ecbParallel.AddComponent(entityInQueryIndex, checkFlagPassedCheckerEntity, new CheckFlagPassedTag());
        }

        private void ChangeTurn(EntityCommandBuffer.ParallelWriter ecbParallel, Team currentTurnTeam)
        {
            Entities.
                ForEach((int entityInQueryIndex, ref GameManagerComponent gameManagerComponent) =>
                {
                    gameManagerComponent.teamToMove = SwapTeam(currentTurnTeam);
                    CreateFlagPassedCheckerEntity(entityInQueryIndex, ecbParallel);
                }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(Dependency);
        }

        private static Team SwapTeam(Team currentTurnTeam)
        {
            return currentTurnTeam == Team.Invader ? Team.Defender : Team.Invader;
        }

        private void DestroyChangeTurnEntity(EntityCommandBuffer.ParallelWriter ecbParallel)
        {
            Entities.
                WithAll<ChangeTurnComponent>().
                ForEach((Entity e, int entityInQueryIndex) => {
                    ecbParallel.DestroyEntity(entityInQueryIndex,e);
                }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(Dependency);

        }
    }
}
