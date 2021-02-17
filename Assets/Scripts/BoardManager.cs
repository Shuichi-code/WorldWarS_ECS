using System;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Rendering;

public class BoardManager : MonoBehaviour
{
    private static BoardManager instance;
    public static BoardManager GetInstance()
    {
        return instance;
    }

    public Mesh quadMesh;
    public Material cellImage;
    public Material highlightedImage;
    public Material enemyCellImage;
    private int boardIndex = 0;
    private float3 piecePosition;

    NativeArray<Entity> boardArray;
    NativeArray<Entity> pieceArray;

    EntityManager entityManager;
    EntityArchetype entityGameManagerArchetype;
    EntityArchetype entityArchetype;

    const int maxRowCells = 9;
    const int maxColumnCells = 8;

    //default arrangement of pieces in the board. numbers represent the piecerank
    int[] mPieceOrder = new int[21]
    {
        1, 2, 3, 4, 5, 6, 7,
        8, 9, 10, 11, 12, 13,13,
        13,13,13,13, 0, 0, 14
    };

    float3[,] cellposition = new float3[maxRowCells,maxColumnCells];

    private System.Collections.Generic.Dictionary<int, string> mPieceRank = new System.Collections.Generic.Dictionary<int, string>()
    {
        { 0 ,"Spy"},
        { 1 ,"G5S" },
        { 2 ,"G4S" },
        { 3 ,"LtG"},
        { 4 ,"MjG"},
        { 5 ,"BrG"},
        { 6 ,"Col"},
        { 7 ,"LtCol"},
        { 8,"Maj" },
        { 9 ,"Cpt"},
        { 10 ,"1Lt"},
        { 11 ,"2Lt"},
        { 12 ,"Sgt"},
        { 13 ,"Pvt"},
        { 14 ,"Flg"},
    };

    float3[] cellPositionArray = new float3[maxRowCells*maxColumnCells];
    private float3[] neighborCellPositionArray = new float3[4];
    float3 spawnPosition;

    private void Awake()
    {
        instance = this;

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        entityGameManagerArchetype = entityManager.CreateArchetype(typeof(GameManagerComponent));
        Entity gameManager = entityManager.CreateEntity(entityGameManagerArchetype);
        entityManager.SetComponentData(gameManager,
            new GameManagerComponent
            {
                state = GameManagerComponent.State.Playing,
                isDragging = false,
                teamToMove = Color.white
            });

        createBoard();
        createPieces(Color.white);
        createPieces(Color.black);
    }

    /// <summary>
    /// Creates the board by instantiating cell entities based on maxRow vs maxCol
    /// </summary>
    public void createBoard()
    {
        entityArchetype = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(CellComponent)
        );
        boardArray = new NativeArray<Entity>(maxRowCells * maxColumnCells, Allocator.Temp);
        entityManager.CreateEntity(entityArchetype, boardArray);
        boardIndex = 0;
        for (int columns = 0; columns < maxColumnCells; columns++)
        {
            for (int rows = 0; rows < maxRowCells; rows++)
            {
                spawnPosition = new float3(-4f+rows, -4f+columns, 50);
                cellPositionArray[boardIndex] = spawnPosition;
                cellposition[rows, columns] = spawnPosition;
                entityManager.SetComponentData(boardArray[boardIndex],
                    new Translation
                    {
                        Value = spawnPosition
                    }
                );
                //if cells are at the edge rows then add the last cell component tag
                if(columns == maxColumnCells - maxColumnCells)
                {
                    entityManager.AddComponent(boardArray[boardIndex], typeof(LastCellsForBlackTag));
                }
                else if (columns == maxColumnCells - 1)
                {
                    entityManager.AddComponent(boardArray[boardIndex], typeof(LastCellsForWhiteTag));
                }

                boardIndex++;
            }
        }
        setNeighbors();
    }

    /// <summary>
    /// Sets the neighborbuffers for each of the cell elements
    /// </summary>
    private void setNeighbors()
    {
        for(int i = 0; i < maxRowCells*maxColumnCells; i++ )
        {
            float3 currentCellPosition = cellPositionArray[i];
            Entity currentCell = boardArray[i];

            float cellXCoordinate = cellPositionArray[i].x;
            float cellYCoordinate = cellPositionArray[i].y;
            neighborCellPositionArray[0] = new float3(cellXCoordinate, cellYCoordinate + 1f, cellPositionArray[i].z);
            //surroundCellList.Add(new float3(dragPiecePosition.Value.x + 1, dragPiecePosition.Value.y + 1, dragPiecePosition.Value.z));
            neighborCellPositionArray[1] = new float3(cellXCoordinate + 1f, cellYCoordinate, cellPositionArray[i].z);
            //surroundCellList.Add(new float3(dragPiecePosition.Value.x + 1, dragPiecePosition.Value.y - 1, dragPiecePosition.Value.z));
            neighborCellPositionArray[2] = new float3(cellXCoordinate, cellYCoordinate - 1f, cellPositionArray[i].z);
            //surroundCellList.Add(new float3(dragPiecePosition.Value.x - 1, dragPiecePosition.Value.y - 1, dragPiecePosition.Value.z));
            neighborCellPositionArray[3] = new float3(cellXCoordinate - 1f, cellYCoordinate, cellPositionArray[i].z);

            foreach (float3 neighborCellPosition in neighborCellPositionArray)
            {
                for (int j = 0; j < cellPositionArray.Length; j++)
                {
                    if (neighborCellPosition.x == cellPositionArray[j].x && neighborCellPosition.y == cellPositionArray[j].y)
                    {
                        DynamicBuffer<CellNeighborBufferElement> cellNeighborBuffers = entityManager.AddBuffer<CellNeighborBufferElement>(boardArray[i]);
                        cellNeighborBuffers.Add(new CellNeighborBufferElement { cellNeighbor = boardArray[j] });
                    }
                }

            }
        }
    }

    /// <summary>
    /// Instantiates the piece entities
    /// </summary>
    public void createPieces(Color color)
    {
        entityArchetype = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(PieceComponent),
            typeof(PieceTag)
        );

        pieceArray = new NativeArray<Entity>(21, Allocator.Temp);
        entityManager.CreateEntity(entityArchetype, pieceArray);

        for (int i = 0; i < mPieceOrder.Length; i++)
        {
            //Place the pieces on the board
            if (color == Color.white)
            {
                //Placing white pieces
                if (i < 9)
                {
                    piecePosition = new float3(cellposition[i, 2].x, cellposition[i, 2].y, cellposition[i, 2].z - 10f);
                }
                else if (i >= 9 && i < 18)
                {
                    piecePosition = new float3(cellposition[i - 9, 1].x, cellposition[i - 9, 1].y, cellposition[i - 9, 1].z - 10f);
                }
                else
                {
                    piecePosition = new float3(cellposition[i - 18, 0].x, cellposition[i - 18, 0].y, cellposition[i - 18, 0].z - 10f);
                }

            }
            else
            {
                //Placing black pieces
                if (i < 9)
                {
                    piecePosition = new float3(cellposition[i, 5].x, cellposition[i, 5].y, cellposition[i, 5].z - 10f);
                }
                else if (i >= 9 && i < 18)
                {
                    piecePosition = new float3(cellposition[i - 9, 6].x, cellposition[i - 9, 6].y, cellposition[i - 9, 6].z - 10f);
                }
                else
                {
                    piecePosition = new float3(cellposition[i - 18, 7].x, cellposition[i - 18, 7].y, cellposition[i - 18, 7].z - 10f);
                }
            }
            entityManager.SetComponentData(pieceArray[i], new Translation { Value = piecePosition });
            entityManager.SetComponentData(pieceArray[i],
                 new PieceComponent
                 {
                     originalCellPosition = piecePosition,
                     pieceRank = mPieceOrder[i],
                     teamColor = color
                 }
             );

            //setting the pieces on the cell as reference
            for (int j = 0; j < cellPositionArray.Length; j++)
            {
                if(cellPositionArray[j].x == piecePosition.x && cellPositionArray[j].y == piecePosition.y)
                {
                    entityManager.AddComponent<PieceOnCellComponent>(boardArray[j]);
                    entityManager.SetComponentData(boardArray[j],
                        new PieceOnCellComponent
                        {
                            pieceEntity = pieceArray[i]
                        }
                    );
                }
            }
        }
    }
}
