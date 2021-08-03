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
            var playerEntity = GetPlayerEntity<PlayerTag>();
            var enemyEntity = GetPlayerEntity<EnemyTag>();
            //attach special ability to the player
            TagPlayerWithSpecialAbility(ecb, playerEntity, enemyEntity);
        }

        private Entity GetPlayerEntity<T>()
        {
            return GetEntityQuery(ComponentType.ReadOnly<T>(),
                ComponentType.ReadOnly<TimeComponent>()).GetSingletonEntity();
        }

        private void TagPlayerWithSpecialAbility(EntityCommandBuffer ecb, Entity playerEntity, Entity enemyEntity)
        {
            Entities.WithAll<CapturedComponent>().WithAny<PlayerTag, EnemyTag>().ForEach(
                (Entity e, in ArmyComponent armyComponent) =>
                {
                    if (armyComponent.army != Army.Russia) return;
                    ecb.AddComponent(HasComponent<PlayerTag>(e) ? playerEntity : enemyEntity, new SpecialAbilityComponent());
                }).Schedule();
            Dependency.Complete();
        }
    }
}
