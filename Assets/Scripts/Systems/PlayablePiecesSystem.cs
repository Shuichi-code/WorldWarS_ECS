using Assets.Scripts.Components;
using Assets.Scripts.Class;
using Assets.Scripts.Tags;
using Unity.Entities;

namespace Assets.Scripts.Systems
{
    public class PlayablePiecesSystem : ParallelSystem
    {
        protected override void OnUpdate()
        {
            #region CheckGameState
            var gmQuery = GetEntityQuery(ComponentType.ReadOnly<GameManagerComponent>());
            var gm = gmQuery.GetSingleton<GameManagerComponent>();

            #endregion
            var ecb = EcbSystem.CreateCommandBuffer().AsParallelWriter();
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
            EcbSystem.AddJobHandleForProducer(this.Dependency);
        }
    }
}
