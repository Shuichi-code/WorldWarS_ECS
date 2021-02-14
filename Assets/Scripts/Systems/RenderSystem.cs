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
    //private Mesh quadMesh;
    private Material cellImage;
    private Material highlightedImage;
    private Material enemycellImage;

    private EntityCommandBufferSystem entityCommandBufferSystem;

    private System.Collections.Generic.Dictionary<int, string> mPieceRank = new System.Collections.Generic.Dictionary<int, string>()
    {
        { 0 ,"Spy"},
        { 1 ,"G5S" },
        { 2 ,"G4S" },
        { 3 ,"LtG"},
        { 4 ,"MjG"},
        { 5 ,"BrG"},
        { 6 ,"Col"},
        { 7 ,"LtCol"},
        { 8,"Maj" },
        { 9 ,"Cpt"},
        { 10 ,"1Lt"},
        { 11 ,"2Lt"},
        { 12 ,"Sgt"},
        { 13 ,"Pvt"},
        { 14 ,"Flg"},
    };
    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        //quadMesh = BoardManager.GetInstance().quadMesh;
        //cellImage = BoardManager.GetInstance().cellImage;
        //highlightedImage = BoardManager.GetInstance().highlightedImage;
        //enemycellImage = BoardManager.GetInstance().enemyCellImage;
        entityCommandBufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate() {
        EntityCommandBuffer entityCommandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
        //Mesh quadMesh = BoardManager.GetInstance().quadMesh;
        //code for rendering highlighted cells

        Entities.
            WithoutBurst().
            //WithAll<HighlightedTag>().
            ForEach((Entity highlightedCellEntity, ref HighlightedTag highlightedTag, in Translation translation) => {

            }).Run();

        //code for rendering enemy cells
        Entities.
            WithoutBurst().
            WithAll<EnemyCellTag>().
            ForEach((ref Translation translation) => {
                Mesh quadMesh = new Mesh();
                quadMesh.name = "Quad";
                Graphics.DrawMesh(
                    quadMesh,
                    translation.Value,
                    Quaternion.identity,
                    enemycellImage,
                    0
               );
            }).Run();
    }
}
