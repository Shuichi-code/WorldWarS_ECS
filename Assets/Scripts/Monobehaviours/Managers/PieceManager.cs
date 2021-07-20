using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Scripts.Monobehaviours.Managers
{
    public class PieceManager : MonoBehaviour
    {
        private NativeArray<Entity> pieceArray;
        private EntityArchetype entityArchetype;
        private EntityManager entityManager;

        public const float PieceZ = 0f;

        private static PieceManager _instance;
        private float startingXCoordinate;
        private float startingYCoordinate;
        private GameManager _gameManager;
        private const float InvaderPieceStartingXCoordinate = -4f;
        private const float InvaderPieceStartingYCoordinate = -2f;
        private const float DefenderPieceStartingXCoordinate = 4f;
        private const float DefenderPieceStartingYCoordinate = 1f;


        public static PieceManager GetInstance()
        {
            return _instance;
        }
        public void Awake()
        {
            _instance = this;
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        void Start()
        {
            _gameManager = GameManager.GetInstance();
        }

        public void CreatePlayerPieces(FixedString32 chosenOpening, Team team, Army army)
        {
            CreatePlayerPieceEntities();

            //get arrangement based on chosenOpening
            int[,] chosenOpenArray = new Dictionaries().mOpenings[chosenOpening.ToString()];

            //set the player pieces based on the arrangement
            int xIndex = 0;
            int yIndex = 0;
            var pieceEntityIndex = 0;
            while (xIndex < 9)
            {
                var pieceRank = chosenOpenArray[yIndex, xIndex];
                if (pieceRank != Piece.Null)
                {
                    var pieceLocation = GetPieceCoordinate(xIndex, yIndex, team);
                    var pieceEntity = pieceArray[pieceEntityIndex];


                    SetPieceEntityLocation(pieceEntity, pieceLocation);

                    entityManager.SetComponentData(pieceEntity,
                        new PieceComponent
                        {
                            originalCellPosition = pieceLocation,
                            pieceRank = pieceRank,
                            team = team
                        }
                    );

                    entityManager.SetComponentData(pieceEntity, new ArmyComponent() {army = army});

                    BoardManager.GetInstance().SetPiecesOnCellsAsReference(pieceEntityIndex, pieceLocation, pieceArray);
                    pieceEntityIndex++;
                }

                xIndex++;
                if (xIndex == 9 && yIndex < 2)
                {
                    yIndex++;
                    xIndex = 0;
                }
            }
        }

        private void SetPieceEntityLocation(Entity pieceEntity,float3 pieceCoordinate)
        {
            entityManager.SetComponentData(pieceEntity, new Translation { Value = pieceCoordinate });

        }

        private void CreatePlayerPieceEntities()
        {
            entityArchetype = entityManager.CreateArchetype(
                typeof(Translation),
                typeof(PieceComponent),
                typeof(ArmyComponent)
            );

            pieceArray = new NativeArray<Entity>(21, Allocator.Temp);
            entityManager.CreateEntity(entityArchetype, pieceArray);
        }

        public static float3 GetPieceCoordinate(int xIndex, int yIndex, Team team)
        {
            float2 pieceCoordinate = team == Team.Invader
                ? new float2(InvaderPieceStartingXCoordinate + xIndex, InvaderPieceStartingYCoordinate - yIndex)
                : new float2(DefenderPieceStartingXCoordinate -xIndex, DefenderPieceStartingYCoordinate + yIndex);

            return new float3(pieceCoordinate, PieceZ);
        }
    }
}