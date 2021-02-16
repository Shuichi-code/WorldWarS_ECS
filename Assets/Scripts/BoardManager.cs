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
    public bool isSelecting { get; set; } = false;
    public Mesh quadMesh;
    public Material cellImage;
    public Material highlightedImage;
    public Material enemyCellImage;
    private int boardIndex = 0;
    private float3 piecePosition;

    [SerializeField]
    NativeArray<Entity> boardArray;
    NativeArray<Entity> pieceArray;

    EntityManager entityManager;
    EntityArchetype entityGameManagerArchetype;
    EntityArchetype entityArchetype;

    //Matrix4x4 matrix;

    const int maxRow = 9;
    const int maxCol = 8;

    //default arrangement of pieces in the board. numbers represent the piecerank
    int[] mPieceOrder = new int[21]
    {
        1, 2, 3, 4, 5, 6, 7,
        8, 9, 10, 11, 12, 13,13,
        13,13,13,13, 0, 0, 14
    };

    float3[,] cellposition = new float3[maxRow,maxCol];

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
    float3[] cellPositionArray = new float3[maxRow*maxCol];
    private float3[] neighborCellPositionArray = new float3[4];
    float3 spawnPosition;

    private void Awake()
    {
        instance = this;

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        entityGameManagerArchetype = entityManager.CreateArchetype(
                typeof(GameManagerComponent)
        );
        Entity gameManager = entityManager.CreateEntity(entityGameManagerArchetype);
        entityManager.SetComponentData(gameManager,
            new GameManagerComponent
            {
                state = GameManagerComponent.State.Playing,
                isDragging = false,
                teamToMove = Color.white
            });
        //this code is for Graphics.DrawMeshInstanced
        //matrix = new Matrix4x4();

        createBoard();
        createPieces(Color.white);
        createPieces(Color.black);
    }

    /// <summary>
    /// Creates the board by instantiating cell entities based on maxRow vs maxCol
    /// </summary>
    private void createBoard()
    {
        entityArchetype = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(CellComponent)
        );
        boardArray = new NativeArray<Entity>(maxRow * maxCol, Allocator.Temp);
        entityManager.CreateEntity(entityArchetype, boardArray);
        boardIndex = 0;
        for (int columns = 0; columns < maxCol; columns++)
        {
            for (int rows = 0; rows < maxRow; rows++)
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
                /*entityManager.SetSharedComponentData(boardArray[boardIndex],
                    new RenderMesh
                    {
                        mesh = quadMesh,
                        material = cellImage
                    }
                );*/

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
        for(int i = 0; i < maxRow*maxCol; i++ )
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
    private void createPieces(Color color)
    {
        entityArchetype = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(PieceComponent),
            typeof(PieceTag)//,
            //typeof(RenderMesh),
            //typeof(RenderBounds),
            //typeof(LocalToWorld)
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
                    //Debug.Log(i);;
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

            //code for setting the rendermesh
            /*entityManager.SetSharedComponentData(pieceArray[i], new RenderMesh {
                mesh = quadMesh,
                material = Resources.Load(mPieceRank[mPieceOrder[i]], typeof(Material)) as Material
            });*/

            //setting the pieces on the cell as reference
            for (int j = 0; j < cellPositionArray.Length; j++)
            {
                if(cellPositionArray[j].x == piecePosition.x && cellPositionArray[j].y == piecePosition.y)
                {
                    entityManager.AddComponent<PieceOnCellComponent>(boardArray[j]);
                    entityManager.SetComponentData(boardArray[j],
                        new PieceOnCellComponent
                        {
                            piece = pieceArray[i]
                        }
                    );
                }
            }
        }
    }
}
