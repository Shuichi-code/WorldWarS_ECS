using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

[DisableAutoCreation]
public class CheckCollisionSystem : SystemBase
{
    EntityCommandBufferSystem entityCommandBufferSystem;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        entityCommandBufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        EntityCommandBuffer entityCommandBuffer = entityCommandBufferSystem.CreateCommandBuffer();

        EntityQuery attackingTagQuery = GetEntityQuery(ComponentType.ReadOnly<AttackingPieceTag>());

        Entities.
            ForEach((Entity pieceEntity, in Translation pieceTranslation, in PieceComponent pieceComponent) => {
                //NativeArray<Entity> deadPieces = new NativeArray<Entity>();
                if (!attackingTagQuery.IsEmptyIgnoreFilter)
                {
                    //Entity attackingEntity = attackingTagQuery.GetSingletonEntity();
                    AttackingPieceTag attackingPieceTagComponent = attackingTagQuery.GetSingleton<AttackingPieceTag>();

                    if(attackingPieceTagComponent.position.x == pieceTranslation.Value.x && attackingPieceTagComponent.position.y == pieceTranslation.Value.y && attackingPieceTagComponent.teamColor != pieceComponent.teamColor)
                    {
                        Debug.Log("BeginAttack!");
                    }
                }
        }).Schedule();
    }

    private static NativeArray<Entity> checkWhichPieceIsLower(int attackingRank, int defendingRank, Entity attackingEntity, Entity defendingEntity)
    {
        NativeArray<Entity> deadEntities = new NativeArray<Entity>();
        if ((attackingRank == 14 && defendingRank == 14) ||
            (attackingRank == 13 && defendingRank == 0) ||
            ((attackingRank < defendingRank) && !(attackingRank == 0 && defendingRank == 13))
            )
        {
            //return the defending rank
            deadEntities[0] = defendingEntity;
            //TODO: Add function that checks if Flag is on the opposite side of the board
        }

        //if attacking rank is lower than defending rank, defending side wins
        else if ((attackingRank > defendingRank) || (attackingRank == 0 && defendingRank == 13))
        {
            deadEntities[0] = attackingEntity;
        }

        //if attacking rank is equal than defending rank, both sides lose
        else
        {
            deadEntities[0] = attackingEntity;
            deadEntities[1] = defendingEntity;
        }
        return deadEntities;
    }
}

