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

        public readonly OpeningArrangement openingArrangement = new OpeningArrangement();
        private static PieceManager _instance;
        private float startingXCoordinate;
        private float startingYCoordinate;
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

        public void CreatePieces(Team team)
        {
            entityArchetype = entityManager.CreateArchetype(
                typeof(Translation),
                typeof(PieceComponent)
            );

            pieceArray = new NativeArray<Entity>(21, Allocator.Temp);
            entityManager.CreateEntity(entityArchetype, pieceArray);

            if (team == Team.Invader)
            {
                DefaultPlacePieces(team);
            }
            else
            {
                DefaultPlacePieces(team);
            }
        }

        //TODO: Finish this.
        private void PlayerPlacePieces(Team team)
        {
            int xIndex = 0;
            int yIndex = 0;
            while (yIndex < 9)
            {
                int pieceRank = openingArrangement.defaultArrangementArray[xIndex, yIndex];
                if (pieceRank != 15) //15 is null value;
                {
                    startingXCoordinate = GameManager.GetInstance().startingXCoordinate;
                    startingYCoordinate = GameManager.GetInstance().startingYCoordinate;
                    //place cells on x-index
                    int xCoordinate = yIndex + (int) startingXCoordinate;
                    int yCoordinate = xIndex + (int) startingYCoordinate;
                    float3 piecePosition = new float3(xCoordinate, yCoordinate, PieceZ);
                    Entity pieceEntity = pieceArray[xIndex + yIndex];

                    entityManager.SetComponentData(pieceEntity, new Translation { Value = piecePosition });
                    entityManager.SetComponentData(pieceEntity,
                        new PieceComponent
                        {
                            originalCellPosition = piecePosition,
                            pieceRank = pieceRank,
                            team = team
                        }
                    );

                    BoardManager.GetInstance().SetPiecesOnCellsAsReference(xIndex + yIndex, piecePosition, pieceArray);
                }

                yIndex++;
                if (yIndex <= 9) continue;
                xIndex++;
                yIndex = 0;
            }
            //place the pieces according to openingFormation
            //set game state to placingpieces
        }

        public void DefaultPlacePieces(Team team)
        {
            int[] yCoordinateArray = SetColumnPlaces(team);
            int pieceIndex = 0;
            while (pieceIndex < 21)
            {

                float3 piecePosition = SetDefaultPieceCoordinates(yCoordinateArray, pieceIndex);

                //Place the pieces on the board
                entityManager.SetComponentData(pieceArray[pieceIndex], new Translation { Value = piecePosition });
                entityManager.SetComponentData(pieceArray[pieceIndex],
                    new PieceComponent
                    {
                        originalCellPosition = piecePosition,
                        pieceRank = openingArrangement.defaultPieceArrangementArray[pieceIndex],
                        team = team
                    }
                );

                BoardManager.GetInstance().SetPiecesOnCellsAsReference(pieceIndex, piecePosition, pieceArray);
                pieceIndex++;
            }
        }

        /// <summary>
        /// Sets the possible y-coordinates of the pieces based on the piece team color
        /// </summary>
        /// <param name="team"></param>
        /// <returns></returns>
        public static int[] SetColumnPlaces(Team team)
        {
            var columnsArray = team == Team.Invader ? new int[] { 2, 1, 0 } : new int[] { 5, 6, 7 };
            return columnsArray;
        }

        public static float3 SetDefaultPieceCoordinates(int[] yCoordinateArray, int pieceIndex)
        {
            int startingXCoordinate = -4;
            int startingYCoordinate = -4;
            int xCoordinate = (pieceIndex < 9) ? (pieceIndex + startingXCoordinate) : (pieceIndex < 18) ? (pieceIndex - 9 + startingXCoordinate) : (pieceIndex - 18 + startingXCoordinate);
            int yCoordinate = (pieceIndex < 9) ? (yCoordinateArray[0] + startingYCoordinate) : (pieceIndex < 18) ? (yCoordinateArray[1] + startingYCoordinate) : yCoordinateArray[2] + startingYCoordinate;
            return new float3(xCoordinate, yCoordinate, PieceZ);
        }

        public void CreatePlayerPieces(Player player)
        {
            CreatePlayerPieceEntities();

            //get arrangement based on chosenOpening
            int[,] chosenOpenArray = new Dictionaries().mOpenings[player.ChosenOpening];

            //set the player pieces based on the arrangement
            int xIndex = 0;
            int yIndex = 0;
            var pieceEntityIndex = 0;
            while (xIndex < 9)
            {
                var pieceRank = chosenOpenArray[yIndex, xIndex];
                if (pieceRank != Piece.Null)
                {
                    var pieceLocation = GetPieceCoordinate(xIndex, yIndex, player.Team);
                    var pieceEntity = pieceArray[pieceEntityIndex];


                    SetPieceEntityLocation(pieceEntity, pieceLocation);

                    entityManager.SetComponentData(pieceEntity,
                        new PieceComponent
                        {
                            originalCellPosition = pieceLocation,
                            pieceRank = pieceRank,
                            team = player.Team
                        }
                    );
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
                typeof(PieceComponent)
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