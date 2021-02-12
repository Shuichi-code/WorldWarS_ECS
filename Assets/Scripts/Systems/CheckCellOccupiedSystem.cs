using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[DisableAutoCreation]
public class CheckCellOccupiedSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.
            WithoutBurst().
            ForEach((ref CellComponent cellComponent, in Translation translation) =>
            {
                //EntityQuery getPieces = GetEntityQuery(ComponentType.ReadOnly<PieceComponent>());
                EntityQuery getPieces = GetEntityQuery(ComponentType.ReadOnly<PieceComponent>());
                NativeArray<PieceComponent> pieceArray = getPieces.ToComponentDataArray<PieceComponent>(Allocator.Temp);
                if (!getPieces.IsEmptyIgnoreFilter)
                {
                    //NativeArray<PieceComponent> pieceArray = getPieces.ToComponentDataArray<PieceComponent>(Allocator.Temp);

                    foreach (PieceComponent piece in pieceArray)
                    {
                        if (translation.Value.x == piece.originalCellPosition.x && translation.Value.y == piece.originalCellPosition.y)
                        {
                            //Debug.Log("Cell has piece!");
                            cellComponent.hasPiece = true;
                            break;
                        }
                        else
                        {
                            cellComponent.hasPiece = false;
                        }
                    }
                }
                pieceArray.Dispose();
            }).Run();

    }
}
