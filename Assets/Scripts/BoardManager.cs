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

    int[] mPieceOrder = new int[21]
    {
        1, 2, 3, 4, 5, 6, 7,
        8, 9, 10, 11, 12, 13,13,
        13,13,13,13, 0, 0, 14
    };
    float3[,] cellposition = new float3[maxRow,maxCol];

    private System.Collections.Generic.Dictionary<string, int> mPieceRank = new System.Collections.Generic.Dictionary<string, int>()
    {
        {"Spy", 0 },
        {"G5S", 1 },
        {"G4S", 2 },
        {"LtG", 3 },
        {"MjG", 4 },
        {"BrG", 5 },
        {"Col", 6 },
        {"LtCol", 7 },
        {"Maj", 8 },
        {"Cpt", 9 },
        {"1Lt", 10 },
        {"2Lt", 11 },
        {"Sgt", 12 },
        {"Pvt", 13 },
        {"Flg", 14 },
    };
    float3[] cellPositionArray = new float3[maxRow*maxCol];

    private void Awake()
    {
        instance = this;

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        entityGameManagerArchetype = entityManager.CreateArchetype(
                typeof(GameManagerComponent)
        );
        Entity gameManager = entityManager.CreateEntity(entityGameManagerArchetype);
        entityManager.SetComponentData( gameManager,
            new GameManagerComponent
            {
                state = GameManagerComponent.State.Playing,
                isDragging = false
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
                float3 spawnPosition = new float3(-4f+rows, -4f+columns, 50);
                cellPositionArray[boardIndex] = spawnPosition;
                cellposition[rows, columns] = spawnPosition;
                //Debug.Log("Spawn position at CellPosition["+rows+","+columns+"] is "+spawnPosition);
                entityManager.SetComponentData(boardArray[boardIndex],
                    new Translation
                    {
                        Value = spawnPosition
                    }
                );
                entityManager.AddBuffer<CellNeighborBuffer>(boardArray[boardIndex]);

                //this code is for Graphics.DrawMeshInstanced
                /*matrix.SetTRS(spawnPosition, Quaternion.identity, Vector3.one);
                entityManager.SetComponentData(boardArray[boardIndex],
                    new CellComponent
                    {
                        matrix = matrix
                    }
                );*/
                boardIndex++;
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
            typeof(PieceTag)
        );

        pieceArray = new NativeArray<Entity>(21, Allocator.Temp);
        entityManager.CreateEntity(entityArchetype, pieceArray);

        for (int i = 0; i < mPieceOrder.Length; i++)
        {
            //This is code for the placement of the pieces on the board
            if (color == Color.white)
            {
                //TODO: write code for placing white pieces
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
                //TODO: write code for placing black pieces
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
            entityManager.SetComponentData(pieceArray[i],
                 new Translation
                 {
                     Value = piecePosition
                 }
            );
            entityManager.SetComponentData(pieceArray[i],
                 new PieceComponent
                 {
                     originalCellPosition = piecePosition,
                     pieceRank = mPieceOrder[i],
                     teamColor = color
                 }
             );
            for (int j = 0; j < cellPositionArray.Length; j++)
            {
                if(cellPositionArray[j].x == piecePosition.x && cellPositionArray[j].y == piecePosition.y)
                {
                    /*entityManager.SetComponentData(boardArray[j],
                        new CellComponent
                        {
                            hasPiece = true,
                            pieceColor = color
                        }
                    );*/
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
