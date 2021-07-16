using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Monobehaviours.Managers;
using Assets.Scripts.Tags;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

namespace Assets.Scripts.Systems
{
    public class CountdownSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem ecbSystem;
        private EntityArchetype eventEntityArchetype;

        public delegate void CountDownDelegate(Team teamClockToCountDown, float timeRemaining);

        public event CountDownDelegate clockTick;

        protected override void OnCreate()
        {
            base.OnCreate();
            // Find the ECB system once and store it for later usage
            ecbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {

            var ecb = ecbSystem.CreateCommandBuffer();
            var teamToMove = GetEntityQuery(ComponentType.ReadOnly<GameManagerComponent>())
                .GetSingleton<GameManagerComponent>().teamToMove;
            var delta = Time.DeltaTime;
            var entityArchetype = EntityManager.CreateArchetype(typeof(GameFinishedEventComponent));
            var clockEntityArchetype = EntityManager.CreateArchetype(typeof(CountdownEventComponent));

            Entities.
                ForEach((Entity e, ref TimeComponent timeComponent, in TeamComponent team) =>
                {
                    if (team.myTeam != teamToMove) return;
                    if (math.round(timeComponent.TimeRemaining) == 0f)
                    {
                        Debug.Log("Sending out finish event!");
                        ArbiterCheckingSystem.DeclareWinner(ecb, entityArchetype, GameManager.SwapTeam(team.myTeam));
                        ecb.RemoveComponent<TimeComponent>(e);
                    }
                    else
                    {
                        timeComponent.TimeRemaining -= delta;
                        var eventEntity = ecb.CreateEntity(clockEntityArchetype);
                        ecb.AddComponent(eventEntity, new CountdownEventComponent() { winningTeam = team.myTeam, Time = timeComponent.TimeRemaining });
                    }
                }).Schedule();
            Dependency.Complete();
            //ecbSystem.AddJobHandleForProducer(this.Dependency);

            CheckIfCountDownEventRaised();
        }

        private void CheckIfCountDownEventRaised()
        {
            Entities
                .ForEach((in CountdownEventComponent eventComponent) => { clockTick?.Invoke(eventComponent.winningTeam, eventComponent.Time); })
                .WithoutBurst().Run();
            EntityManager.DestroyEntity(GetEntityQuery(typeof(CountdownEventComponent)));
        }
    }
}
