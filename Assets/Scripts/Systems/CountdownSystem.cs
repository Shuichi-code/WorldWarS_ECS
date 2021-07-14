using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Monobehaviours.Managers;
using Unity.Entities;

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

            Entities
                .ForEach((Entity e, int entityInQueryIndex, ref TimeComponent timeComponent, in TeamComponent team) => {

                    if (timeComponent.TimeRemaining <= 0)
                    {
                        var eventEntity = ecb.CreateEntity(entityArchetype);
                        ecb.AddComponent(eventEntity, new GameFinishedEventComponent(){winningTeam = GameManager.SwapTeam(team.myTeam)});
                    }
                    else
                    {
                        if (team.myTeam != teamToMove) return;
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
