using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine;

[DisableAutoCreation]
public class GenerateBoardSystem : ComponentSystem
{
    EntityManager entityManager;
    private EntityArchetype entityArchetype;
    private NativeArray<Entity> boardArray;

    private Mesh mesh;
    private Material cellSprite;
    private Shader shader;

    const int maxRow = 9;
    const int maxCol = 8;

    private int boardIndex = 0;
    protected override void OnCreate()
    {
        base.OnStartRunning();

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        entityArchetype = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(RenderMesh),
            typeof(RenderBounds),
            typeof(CellComponent)
        );
        boardArray = new NativeArray<Entity>(maxRow * maxCol, Allocator.Temp);
        entityManager.CreateEntity(entityArchetype, boardArray);

        mesh = new Mesh();
        mesh.name = "Quad";

        cellSprite = Resources.Load("WalkingSpriteSheet", typeof(Material)) as Material;
        createBoard();
    }

    /// <summary>
    /// Sets the cells in the correct position and adds the mesh and material data to each cell
    /// </summary>
    private void createBoard()
    {
        boardIndex = 0;

        for (int rows = 0; rows < maxRow; rows++)
        {
            for (int columns = 0; columns < maxCol; columns++)
            {
                float3 spawnPosition = new float3(-4f + rows, -3.5f + columns, 50);

                entityManager.SetComponentData(boardArray[boardIndex],
                    new Translation
                    {
                        Value = spawnPosition
                    }
                );
                entityManager.SetSharedComponentData<RenderMesh>(boardArray[boardIndex], new RenderMesh
                {
                    mesh = mesh,
                    material = cellSprite,
                    layer = 50
                });
                boardIndex++;
            }
        }
    }

    protected override void OnUpdate()
    {
        //throw new System.NotImplementedException();
    }
}
