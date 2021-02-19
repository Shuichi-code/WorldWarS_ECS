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
        #region Initializing Data

        EntityCommandBuffer entityCommandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
        EntityArchetype eventEntityArchetype = EntityManager.CreateArchetype(typeof(GameFinishedEventComponent));

        ComponentDataFromEntity<PieceOnCellComponent> pieceOnCellComponentDataArray = GetComponentDataFromEntity<PieceOnCellComponent>();
        ComponentDataFromEntity<PieceComponent> pieceComponentDataArray = GetComponentDataFromEntity<PieceComponent>();

        #endregion Initializing Data

        Entities.ForEach((Entity arbiterEntity, in ArbiterComponent arbiterComponent) =>
        {
            #region Intializing Data

            NativeArray<int> battlingRanksArray = new NativeArray<int>(2, Allocator.Temp);
            NativeArray<Entity> battlingEntitiesArray = new NativeArray<Entity>(2, Allocator.Temp);
            NativeArray<Color> battlingColorsArray = new NativeArray<Color>(2, Allocator.Temp);

            PieceComponent attackingPieceComponent = GetComponent<PieceComponent>(arbiterComponent.attackingPieceEntity);
            PieceComponent defendingPieceComponent = GetComponent<PieceComponent>(arbiterComponent.defendingPieceEntity);

            battlingRanksArray[0] = attackingPieceComponent.pieceRank;
            battlingRanksArray[1] = defendingPieceComponent.pieceRank;

            battlingEntitiesArray[0] = arbiterComponent.attackingPieceEntity;
            battlingEntitiesArray[1] = arbiterComponent.defendingPieceEntity;

            battlingColorsArray[0] = attackingPieceComponent.teamColor;
            battlingColorsArray[1] = defendingPieceComponent.teamColor;

            #endregion Intializing Data

            NativeArray<Entity> loserPieceEntities = new NativeArray<Entity>(DetermineLosers(battlingRanksArray, battlingEntitiesArray, battlingColorsArray), Allocator.Temp);

            //destroy the losing piece entities
            int i = 0;
            while (i < loserPieceEntities.Length && !loserPieceEntities[i].Equals(Entity.Null))
            {
                //Debug.Log("i is " + i + ".While deadPieceArrayLength is " + loserPieceEntities.Length);
                PieceComponent deadPieceComponent = GetComponent<PieceComponent>(loserPieceEntities[i]);
                int deadPieceRank = deadPieceComponent.pieceRank;
                Color deadPieceColor = deadPieceComponent.teamColor;

                //if the dead piece is a flag, declare a winner
                if (deadPieceRank == 14)
                {
                    Color winningColor = (deadPieceColor == Color.white) ? Color.black : Color.white;
                    DeclareWinner(entityCommandBuffer, eventEntityArchetype, winningColor);
                }

                //if both pieces are dead, destroy the PieceOnCellComponent on the cell battleground
                if (i == loserPieceEntities.Length - 1)
                {
                    entityCommandBuffer.RemoveComponent<PieceOnCellComponent>(arbiterComponent.cellBattleGround);
                }
                entityCommandBuffer.DestroyEntity(loserPieceEntities[i]);
                i++;
            }

            //Make sure all NativeArrays have been disposed
            loserPieceEntities.Dispose();
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
                Debug.Log("Triggering win event!");
                OnGameWin?.Invoke(eventComponent.winningTeamColor);
            }).Run();
        EntityManager.DestroyEntity(GetEntityQuery(typeof(GameFinishedEventComponent)));
    }

    /// <summary>
    /// Creates and EventComponent entity that triggers an event to signal that a winner has been decided
    /// </summary>
    /// <param name="entityCommandBuffer">Command Buffer for executing commands</param>
    /// <param name="eventEntityArchetype">Entity Archetype for the creation of Event Component</param>
    /// <param name="winningColor"></param>
    private static void DeclareWinner(EntityCommandBuffer entityCommandBuffer, EntityArchetype eventEntityArchetype, Color winningColor)
    {
        if (winningColor != Color.clear)
        {
            Debug.Log("Flag has fallen!");
            Entity eventEntity = entityCommandBuffer.CreateEntity(eventEntityArchetype);
            entityCommandBuffer.SetComponent<GameFinishedEventComponent>(eventEntity, new GameFinishedEventComponent { winningTeamColor = winningColor });
        }
    }

    /// <summary>
    /// Compares the rank of the pieces in battle and returns an array of pieces to be destroyed. Triggers an event when a flag has been destroyed.
    /// </summary>
    /// <param name="rankArray">Array of the attacking and defending ranks</param>
    /// <param name="entityArray">Array of the attacking and defending entities</param>
    /// <param name="colorArray">Array of the attacking and defending colors</param>
    /// <param name="entityCommandBuffer">Command buffer for executing tasks</param>
    /// <param name="arbiter">ArbiterComopnent</param>
    /// <param name="eventEntityArchetype">Entity Archetype for the creation of the event component that will trigger if a flag has passed the other side or a flag has been captured</param>
    /// <returns></returns>
    private static NativeArray<Entity> DetermineLosers(NativeArray<int> rankArray, NativeArray<Entity> entityArray, NativeArray<Color> colorArray)
    {
        #region Intializing Data

        NativeArray<Entity> loserPieceEntities = new NativeArray<Entity>(2, Allocator.Temp);
        Color winningColor = Color.clear;
        int attackingRank = rankArray[0];
        int defendingRank = rankArray[1];
        Entity attackingEntity = entityArray[0];
        Entity defendingEntity = entityArray[1];
        Color attackingColor = colorArray[0];
        Color defendingColor = colorArray[1];
        loserPieceEntities[0] = Entity.Null;
        loserPieceEntities[1] = Entity.Null;
        #endregion Intializing Data

        if (IsAttackerWInner(attackingRank, defendingRank))
        {
            //return the defending entity for destruction
            loserPieceEntities[0] = defendingEntity;
        }

        //if attacking rank is lower than defending rank or the attacker is a spy and the defender is a private, defending side wins
        else if (IsDefenderWinner(attackingRank, defendingRank))
        {
            //return attacking entity for destruction
            loserPieceEntities[0] = attackingEntity;
        }

        //if attacking rank is equal than defending rank, both pieces lose
        else if (IsRankTied(attackingRank, defendingRank))
        {
            loserPieceEntities[0] = defendingEntity;
            loserPieceEntities[1] = attackingEntity;
        }
        return loserPieceEntities;
    }

    /// <summary>
    /// Returns true if a flag attacks a flag, or if a private attacks a spy, or if the attacker has a higher rank than the defender and a spy is not attacking a private
    /// </summary>
    /// <param name="attackingRank"></param>
    /// <param name="defendingRank"></param>
    /// <returns></returns>
    private static bool IsAttackerWInner(int attackingRank, int defendingRank)
    {
        return (attackingRank == 14 && defendingRank == 14) ||
               (attackingRank == 13 && defendingRank == 0) ||
               ((attackingRank < defendingRank) && !(attackingRank == 0 && defendingRank == 13));
    }

    /// <summary>
    /// Returns true if the defender has a higher rank or if a spy is attacking a private
    /// </summary>
    /// <param name="attackingRank"></param>
    /// <param name="defendingRank"></param>
    /// <returns></returns>
    private static bool IsDefenderWinner(int attackingRank, int defendingRank)
    {
        return (attackingRank > defendingRank) ||
            (attackingRank == 0 && defendingRank == 13);
    }

    /// <summary>
    /// Returns true if the attacker and defenders have the same rank and are not flag pieces
    /// </summary>
    /// <param name="attackingRank"></param>
    /// <param name="defendingRank"></param>
    /// <returns></returns>
    private static bool IsRankTied(int attackingRank, int defendingRank)
    {
        return !(attackingRank == 14 && defendingRank == 14) && (attackingRank == defendingRank);
    }
}