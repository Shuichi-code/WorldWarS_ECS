using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using Unity.Entities;

namespace Assets.Scripts.Systems
{
    /// <summary>
    /// System that handles the activateUI, allowing the player to activate their army's special powers
    /// </summary>
    public class ChargeAbilitySystem : ParallelSystem
    {
        public delegate void ChargeAbilityActivateDelegate(bool activateUI);
        public event ChargeAbilityActivateDelegate ChargedAbilityActiveEvent;

        protected override void OnUpdate()
        {
            var gmQuery = GetEntityQuery(ComponentType.ReadOnly<GameManagerComponent>());
            var gm = gmQuery.GetSingleton<GameManagerComponent>();
            var teamToMove = gm.teamToMove;
            var ecbParallel = EcbSystem.CreateCommandBuffer().AsParallelWriter();

            Entities.WithAll<ChargedAbilityTag,PlayerTag>().
                ForEach((Entity e, int entityInQueryIndex, in TeamComponent teamComponent) =>
                {
                    if (teamToMove == teamComponent.myTeam )
                    {
                        if (HasComponent<ChargeEventFiredTag>(e)) return;
                        ecbParallel.AddComponent(entityInQueryIndex,e, new ChargeEventFiredTag());
                        BroadcastChargeAbilityEvent(ecbParallel, entityInQueryIndex, true);
                    }
                    else
                    {
                        if (!HasComponent<ChargeEventFiredTag>(e)) return;
                        ecbParallel.RemoveComponent<ChargeEventFiredTag>(entityInQueryIndex,e);
                        BroadcastChargeAbilityEvent(ecbParallel, entityInQueryIndex, false);
                    }
                }).Schedule();
            CompleteDependency();

            InvokeChargeAbilityEventIfAvailable();
        }

        private void InvokeChargeAbilityEventIfAvailable()
        {
            var ecb = EcbSystem.CreateCommandBuffer();
            Entities
                .ForEach((Entity e, in ChargedAbilityEventComponent eventComponent) =>
                {
                    ChargedAbilityActiveEvent?.Invoke(eventComponent.activateUI);
                    ecb.DestroyEntity(e);
                })
                .WithoutBurst().Run();
        }

        private static void BroadcastChargeAbilityEvent(EntityCommandBuffer.ParallelWriter ecb, int entityInQueryIndex,
            bool setUIActive)
        {
            var chargedEventEntity = ecb.CreateEntity(entityInQueryIndex);
            ecb.AddComponent(entityInQueryIndex, chargedEventEntity, new ChargedAbilityEventComponent() {activateUI = setUIActive});
        }
    }
}