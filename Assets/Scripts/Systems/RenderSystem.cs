using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

//[DisableAutoCreation]
public class RenderSystem : SystemBase
{
    private EntityCommandBufferSystem entityCommandBufferSystem;
    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        entityCommandBufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate() {
        EntityCommandBuffer entityCommandBuffer = entityCommandBufferSystem.CreateCommandBuffer();

        //code for rendering normal cells
        Entities.
        WithoutBurst().
        ForEach((in CellComponent cellComponent, in Translation translation) => {
            Mesh quadMesh = BoardManager.GetInstance().quadMesh;
            Material highlightedImage = BoardManager.GetInstance().cellImage;
            Graphics.DrawMesh(
                quadMesh,
                translation.Value,
                Quaternion.identity,
                highlightedImage,
                0
           );
        }).Run();

        //code for rendering highlighted cells
        Entities.
            WithoutBurst().
            ForEach((in HighlightedTag highlightedTag, in Translation translation) => {
                Mesh quadMesh = BoardManager.GetInstance().quadMesh;
                Material highlightedImage = BoardManager.GetInstance().highlightedImage;
                Graphics.DrawMesh(
                    quadMesh,
                    translation.Value,
                    Quaternion.identity,
                    highlightedImage,
                    0
               );
            }).Run();

        //code for rendering enemy cells
        Entities.
            WithoutBurst().
            WithAll<EnemyCellTag>().
            ForEach((in Translation translation) => {
                Mesh quadMesh = BoardManager.GetInstance().quadMesh;
                Material enemycellImage = BoardManager.GetInstance().enemyCellImage;
                Graphics.DrawMesh(
                    quadMesh,
                    translation.Value,
                    Quaternion.identity,
                    enemycellImage,
                    0
               );
            }).Run();

        //code for rendering highlighted cells
        Entities.
            WithoutBurst().
            ForEach((in PieceComponent pieceComponent, in Translation translation) => {
                Mesh quadMesh = BoardManager.GetInstance().quadMesh;
                Material highlightedImage = BoardManager.GetInstance().highlightedImage;
                Graphics.DrawMesh(
                    quadMesh,
                    translation.Value,
                    Quaternion.identity,
                    Resources.Load(BoardManager.GetInstance().mPieceRank[pieceComponent.pieceRank], typeof(Material)) as Material,
                    0
               );
            }).Run();
    }
}
