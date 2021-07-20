using Assets.Scripts.Class;
using Assets.Scripts.Components;
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

            var entities = GetEntityQuery(ComponentType.ReadOnly<CapturedComponent>(),
                ComponentType.ReadOnly<ArmyComponent>(),
                ComponentType.ReadOnly<TeamComponent>());
            if (entities.CalculateEntityCount() == 0) return;
            var armyArray = entities.ToComponentDataArray<ArmyComponent>(Allocator.Temp);
            var teamArray = entities.ToComponentDataArray<TeamComponent>(Allocator.Temp);

            //check if russian army
            for (int i = 0; i < armyArray.Length; i++)
            {
                if (armyArray[i].army != Army.Russia) continue;
                var team = teamArray[i].myTeam;
                var spyQuery = GetEntityQuery(ComponentType.ReadOnly<PieceTag>());
            }
            //get the team

            //execute job on the team's spy

        }
    }
}
