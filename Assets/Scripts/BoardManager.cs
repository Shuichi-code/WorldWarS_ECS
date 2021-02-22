using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public Material cellImage;
    public Material enemyCellImage;
    public Material highlightedImage;
    public GameObject piecePlaceCanvas;

    public System.Collections.Generic.Dictionary<int, string> mPieceRank = new System.Collections.Generic.Dictionary<int, string>()
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

    public Mesh quadMesh;
    private const int maxColumnCells = 8;
    private const int maxRowCells = 9;
    private static BoardManager instance;

    private NativeArray<Entity> boardArray;

    private int boardIndex = 0;

    private float3[,] cellposition = new float3[maxRowCells, maxColumnCells];

    private float3[] cellPositionArray = new float3[maxRowCells * maxColumnCells];

    private EntityArchetype entityArchetype;

    private EntityArchetype entityGameManagerArchetype;

    private EntityManager entityManager;

    //default arrangement of pieces in the board. numbers represent the piecerank
    private int[] defaultPieceArrangementArray = new int[21]
    {
        1, 2, 3, 4, 5, 6, 7, 8, 9,
        10, 11, 12, 13,13,13,13,13,13,
        0, 0, 14
    };

    //Blitzkrieg-left arrangement
    private int[] blitzkriegLeftPieceArrangementArray = new int[21]
    {
        1, 2, 0, 13, 13, 10, 7, 8, 9,
        3, 4, 5, 13,13,13,13,11,12,
        14, 0, 6
    };

    //Blitzkrieg- right arrangement
    private int[] blitzkriegRightPieceArrangementArray = new int[21]
    {
        9, 8, 7, 13, 13, 10, 0, 2, 1,
        12, 11, 13, 13,13,13,5,4,3,
        6, 0, 14
    };

    //Mothership arrangement
    private int[] mothershipPieceArrangementArray = new int[21]
    {
        9,  8,   13, 0, 3, 0, 13, 5, 6,
        12, 11, 13, 1, 14, 2, 13, 4, 7,
        10, 13, 13
    };

    //Box arrangement
    private int[] boxPieceArrangementArray = new int[21]
    {
            1, 2, 0, 0, 3, 4, 5, 6, 7, 
            8, 9, 10, 11, 12, 13,13,13,13,
            13, 13, 14
    };

    //TODO: Random place arrangement
    private int[] randomArrangementArray = new int[]    
    {
        1, 2, 3, 4, 5, 6, 7, 8, 9,
        10, 11, 12, 13,13,13,13,13,13,
        0, 0, 14
    };

    private NativeArray<Entity> pieceArray;

    private float3 spawnPosition;

    public static BoardManager GetInstance()
    {
        return instance;
    }

    /// <summary>
    /// Creates the board by instantiating cell entities based on maxRow vs maxCol
    /// </summary>
    public void CreateBoard()
    {
        entityArchetype = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(CellComponent)
        );
        boardArray = new NativeArray<Entity>(maxRowCells * maxColumnCells, Allocator.Temp);
        entityManager.CreateEntity(entityArchetype, boardArray);
        boardIndex = 0;
        PlaceCellsOnBoard();
        SetNeighbors();
    }

    /// <summary>
    /// Creates the piece entities
    /// </summary>
    public void CreatePieces(Color color)
    {
        entityArchetype = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(PieceComponent),
            typeof(PieceTag)
        );

        pieceArray = new NativeArray<Entity>(defaultPieceArrangementArray.Length, Allocator.Temp);
        entityManager.CreateEntity(entityArchetype, pieceArray);
        //TODO: Create method of placing the entities in a UI that the player can drag and drop on the board.
        if (color == Color.white)
        {
            PlayerPlacePieces(color);
        }
        else
        {
            Placepieces(color);
        }
        //PlayerPlacePieces(color);
        //Placepieces(color);
    }

    private void PlayerPlacePieces(Color color)
    {
        //Turns on the UI where the piece entities shall be put.
        //Change the games state to Waiting to Start
        //Have all the piece entities be rendered on that UI
        //throw new NotImplementedException();
    }

    /// <summary>
    /// Places pieces on the board on default places. This is only for testing purposes.
    /// </summary>
    /// <param name="teamColor"></param>
    public void Placepieces(Color teamColor)
    {
        int[] ycoordinateArray = SetColumnPlaces(teamColor);
        int pieceIndex = 0;
        while (pieceIndex < defaultPieceArrangementArray.Length)
        {
            int xcoordinate = (pieceIndex < 9) ? (pieceIndex - 4) : (pieceIndex >= 9 && pieceIndex < 18) ? (pieceIndex - 9 - 4) : (pieceIndex - 18 - 4);
            int ycoordinate = (pieceIndex < 9) ? (ycoordinateArray[0] - 4) : (pieceIndex >= 9 && pieceIndex < 18) ? (ycoordinateArray[1] - 4) : ycoordinateArray[2] - 4;
            float3 piecePosition = new float3(xcoordinate, ycoordinate, 50);

            //Place the pieces on the board
            entityManager.SetComponentData(pieceArray[pieceIndex], new Translation { Value = piecePosition });
            entityManager.SetComponentData(pieceArray[pieceIndex],
                 new PieceComponent
                 {
                     originalCellPosition = piecePosition,
                     pieceRank = defaultPieceArrangementArray[pieceIndex],
                     teamColor = teamColor
                 }
             );

            //setting the pieces on the cell as reference
            SetPiecesOnCellsAsReference(pieceIndex, piecePosition);
            pieceIndex++;
        }
    }

    private void Awake()
    {
        instance = this;

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        entityGameManagerArchetype = entityManager.CreateArchetype(typeof(GameManagerComponent));
        Entity gameManager = entityManager.CreateEntity(entityGameManagerArchetype);
        entityManager.SetComponentData(gameManager,
            new GameManagerComponent
            {
                state = State.Playing,
                isDragging = false,
                teamToMove = Color.white
            });

        CreateBoard();
        CreatePieces(Color.white);
        CreatePieces(Color.black);
    }

    /// <summary>
    /// Sets the allowable neighbor cells or moves for the current cells.
    /// </summary>
    /// <param name="i"></param>
    /// <param name="cellXCoordinate"></param>
    /// <param name="cellYCoordinate"></param>
    /// <param name="neighborCellPositionArray"></param>
    /// <returns></returns>
    private float3[] FillUpValidNeighborCells(int i, float cellXCoordinate, float cellYCoordinate, float3[] neighborCellPositionArray)
    {
        neighborCellPositionArray[0] = new float3(cellXCoordinate, cellYCoordinate + 1f, cellPositionArray[i].z); //Up
        //surroundCellList.Add(new float3(dragPiecePosition.Value.x + 1, dragPiecePosition.Value.y + 1, dragPiecePosition.Value.z));
        neighborCellPositionArray[1] = new float3(cellXCoordinate + 1f, cellYCoordinate, cellPositionArray[i].z); //Left
        //surroundCellList.Add(new float3(dragPiecePosition.Value.x + 1, dragPiecePosition.Value.y - 1, dragPiecePosition.Value.z));
        neighborCellPositionArray[2] = new float3(cellXCoordinate, cellYCoordinate - 1f, cellPositionArray[i].z); //Right
        //surroundCellList.Add(new float3(dragPiecePosition.Value.x - 1, dragPiecePosition.Value.y - 1, dragPiecePosition.Value.z));
        neighborCellPositionArray[3] = new float3(cellXCoordinate - 1f, cellYCoordinate, cellPositionArray[i].z); //Down

        return neighborCellPositionArray;
    }

    private void PlaceCellsOnBoard()
    {
        for (int columns = 0; columns < maxColumnCells; columns++)
        {
            for (int rows = 0; rows < maxRowCells; rows++)
            {
                spawnPosition = new float3(-4f + rows, -4f + columns, 50);
                cellPositionArray[boardIndex] = spawnPosition;
                cellposition[rows, columns] = spawnPosition;
                entityManager.SetComponentData(boardArray[boardIndex],
                    new Translation
                    {
                        Value = spawnPosition
                    }
                );

                TagEdgeCells(columns);
                boardIndex++;
            }
        }
    }

    /// <summary>
    /// Sets the possible y-coordinates of the pieces based on the piece team color
    /// </summary>
    /// <param name="teamColor"></param>
    /// <returns></returns>
    private int[] SetColumnPlaces(Color teamColor)
    {
        if (teamColor == Color.white)
        {
            int[] columnsArray = { 2, 1, 0 };
            return columnsArray;
        }
        else
        {
            int[] columnsArray = { 5, 6, 7 };
            return columnsArray;
        }
    }

    /// <summary>
    /// Sets the neighborbuffers for each of the cell elements
    /// </summary>
    private void SetNeighbors()
    {
        for (int i = 0; i < maxRowCells * maxColumnCells; i++)
        {
            #region Initializing Data

            float3 currentCellPosition = cellPositionArray[i];
            Entity currentCell = boardArray[i];

            float cellXCoordinate = cellPositionArray[i].x;
            float cellYCoordinate = cellPositionArray[i].y;

            float3[] neighborCellPositionArray = new float3[4];

            #endregion Initializing Data

            neighborCellPositionArray = FillUpValidNeighborCells(i, cellXCoordinate, cellYCoordinate, neighborCellPositionArray);

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
    /// Set the created pieces on the cells they occupy as reference
    /// </summary>
    /// <param name="i"></param>
    private void SetPiecesOnCellsAsReference(int i, float3 piecePosition)
    {
        for (int j = 0; j < cellPositionArray.Length; j++)
        {
            if (cellPositionArray[j].x == piecePosition.x && cellPositionArray[j].y == piecePosition.y)
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

    /// <summary>
    /// Add the last cell component tag if cells are at the edge rows.Important for checking if Flag has passed the other side.
    /// </summary>
    /// <param name="columns"></param>
    private void TagEdgeCells(int columns)
    {
        if (columns == maxColumnCells - maxColumnCells)
        {
            entityManager.AddComponent(boardArray[boardIndex], typeof(LastCellsForBlackTag));
        }
        else if (columns == maxColumnCells - 1)
        {
            entityManager.AddComponent(boardArray[boardIndex], typeof(LastCellsForWhiteTag));
        }
    }
}