using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

//Tag the piece to be pickedup
namespace Assets.Scripts.Systems
{
    public class PickUpSystem : SystemBase
    {
        EndSimulationEntityCommandBufferSystem ecbSystem;
        protected override void OnCreate()
        {
            base.OnCreate();

            ecbSystem = World
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            #region Initializing Data

            bool mouseButtonDown = Input.GetMouseButtonDown(0);
            float3 roundedWorldPos = Location.GetRoundedMousePosition();
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();

            #endregion

            Entities
                .WithAll<PieceComponent, PlayableTag>()
                .ForEach((Entity e, int entityInQueryIndex, ref Translation pieceTranslation, in PieceComponent piece) =>
                {

                    float3 pieceRoundedLocation = math.round(pieceTranslation.Value);
                    float3 selectedPieceRoundedLocation = roundedWorldPos;

                    if (Location.IsMatchLocation(pieceRoundedLocation, selectedPieceRoundedLocation) && mouseButtonDown && !HasComponent<SelectedTag>(e))
                        Tag.TagAsSelectedPiece(ecb, entityInQueryIndex, e);

                }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(this.Dependency);
        }
    }
}
