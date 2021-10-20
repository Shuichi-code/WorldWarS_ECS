using System.ComponentModel;
using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;

[DisableAutoCreation]
public class AISystem : SystemBase
{
    protected override void OnUpdate()
    {
        //get the enemy player's team

        var enemyPlayerQuery = GetEntityQuery(ComponentType.ReadOnly<EnemyTag>(), ComponentType.ReadOnly <TimeComponent>(), ComponentType.ReadOnly<TeamComponent>());
        var enemyTeam = enemyPlayerQuery.GetSingleton<TeamComponent>().myTeam;
        var gmQuery = GetEntityQuery(ComponentType.ReadOnly<GameManagerComponent>());
        var teamToMove = gmQuery.GetSingleton<GameManagerComponent>().teamToMove;

        //check if it is his time to move
        if (enemyTeam != teamToMove) return;

        //get available moves
        var moves = new NativeList<float3>(Allocator.TempJob);
        moves = GetPossibleMoves(teamToMove, moves);

        moves.Dispose();

    }

    private NativeList<float3> GetPossibleMoves(Team teamToMove, NativeList<float3> moves)
    {
        var pieceLocationList = new NativeList<float3>(Allocator.TempJob);
        var validMovesArray = new NativeArray<float3>(4, Allocator.TempJob);
        var getPieceLocationJob = Entities.
            WithAll<PieceTag>().
            WithNone<PrisonerTag>().
            ForEach((in Translation pieceTranslation, in TeamComponent teamComponent) =>
            {
                if (teamComponent.myTeam != teamToMove) return;
                pieceLocationList.Add(pieceTranslation.Value);

            }).Schedule(Dependency);

        foreach (var pieceLocation in pieceLocationList)
        {
            validMovesArray = GetValidMoves(pieceLocation, validMovesArray);
            foreach (var validMove in validMovesArray)
            {
                moves.Add(validMove);
                RemoveInvalidMove(moves, validMove, getPieceLocationJob);
            }

        }


        pieceLocationList.Dispose();
        validMovesArray.Dispose();
        return moves;
    }

    /// <summary>
    /// Compares the valid moves with the location of the pieces
    /// </summary>
    /// <param name="moves"></param>
    /// <param name="validMove"></param>
    /// <param name="getPieceLocationJob"></param>
    private void RemoveInvalidMove(NativeList<float3> moves, float3 validMove, JobHandle getPieceLocationJob)
    {
        Entities.
            WithAll<PieceTag, EnemyTag>().
            WithNone<PrisonerTag>().
            ForEach((Entity e, in Translation pieceTranslation) =>
        {
            if (Location.IsMatchLocation(validMove, pieceTranslation.Value) && !HasComponent<EnemyTag>(e))
            {
                moves.RemoveAt(moves.Length);
            }
        }).Schedule(getPieceLocationJob);
        Dependency.Complete();
    }

    private NativeArray<float3> GetValidMoves(float3 pieceLocation, NativeArray<float3> validMovesArray)
    {
        validMovesArray[0] = new float3(pieceLocation.x + 1, pieceLocation.y, pieceLocation.z);
        validMovesArray[1] = new float3(pieceLocation.x - 1, pieceLocation.y, pieceLocation.z);
        validMovesArray[2] = new float3(pieceLocation.x, pieceLocation.y + 1, pieceLocation.z);
        validMovesArray[3] = new float3(pieceLocation.x, pieceLocation.y - 1, pieceLocation.z);

        return validMovesArray;
    }
}
