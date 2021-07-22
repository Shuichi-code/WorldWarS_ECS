using System.Text.RegularExpressions;
using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using Unity.Collections;
using Unity.Entities;

namespace Assets.Scripts.Systems.ArmySystems
{
    public class RussiaSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem ecbSystem;
        protected override void OnCreate()
        {
            // Find the ECB system once and store it for later usage
            base.OnCreate();
            ecbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var ecb = ecbSystem.CreateCommandBuffer();

            var entities = GetEntityQuery(ComponentType.ReadOnly<CapturedComponent>());
            if (entities.CalculateEntityCount() == 0) return;
            var playerEntity = GetEntityQuery(ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<TimeComponent>()).GetSingletonEntity();
            var enemyEntity = GetEntityQuery(ComponentType.ReadOnly<EnemyTag>(), ComponentType.ReadOnly<TimeComponent>()).GetSingletonEntity();
            //attach special ability to the player
            Entities.
                WithAny<CapturedComponent>().
                ForEach((Entity e, ArmyComponent armyComponent) =>
                {
                    if (armyComponent.army != Army.Russia) return;
                    var playerEntityToBeTagged = HasComponent<PlayerTag>(e) ? playerEntity : enemyEntity;
                    ecb.AddComponent(playerEntityToBeTagged, new SpecialAbilityComponent());
                }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(this.Dependency);
        }
    }
}
