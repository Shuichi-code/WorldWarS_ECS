using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class RenderSystem : ComponentSystem
{
    private Mesh quadMesh;
    private Material cellImage;

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
        quadMesh = BoardManager.GetInstance().quadMesh;
        cellImage = BoardManager.GetInstance().cellImage;
    }
    protected override void OnUpdate() {

        #region For Graphcs.DrawMeshInstanced
        //this code is for Graphics.DrawMeshInstanced
        /*EntityQuery entityQuery = GetEntityQuery(typeof(CellComponent));

        NativeArray<CellComponent> cellArray = entityQuery.ToComponentDataArray<CellComponent>(Allocator.Temp);

        List<Matrix4x4> cellList = new List<Matrix4x4>();
        for (int i = 0; i < cellArray.Length; i++)
        {
            cellList.Add(cellArray[i].matrix);
        }

        //MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        //material.enableInstancing = true;

        Graphics.DrawMeshInstanced(
            quadMesh,
            0,
            cellImage,
            cellList
        );*/
        #endregion


        //code for drawing the cells
        Entities.WithAll<CellComponent>().
            ForEach((ref Translation translation, ref CellComponent cellComponent) => {
            Graphics.DrawMesh(
                quadMesh,
                translation.Value,
                Quaternion.identity,
                cellImage,
                0
            );
        });

        //code for rendering the pieces
        Entities.WithAll<PieceComponent>()
            .ForEach((ref Translation translation, ref PieceComponent piece)=> {
            Graphics.DrawMesh(
                quadMesh,
                translation.Value,
                Quaternion.identity,
                Resources.Load(mPieceRank[piece.pieceRank], typeof(Material)) as Material,
                0
            );
        });
    }
}
