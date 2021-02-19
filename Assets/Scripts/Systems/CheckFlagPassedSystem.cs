using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

//This code is on hold due to freezing when implementing
[DisableAutoCreation]
public class CheckFlagPassedSystem : SystemBase
{
    private EntityCommandBufferSystem entityCommandBufferSystem;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        entityCommandBufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityArchetype eventEntityArchetype = EntityManager.CreateArchetype(typeof(GameFinishedEventComponent));

        EntityCommandBuffer entityCommandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
        Entities.
            ForEach((Entity cellEntity, in PieceOnCellComponent pieceOnCellComponent) =>
            {
                int pieceRank = GetComponent<PieceComponent>(pieceOnCellComponent.pieceEntity).pieceRank;
                Color pieceOnCellColor = GetComponent<PieceComponent>(pieceOnCellComponent.pieceEntity).teamColor;
                if ((HasComponent<LastCellsForBlackTag>(cellEntity) && pieceOnCellColor == Color.black && pieceRank == 14) ||
                    (HasComponent<LastCellsForWhiteTag>(cellEntity) && pieceOnCellColor == Color.white && pieceRank == 14))
                {
                    //player wins
                    Entity eventEntity = entityCommandBuffer.CreateEntity(eventEntityArchetype);
                    entityCommandBuffer.SetComponent<GameFinishedEventComponent>(eventEntity, new GameFinishedEventComponent { winningTeamColor = pieceOnCellColor });
                }
            }).Run();
    }
}