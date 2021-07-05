using Assets.Scripts.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Assets.Scripts.Class.Location;
using static Assets.Scripts.Class.Tag;

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
            float3 mousePos = Input.mousePosition;
            float3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
            #endregion
            Entities
                .WithAll<PieceComponent, PlayableTag>()
                .ForEach((Entity e, int entityInQueryIndex, ref Translation pieceTranslation, in PieceComponent piece) =>
                {

                    float3 pieceRoundedLocation = math.round(pieceTranslation.Value);
                    float3 selectedPieceRoundedLocation = math.round(worldPos);

                    if (IsMatchLocation(pieceRoundedLocation, selectedPieceRoundedLocation) && mouseButtonDown && !HasComponent<SelectedTag>(e))
                        TagAsSelectedPiece(ecb, entityInQueryIndex, e);

                }).ScheduleParallel();
            ecbSystem.AddJobHandleForProducer(this.Dependency);
        }
    }
}
