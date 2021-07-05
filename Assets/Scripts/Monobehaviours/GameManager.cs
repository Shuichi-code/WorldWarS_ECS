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

        public System.Collections.Generic.Dictionary<int, string> mPieceRank = new System.Collections.Generic.Dictionary<int, string>()
        {
            { 0,"Spy"},
            { 1,"G5S" },
            { 2,"G4S" },
            { 3,"LtG"},
            { 4,"MjG"},
            { 5,"BrG"},
            { 6,"Col"},
            { 7,"LtCol"},
            { 8,"Maj" },
            { 9,"Cpt"},
            { 10,"1Lt"},
            { 11,"2Lt"},
            { 12,"Sgt"},
            { 13,"Pvt"},
            { 14,"Flg"},
        };

        public Mesh quadMesh;

        private static GameManager _instance;

        public readonly float startingXCoordinate = -4f;
        public readonly float startingYCoordinate = -4f;

        public static string openingMode;

        private EntityArchetype entityArchetype;
        private EntityManager entityManager;

        public static GameManager GetInstance()
        {
            return _instance;
        }

        private void Start()
        {
            _instance = this;
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            CreateGameManagerEntity();
            CreateGameWorld();
            SetGameState(GameState.Playing);
        }

        public void CreateGameWorld()
        {
            BoardManager.GetInstance().CreateBoard();
            PieceManager.GetInstance().CreatePieces(Team.Invader);
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