using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct PieceComponent : IComponentData
{
    public int pieceRank;
    public Color teamColor;
    public float3 originalCellPosition;
}
