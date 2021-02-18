using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct ArbiterComponent : IComponentData
{

    public Entity attackingPieceEntity;
    public Entity defendingPieceEntity;
    public Entity cellBattleGround;

}
