using System;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class PiecePickupSystem : SystemBase
{
    private EntityCommandBufferSystem entityCommandBuffer;

    protected override void OnStartRunning()
    {
        base.OnCreate();
        entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        #region Initializing Data

        //var ecb = new EntityCommandBuffer(Allocator.TempJob);//entityCommandBuffer.CreateCommandBuffer();
        var ecb = entityCommandBuffer.CreateCommandBuffer();
        //get the gamemanager Entity
        EntityQuery gameManagerQuery = GetEntityQuery(typeof(GameManagerComponent));
        Entity gameManagerEntity = gameManagerQuery.GetSingletonEntity();
        ComponentDataFromEntity<GameManagerComponent> gameManagerArray = GetComponentDataFromEntity<GameManagerComponent>();

        EntityQuery cellWithPieceQuery = GetEntityQuery(typeof(PieceOnCellComponent));
        ComponentDataFromEntity<PieceOnCellComponent> cellWithPieceArray = GetComponentDataFromEntity<PieceOnCellComponent>();

        //gets the mouseposition to check which piece is being dragged
        float3 mousePos = Input.mousePosition;
        float3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

        #endregion Initializing Data

        //job for iterating through all the pieces and puts a tag on the entity nearest the mouse when the left click is held down
        Entities.
            ForEach((Entity pieceEntity, in Translation pieceTranslation, in PieceComponent pieceComponent) =>
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Color teamColorToMove = gameManagerArray[gameManagerEntity].teamToMove;
                    if (IsDragPieceValid(pieceTranslation, pieceComponent, gameManagerEntity, ref gameManagerArray, worldPos, teamColorToMove))
                    {
                        if (!HasComponent<SelectedTag>(pieceEntity))
                        {
                            ecb.AddComponent<SelectedTag>(pieceEntity);
                        }

                        ecb.SetComponent(gameManagerEntity,
                            new GameManagerComponent
                            {
                                isDragging = true,
                                state = State.Playing,
                                teamToMove = gameManagerArray[gameManagerEntity].teamToMove
                            }
                        );
                    }
                }
            }).Run();
    }

    /// <summary>
    /// Returns true if the piece clicked by player is nearest the mouse pointer, it is his team to move, and the the player did not click on the last frame
    /// </summary>
    /// <param name="pieceTranslation"></param>
    /// <param name="pieceComponent"></param>
    /// <param name="gameManagerEntity"></param>
    /// <param name="gameManagerArray"></param>
    /// <param name="worldPos"></param>
    /// <param name="teamColorToMove"></param>
    /// <returns></returns>
    private static bool IsDragPieceValid(Translation pieceTranslation, PieceComponent pieceComponent, Entity gameManagerEntity, ref ComponentDataFromEntity<GameManagerComponent> gameManagerArray, float3 worldPos, Color teamColorToMove)
    {
        return MousePointerHasFoundPiece(pieceTranslation, worldPos) &&
                            teamColorToMove == pieceComponent.teamColor &&
                            !gameManagerArray[gameManagerEntity].isDragging;
    }

    /// <summary>
    /// Returns true if the mouse position is nearest the piece position
    /// </summary>
    /// <param name="pieceTranslation"></param>
    /// <param name="worldPos"></param>
    /// <returns></returns>
    private static bool MousePointerHasFoundPiece(Translation pieceTranslation, float3 worldPos)
    {
        return (pieceTranslation.Value.x == Math.Round(worldPos.x) && pieceTranslation.Value.y == Math.Round(worldPos.y)) ? true : false;
    }
}