using Assets.Scripts.Class;
using Assets.Scripts.Components;
<<<<<<< Updated upstream
using Unity.Burst;
=======
using Assets.Scripts.Tags;
>>>>>>> Stashed changes
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

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

<<<<<<< Updated upstream
        var entities = GetEntityQuery(ComponentType.ReadOnly<CapturedComponent>(),
            ComponentType.ReadOnly<ArmyComponent>());
        if (entities.CalculateEntityCount() == 0) return;
        var armyArray = entities.ToComponentDataArray<ArmyComponent>(Allocator.Temp);
        var pieceArray = entities.ToComponentDataArray<PieceComponent>(Allocator.Temp);

        //check if russian army
        for (int i = 0; i < armyArray.Length; i++)
        {
            if (armyArray[i].army != Army.Russia) continue;
            var team = pieceArray[i].team;
            var spyQuery = GetEntityQuery(ComponentType.ReadOnly<PieceComponent>());
        }
        //get the team
=======
            var capturedEntityQuery = GetCapturedEntities();
            if (capturedEntityQuery.CalculateEntityCount() == 0) return;
            var armyArray = capturedEntityQuery.ToComponentDataArray<ArmyComponent>(Allocator.Temp);
            var teamArray = capturedEntityQuery.ToComponentDataArray<TeamComponent>(Allocator.Temp);

            //check if russian army
            for (var i = 0; i < armyArray.Length; i++)
            {
                if (armyArray[i].army != Army.Russia) continue;
                var capturedTeam = teamArray[i].myTeam;
                Entities.
                    WithAny<PlayerTag,EnemyTag>().
                    ForEach((Entity e, int entityInQueryIndex, in TeamComponent teamComponent, in ArmyComponent armyComponent) =>
                {
                    if (teamComponent.myTeam == capturedTeam && armyComponent.army == Army.Russia)
                    {
                        //Add special ability
                        ecb.AddComponent(e, typeof(SpecialAbilityComponent));
                    }
                }).ScheduleParallel();

            }
            //get the team
>>>>>>> Stashed changes

        //execute job on the team's spy

<<<<<<< Updated upstream
=======
        }

        private EntityQuery GetCapturedEntities()
        {
            return GetEntityQuery(ComponentType.ReadOnly<CapturedComponent>(),
                ComponentType.ReadOnly<ArmyComponent>(),
                ComponentType.ReadOnly<TeamComponent>());
        }
>>>>>>> Stashed changes
    }
}
