using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Monobehaviours;
using Assets.Scripts.Monobehaviours.Managers;
using Assets.Scripts.Tags;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

//[DisableAutoCreation]
namespace Assets.Scripts.Systems
{
    public class RenderSystem : SystemBase
    {
        private EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            entityCommandBufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            EntityCommandBuffer entityCommandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            //code for rendering pieces
            Entities.
                WithoutBurst().
                ForEach((in PieceComponent pieceComponent, in Translation translation) =>
                {
                    var pieceMaterial = Resources.Load(new Dictionaries().mPieceRank[pieceComponent.pieceRank],
                        typeof(Material)) as Material;
                    Render(translation, pieceMaterial);
                }).Run();
            //code for rendering normal cells
            var cellImage = GameManager.GetInstance().cellImage;
            Entities.
                WithoutBurst().
                ForEach((in CellTag cellComponent, in Translation translation) =>
                {
                    Render(translation, cellImage);
                }).Run();

            var highlightedImage = GameManager.GetInstance().highlightedImage;
            //code for rendering highlighted cells
            Entities.
                WithoutBurst().
                ForEach((in HighlightedTag highlightedTag, in Translation translation) =>
                {
                    Render(translation, highlightedImage);
                }).Run();

            var enemyCellImage = GameManager.GetInstance().enemyCellImage;
            //code for rendering enemy cells
            Entities.
                WithoutBurst().
                WithAll<EnemyCellTag>().
                ForEach((in Translation translation) =>
                {
                    Render(translation, enemyCellImage);
                }).Run();
        }

        private static void Render(Translation translation, Material material)
        {
            Mesh quadMesh = GameManager.GetInstance().quadMesh;
            Graphics.DrawMesh(
                quadMesh,
                translation.Value,
                Quaternion.identity,
                material,
                0
            );
        }
    }
}
