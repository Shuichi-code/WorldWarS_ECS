using Assets.Scripts.Class;
using Assets.Scripts.Components;
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
        private static GameManager gameManager;

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            entityCommandBufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            gameManager = GameManager.GetInstance();
        }
        protected override void OnUpdate()
        {
            //code for rendering pieces
            Entities.
                WithAll<PieceTag>().
                WithoutBurst().
                ForEach((in RankComponent rankComponent, in Translation translation) =>
                {
                    var pieceMaterial = Resources.Load(new Dictionaries().mPieceRank[rankComponent.Rank],
                        typeof(Material)) as Material;
                    Render(translation, pieceMaterial);
                }).Run();
            //code for rendering normal cells
            var cellImage = gameManager.cellImage;
            Entities.
                WithoutBurst().
                ForEach((in CellTag cellComponent, in Translation translation) =>
                {
                    Render(translation, cellImage);
                }).Run();

            var highlightedImage = gameManager.highlightedImage;
            //code for rendering highlighted cells
            Entities.
                WithoutBurst().
                ForEach((in HighlightedTag highlightedTag, in Translation translation) =>
                {
                    Render(translation, highlightedImage);
                }).Run();

            var enemyCellImage = gameManager.enemyCellImage;
            //code for rendering enemy cells
            Entities.
                WithoutBurst().
                WithAll<EnemyCellTag>().
                ForEach((in Translation translation) =>
                {
                    Render(translation, enemyCellImage);
                }).Run();
            var highlightedPieceImage = gameManager.highlightedPiece;
            Entities.
                WithoutBurst().
                WithAll<BulletComponent>().
                ForEach((in Translation translation) =>
                {
                    Render(translation, highlightedPieceImage);
                }).Run();
        }

        private static void Render(Translation translation, Material material)
        {
            var quadMesh = gameManager.quadMesh;
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
