using Assets.Scripts.Components;
using Assets.Scripts.Class;
using Unity.Entities;

namespace Assets.Scripts.Systems
{
    public class TurnSystem : SystemBase
    {
        private EntityCommandBufferSystem ecbSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            // Find the ECB system once and store it for later usage
            ecbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            #region CheckGameState
            EntityQuery gmQuery = GetEntityQuery(ComponentType.ReadOnly<GameManagerComponent>());
            GameManagerComponent gm = gmQuery.GetSingleton<GameManagerComponent>();

            #endregion
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
            var teamToMove = gm.teamToMove;

            Entities.
                WithAll<PieceTag>()
                .ForEach((Entity e, int entityInQueryIndex, in TeamComponent teamComponent) =>
                {
                    if (teamComponent.myTeam == teamToMove && !HasComponent<PlayableTag>(e))
                        Tag.AddTag<PlayableTag>(ecb, entityInQueryIndex, e);

                    else if (teamComponent.myTeam != teamToMove && HasComponent<PlayableTag>(e))
                        Tag.RemoveTag<PlayableTag>(ecb, entityInQueryIndex, e);

                }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(this.Dependency);
        }
    }
}
