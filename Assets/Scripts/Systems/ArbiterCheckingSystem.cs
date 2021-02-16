using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

//[UpdateAfter(typeof(PiecePutDownSystem))]
public class ArbiterCheckingSystem : SystemBase
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
        ComponentDataFromEntity<PieceOnCellComponent> pieceOnCellComponentDataArray = GetComponentDataFromEntity<PieceOnCellComponent>();

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
                //return the defending rank
                //NativeArray<Entity> deadEntities = new NativeArray<Entity>(1, Allocator.Temp);
                entityCommandBuffer.DestroyEntity(defendingEntity);
                entityCommandBuffer.SetComponent<PieceOnCellComponent>(arbiter.cellBattleGround, new PieceOnCellComponent { piece = attackingEntity });
                //TODO: Add function that checks if Flag is on the opposite side of the board
            }

            //if attacking rank is lower than defending rank, defending side wins
            else if ((attackingRank > defendingRank) || (attackingRank == 0 && defendingRank == 13))
            {
                //NativeArray<Entity> deadEntities = new NativeArray<Entity>(1, Allocator.Temp);
                entityCommandBuffer.DestroyEntity(attackingEntity);
                entityCommandBuffer.SetComponent<PieceOnCellComponent>(arbiter.cellBattleGround, new PieceOnCellComponent { piece = defendingEntity });
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
        }).Run();
    }
}
