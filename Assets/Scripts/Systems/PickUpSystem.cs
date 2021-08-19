using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

//Tag the piece to be pickedup
namespace Assets.Scripts.Systems
{
    public class PickUpSystem : ParallelSystem
    {
        protected override void OnUpdate()
        {
            #region Initializing Data

            var mouseButtonDown = Input.GetMouseButtonDown(0);
            var roundedWorldPos = Location.GetRoundedMousePosition();
            var ecb = EcbSystem.CreateCommandBuffer().AsParallelWriter();

            #endregion

            Entities
                .WithAll<PieceTag, PlayableTag>()
                .ForEach((Entity e, int entityInQueryIndex, ref Translation pieceTranslation, in PieceTag piece) =>
                {

                    var pieceRoundedLocation = math.round(pieceTranslation.Value);

                    if (Location.IsMatchLocation(pieceRoundedLocation, roundedWorldPos) && mouseButtonDown && !HasComponent<SelectedTag>(e))
                        Tag.AddTag<SelectedTag>(ecb, entityInQueryIndex, e);

                }).ScheduleParallel();
            EcbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
