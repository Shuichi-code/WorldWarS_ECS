using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Scripts.Monobehaviours.Managers
{
    public class BoardManager : MonoBehaviour
    {
        public const int MaxColumnCells = 8;
        public const int MaxRowCells = 9;

        private int boardIndex = 0;
        public const float CellZ = 1f;

        private readonly float3[] cellPositionArray = new float3[BoardManager.MaxRowCells * BoardManager.MaxColumnCells];

        private EntityManager entityManager;
        private NativeArray<Entity> boardArray;

        private static BoardManager _instance;

        public static BoardManager GetInstance()
        {
            return _instance;
        }

        private void Awake()
        {
            _instance = this;
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }
        public void CreateBoard()
        {
            EntityArchetype entityManagerArchetype = entityManager.CreateArchetype(typeof(Translation), typeof(CellTag));
            boardArray = new NativeArray<Entity>(MaxRowCells * MaxColumnCells, Allocator.Temp);
            entityManager.CreateEntity(entityManagerArchetype, boardArray);
            boardIndex = 0;
            PlaceCellsOnBoard();
        }

        public void PlaceCellsOnBoard()
        {
            for (int columns = 0; columns < MaxColumnCells; columns++)
            {
                for (int rows = 0; rows < MaxRowCells; rows++)
                {
                    float startingXCoordinate = GameManager.GetInstance().startingXCoordinate;
                    float startingYCoordinate = GameManager.GetInstance().startingYCoordinate;

                    float3 spawnPosition = new float3(startingXCoordinate + rows, startingYCoordinate + columns, CellZ);
                    cellPositionArray[boardIndex] = spawnPosition;
                    entityManager.SetComponentData(boardArray[boardIndex],
                        new Translation
                        {
                            Value = spawnPosition
                        }
                    );

                    TagEdgeCells(columns);
                    TagHomeCells(columns);
                    boardIndex++;
                }
            }
        }

        private void TagHomeCells(int columns)
        {
            var homeTeam = columns < 3 ? Team.Invader : columns > 6 ? Team.Defender : Team.Null;
            if (homeTeam == Team.Null) return;
            entityManager.AddComponent(boardArray[boardIndex], typeof(HomeCellComponent));
            entityManager.SetComponentData(boardArray[boardIndex], new HomeCellComponent
            {
                homeTeam = homeTeam
            });
        }

        /// <summary>
        /// Add the last cell component tag if cells are at the edge rows.Important for checking if Flag has passed the other side.
        /// </summary>
        /// <param name="columns"></param>
        public void TagEdgeCells(int columns)
        {
            switch (columns)
            {
                case MaxColumnCells - MaxColumnCells:
                    entityManager.AddComponent(boardArray[boardIndex], typeof(LastCellsForDefenderTag));
                    break;
                case MaxColumnCells - 1:
                    entityManager.AddComponent(boardArray[boardIndex], typeof(LastCellsForInvaderTag));
                    break;
            }
        }

        /// <summary>
        /// Set the created pieces on the cells they occupy as reference
        /// </summary>
        /// <param name="i"></param>
        /// <param name="piecePosition"></param>
        /// <param name="pieceArray"></param>
        public void SetPiecesOnCellsAsReference(int i, float3 piecePosition, NativeArray<Entity> pieceArray)
        {
            for (int j = 0; j < cellPositionArray.Length; j++)
            {
                if (!Location.IsMatchLocation(cellPositionArray[j], piecePosition)) continue;
                entityManager.AddComponent<PieceOnCellComponent>(boardArray[j]);
                //Debug.Log("Setting Piece entity to cell");
                entityManager.SetComponentData(boardArray[j],
                    new PieceOnCellComponent
                    {
                        PieceEntity = pieceArray[i]
                    }
                );
            }
        }
    }
}