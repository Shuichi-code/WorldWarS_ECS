using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

//[UpdateAfter(typeof(PiecePutDownSystem))]
public class ArbiterCheckingSystem : SystemBase
{
    private EntityCommandBufferSystem entityCommandBufferSystem;

    public delegate void GameWinnerDelegate(Color winningColor);

    public event GameWinnerDelegate OnGameWin;
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

        Entities.ForEach((Entity arbiterEntity, in ArbiterComponent arbiterComponent) =>
        {
            #region Intializing Data

            NativeArray<int> battlingRanksArray = new NativeArray<int>(2, Allocator.Temp);
            NativeArray<Entity> battlingEntitiesArray = new NativeArray<Entity>(3, Allocator.Temp);
            NativeArray<Color> battlingColorsArray = new NativeArray<Color>(2, Allocator.Temp);

            PieceComponent attackingPieceComponent = GetComponent<PieceComponent>(arbiterComponent.attackingPieceEntity);
            PieceComponent defendingPieceComponent = GetComponent<PieceComponent>(arbiterComponent.defendingPieceEntity);

            battlingRanksArray[0] = attackingPieceComponent.pieceRank;
            battlingRanksArray[1] = defendingPieceComponent.pieceRank;

            battlingEntitiesArray[0] = arbiterComponent.attackingPieceEntity;
            battlingEntitiesArray[1] = arbiterComponent.defendingPieceEntity;
            battlingEntitiesArray[2] = arbiterComponent.cellBattleGround;

            battlingColorsArray[0] = attackingPieceComponent.teamColor;
            battlingColorsArray[1] = defendingPieceComponent.teamColor;

            #endregion Intializing Data

            NativeArray<Entity> deadPieces = new NativeArray<Entity>(DetermineCombatResult(battlingRanksArray, battlingEntitiesArray, battlingColorsArray, entityCommandBuffer, eventEntityArchetype), Allocator.Temp);

            //destroy the losing piece entities
            for (int i = 0; i < deadPieces.Length; i++)
            {
                if (!deadPieces[i].Equals(Entity.Null))
                {
                    entityCommandBuffer.DestroyEntity(deadPieces[i]);
                }
            }

            //Make sure all NativeArrays have been disposed
            deadPieces.Dispose();
            battlingRanksArray.Dispose();
            battlingEntitiesArray.Dispose();
            battlingColorsArray.Dispose();

            entityCommandBuffer.DestroyEntity(arbiterEntity);
        }).Schedule();

        //Triggers the event once the eventcomponent has been created.
        Entities.
            WithoutBurst().
            ForEach((in GameFinishedEventComponent eventComponent) =>
            {
                OnGameWin?.Invoke(eventComponent.winningTeamColor);
            }).Run();
        EntityManager.DestroyEntity(GetEntityQuery(typeof(GameFinishedEventComponent)));
    }

    private static void DeclareWinner(EntityCommandBuffer entityCommandBuffer, EntityArchetype eventEntityArchetype, Color winningColor)
    {
        if (winningColor != Color.clear)
        {
            Entity eventEntity = entityCommandBuffer.CreateEntity(eventEntityArchetype);
            entityCommandBuffer.SetComponent<GameFinishedEventComponent>(eventEntity, new GameFinishedEventComponent { winningTeamColor = winningColor });
        }
    }

    /// <summary>
    /// Compares the rank of the pieces in battle and returns an array of pieces to be destroyed
    /// </summary>
    /// <param name="rankArray">Array of the attacking and defending ranks</param>
    /// <param name="entityArray">Array of the attacking and defending entities</param>
    /// <param name="colorArray">Array of the attacking and defending colors</param>
    /// <param name="entityCommandBuffer">Command buffer for executing tasks</param>
    /// <param name="arbiter">ArbiterComopnent</param>
    /// <param name="eventEntityArchetype">Entity Archetype for the creation of the event component that will trigger if a flag has passed the other side or a flag has been captured</param>
    /// <returns></returns>
    private static NativeArray<Entity> DetermineCombatResult(NativeArray<int> rankArray, NativeArray<Entity> entityArray, NativeArray<Color> colorArray, EntityCommandBuffer entityCommandBuffer, EntityArchetype eventEntityArchetype)
    {
        #region Intializing Data

        NativeArray<Entity> deadEntities = new NativeArray<Entity>(2, Allocator.Temp);
        Color winningColor = Color.clear;
        int attackingRank = rankArray[0];
        int defendingRank = rankArray[1];
        Entity attackingEntity = entityArray[0];
        Entity defendingEntity = entityArray[1];
        Entity cellBattleGround = entityArray[2];
        Color attackingColor = colorArray[0];
        Color defendingColor = colorArray[1];

        #endregion Intializing Data

        if (IsAttackerWInner(attackingRank, defendingRank))
        {
            //if a flag is attacking a flag or if the defender is a flag; attacker wins
            if ((attackingRank == 14 && defendingRank == 14) || defendingRank == 14)
            {
                //get the attacking team flag's color and declare him the winner
                winningColor = attackingColor;
            }
            //return the defending entity for destruction
            deadEntities[0] = defendingEntity;
            deadEntities[1] = Entity.Null;
        }

        //if attacking rank is lower than defending rank or the attacker is a spy and the defender is a private, defending side wins
        else if (IsDefenderWinner(attackingRank, defendingRank))
        {
            //if attacking rank is flag, defending side loses
            if (attackingRank == 14)
            {
                winningColor = defendingColor;
            }
            //return attacking entity for destruction
            deadEntities[0] = attackingEntity;
            deadEntities[1] = Entity.Null;
        }

        //if attacking rank is equal than defending rank, both pieces lose
        else if (IsRankTied(attackingRank, defendingRank))
        {
            deadEntities[0] = defendingEntity;
            deadEntities[1] = attackingEntity;
            entityCommandBuffer.RemoveComponent<PieceOnCellComponent>(cellBattleGround);
        }
        //trigger winning event when winner has been decided
        DeclareWinner(entityCommandBuffer, eventEntityArchetype, winningColor);
        return deadEntities;
    }

    private static bool IsAttackerWInner(int attackingRank, int defendingRank)
    {
        return (attackingRank == 14 && defendingRank == 14) ||
               (attackingRank == 13 && defendingRank == 0) ||
               ((attackingRank < defendingRank) && !(attackingRank == 0 && defendingRank == 13));
    }

    private static bool IsDefenderWinner(int attackingRank, int defendingRank)
    {
        return (attackingRank > defendingRank) ||
            (attackingRank == 0 && defendingRank == 13);
    }

    private static bool IsRankTied(int attackingRank, int defendingRank)
    {
        return !(attackingRank == 14 && defendingRank == 14) && attackingRank == defendingRank;
    }
}