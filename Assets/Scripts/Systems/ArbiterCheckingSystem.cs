using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

//[UpdateAfter(typeof(PiecePutDownSystem))]
public class ArbiterCheckingSystem : SystemBase
{
    private EntityCommandBufferSystem entityCommandBufferSystem;

    public event GameWinnerDelegate OnGameWin;
    public delegate void GameWinnerDelegate(Color winningColor);

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        entityCommandBufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        EntityCommandBuffer entityCommandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
        EntityArchetype eventEntityArchetype = EntityManager.CreateArchetype(typeof(GameFinishedEventComponent));

        ComponentDataFromEntity<PieceOnCellComponent> pieceOnCellComponentDataArray = GetComponentDataFromEntity<PieceOnCellComponent>();
        ComponentDataFromEntity<PieceComponent> pieceComponentDataArray = GetComponentDataFromEntity<PieceComponent>();

        EntityQuery gameManagerQuery = GetEntityQuery(typeof(GameManagerComponent));
        Entity gameManagerEntity = gameManagerQuery.GetSingletonEntity();
        ComponentDataFromEntity<GameManagerComponent> gameManagerArray = GetComponentDataFromEntity<GameManagerComponent>();


        Entities.ForEach((Entity entity, ref ArbiterComponent arbiter) => {
            int attackingRank = arbiter.attackingPiecerank;
            int defendingRank = arbiter.defendingPieceRank;
            Entity attackingEntity = arbiter.attackingPieceEntity;
            Entity defendingEntity = arbiter.defendingPieceEntity;
            NativeArray<Entity> deadEntities = new NativeArray<Entity>(2, Allocator.Temp);

            //NativeArray<Entity> deadPieces = new NativeArray<Entity>(checkWhichPieceIsLower(attackingRank,defendingRank,attackingEntity,defendingEntity), Allocator.Temp);
            if ((attackingRank == 14 && defendingRank == 14) ||
                (attackingRank == 13 && defendingRank == 0) ||
                ((attackingRank < defendingRank) && !(attackingRank == 0 && defendingRank == 13))
                )
            {
                if((attackingRank == 14 && defendingRank == 14) || defendingRank == 14 )
                {
                    //get the attacking team flag's color and declare him the winner
                    Color winningColor = pieceComponentDataArray[attackingEntity].teamColor;
                    Entity eventEntity = entityCommandBuffer.CreateEntity(eventEntityArchetype);
                    entityCommandBuffer.SetComponent<GameFinishedEventComponent>(eventEntity, new GameFinishedEventComponent { winningTeamColor = winningColor });
                    //Debug.Log("Sending out the winner team!");
                }
                //return the defending rank
                //NativeArray<Entity> deadEntities = new NativeArray<Entity>(1, Allocator.Temp);
                entityCommandBuffer.DestroyEntity(defendingEntity);
                entityCommandBuffer.SetComponent<PieceOnCellComponent>(arbiter.cellBattleGround, new PieceOnCellComponent { pieceEntity = attackingEntity });
                //TODO: Add function that checks if Flag is on the opposite side of the board
            }

            //if attacking rank is lower than defending rank, defending side wins
            else if ((attackingRank > defendingRank) || (attackingRank == 0 && defendingRank == 13))
            {
                //if attacking rank is flag, opposite side loses
                if(attackingRank == 14)
                {
                    Color winningColor = pieceComponentDataArray[defendingEntity].teamColor;
                    Entity eventEntity = entityCommandBuffer.CreateEntity(eventEntityArchetype);
                    entityCommandBuffer.SetComponent<GameFinishedEventComponent>(eventEntity, new GameFinishedEventComponent { winningTeamColor = winningColor });
                }
                //NativeArray<Entity> deadEntities = new NativeArray<Entity>(1, Allocator.Temp);
                entityCommandBuffer.DestroyEntity(attackingEntity);
                entityCommandBuffer.SetComponent<PieceOnCellComponent>(arbiter.cellBattleGround, new PieceOnCellComponent { pieceEntity = defendingEntity });
            }

            //if attacking rank is equal than defending rank, both sides lose
            else
            {
                //NativeArray<Entity> deadEntities = new NativeArray<Entity>(2, Allocator.Temp);
                entityCommandBuffer.RemoveComponent<PieceOnCellComponent>(arbiter.cellBattleGround);
                entityCommandBuffer.DestroyEntity(attackingEntity);
                entityCommandBuffer.DestroyEntity(defendingEntity);
            }
            entityCommandBuffer.DestroyEntity(entity);
            deadEntities.Dispose();
        }).Schedule();

        Entities.
            WithoutBurst().
            ForEach((in GameFinishedEventComponent eventComponent)=> {
                OnGameWin?.Invoke(eventComponent.winningTeamColor);
            }).Run();
        EntityManager.DestroyEntity(GetEntityQuery(typeof(GameFinishedEventComponent)));
    }
}
