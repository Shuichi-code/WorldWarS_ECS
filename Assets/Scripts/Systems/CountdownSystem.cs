using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Monobehaviours.Managers;
using Assets.Scripts.Tags;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

namespace Assets.Scripts.Systems
{
    public class CountdownSystem : ParallelSystem
    {
        public delegate void CountDownDelegate(Team teamClockToCountDown, float timeRemaining);
        public event CountDownDelegate ClockTick;
        protected override void OnUpdate()
        {

            var ecb = EcbSystem.CreateCommandBuffer().AsParallelWriter();
            var teamToMove = GetEntityQuery(ComponentType.ReadOnly<GameManagerComponent>())
                .GetSingleton<GameManagerComponent>().teamToMove;
            var delta = Time.DeltaTime;

            Entities.
                ForEach((Entity e, int entityInQueryIndex, ref TimeComponent timeComponent, in TeamComponent team) =>
                {
                    if (team.myTeam != teamToMove) return;
                    if (timeComponent.TimeRemaining < 0f)
                    {
                        timeComponent.TimeRemaining = 0;
                        var gameFinishedEntity = ecb.CreateEntity(entityInQueryIndex);
                        ecb.AddComponent(entityInQueryIndex, gameFinishedEntity, new GameFinishedEventComponent()
                        {
                            winningTeam = team.myTeam == Team.Defender ? Team.Invader : Team.Defender
                        });
                    }
                    else
                    {
                        timeComponent.TimeRemaining -= delta;
                        var eventEntity = ecb.CreateEntity(entityInQueryIndex);
                        ecb.AddComponent(entityInQueryIndex, eventEntity,
                            new CountdownEventComponent() { winningTeam = team.myTeam, Time = timeComponent.TimeRemaining });
                    }
                }).ScheduleParallel();
                EcbSystem.AddJobHandleForProducer(this.Dependency);

            CheckIfCountDownEventRaised();
        }

        private void CheckIfCountDownEventRaised()
        {
            Entities
                .ForEach((in CountdownEventComponent eventComponent) => { ClockTick?.Invoke(eventComponent.winningTeam, eventComponent.Time); })
                .WithoutBurst().Run();
            EntityManager.DestroyEntity(GetEntityQuery(typeof(CountdownEventComponent)));
        }
    }
}
