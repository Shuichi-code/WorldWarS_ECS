using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Monobehaviours
{
    public class GameManager : MonoBehaviour
    {
        public Material cellImage;
        public Material enemyCellImage;
        public Material highlightedImage;

        public Mesh quadMesh;

        private static GameManager _instance;

        public readonly float startingXCoordinate = -4f;
        public readonly float startingYCoordinate = -4f;

        public static string openingMode;

        private EntityArchetype entityArchetype;
        private EntityManager entityManager;


        public Player Player { get; set; }

        public static GameManager GetInstance()
        {
            return _instance;
        }

        private void Start()
        {
            Player = new Player();
            _instance = this;
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            CreateGameManagerEntity();
            CreateGameWorld();
            SetGameState(GameState.WaitingToStart);
        }

        public void CreateGameWorld()
        {
            BoardManager.GetInstance().CreateBoard();
            //PieceManager.GetInstance().CreatePieces(Team.Invader);
            PieceManager.GetInstance().CreatePieces(Team.Defender);
        }

        private void CreateGameManagerEntity()
        {
            EntityArchetype entityGmArchetype = entityManager.CreateArchetype(
                typeof(GameManagerComponent)
            );
            Entity gm = entityManager.CreateEntity(entityGmArchetype);
        }

        public void SetGameState(GameState gameState, Team team = Team.Invader)
        {
            Entity gmEntity = entityManager.CreateEntityQuery(typeof(GameManagerComponent)).GetSingletonEntity();
            entityManager.SetComponentData<GameManagerComponent>(gmEntity, new GameManagerComponent
            {
                gameState = gameState,
                teamToMove = team
            });
        }

        public void DestroyBoardAndPieces()
        {
            entityManager.DestroyEntity(entityManager.CreateEntityQuery(typeof(CellTag)));
            entityManager.DestroyEntity(entityManager.CreateEntityQuery((typeof(PieceComponent))));
        }
    }
}