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
        private GameManager gameManager;
        private const float PlayerPieceStartingXCoordinate = -4f;
        private const float PlayerPieceStartingYCoordinate = -2f;
        private const float EnemyPieceStartingXCoordinate = 4f;
        private const float EnemyPieceStartingYCoordinate = 1f;


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
            gameManager = GameManager.GetInstance();
        }

        public void CreatePlayerPieces(FixedString32 chosenOpening, Team team, Army army, bool isPlayer = true)
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
                    var pieceLocation = GetPieceCoordinate(xIndex, yIndex, isPlayer);
                    var pieceEntity = pieceArray[pieceEntityIndex];


                    SetPieceEntityLocation(pieceEntity, pieceLocation);

                    entityManager.SetComponentData(pieceEntity, new TeamComponent(){ myTeam = team});
                    entityManager.SetComponentData(pieceEntity, new RankComponent() { Rank = pieceRank });
                    entityManager.SetComponentData(pieceEntity, new OriginalLocationComponent() { originalLocation = pieceLocation });
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
                typeof(PieceTag),
                typeof(ArmyComponent),
                typeof(TeamComponent),
                typeof(RankComponent),
                typeof(OriginalLocationComponent)
            );

            pieceArray = new NativeArray<Entity>(21, Allocator.Temp);
            entityManager.CreateEntity(entityArchetype, pieceArray);
        }

        public static float3 GetPieceCoordinate(int xIndex, int yIndex, bool isPlayer)
        {
            var pieceCoordinate = isPlayer
                ? new float2(PlayerPieceStartingXCoordinate + xIndex, PlayerPieceStartingYCoordinate - yIndex)
                : new float2(EnemyPieceStartingXCoordinate -xIndex, EnemyPieceStartingYCoordinate + yIndex);

            return new float3(pieceCoordinate, PieceZ);
        }
    }
}