using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Systems;
using Assets.Scripts.Tags;
using Unity.Entities;
using UnityEngine;
using Random = System.Random;

namespace Assets.Scripts.Monobehaviours.Managers
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
        private EntityManager _entityManager;


        public Player Player { get; set; }
        public Player EnemyAI { get; set; }


        public static GameManager GetInstance()
        {
            return _instance;
        }

        void Awake()
        {
            Player = new Player {TimeRemaining = 120};
            _instance = this;
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void Start()
        {

            CreateGameManagerEntity();
            SetGameState(GameState.WaitingToStart);
            SetSystemsEnabled(false);
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ArrangeArmySystem>().Enabled = false;
        }

        public void CreateGameWorld()
        {
            BoardManager.GetInstance().CreateBoard();
            InitializeEnemyAi();
            PieceManager.GetInstance().CreatePlayerPieces(EnemyAI);
            PieceManager.GetInstance().CreatePlayerPieces(Player);
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ArrangeArmySystem>().Enabled = true;
        }

        private void InitializeEnemyAi()
        {
            EnemyAI = new Player
            {
                ChosenOpening = RandomizeOpening(),
                Team = SwapTeam(Player.Team),
                TimeRemaining = 120
            };
        }

        private static string RandomizeOpening()
        {
            var openingList = new Dictionaries().openingList;
            var random = new Random();
            var i = random.Next(openingList.Count);
            return openingList[i];
        }

        private void CreateGameManagerEntity()
        {
            EntityArchetype entityGmArchetype = _entityManager.CreateArchetype(
                typeof(GameManagerComponent)
            );
            Entity gm = _entityManager.CreateEntity(entityGmArchetype);
        }

        public void SetGameState(GameState gameState, Team team = Team.Invader)
        {
            Entity gmEntity = _entityManager.CreateEntityQuery(typeof(GameManagerComponent)).GetSingletonEntity();
            _entityManager.SetComponentData<GameManagerComponent>(gmEntity, new GameManagerComponent
            {
                gameState = gameState,
                teamToMove = team
            });
        }

        public void DestroyBoardAndPieces()
        {
            _entityManager.DestroyEntity(_entityManager.CreateEntityQuery(typeof(CellTag)));
            _entityManager.DestroyEntity(_entityManager.CreateEntityQuery((typeof(PieceComponent))));
        }

        public static Team SwapTeam(Team team)
        {
            return team == Team.Invader ? Team.Defender : Team.Invader;
        }

        public static void SetSystemsEnabled(bool enabled)
        {
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ArbiterCheckingSystem>().Enabled = enabled;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<CapturedSystem>().Enabled = enabled;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<DragToMouseSystem>().Enabled = enabled;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<HighlightCellSystem>().Enabled = enabled;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<PickUpSystem>().Enabled = enabled;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<RemoveTagsSystem>().Enabled = enabled;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<TurnSystem>().Enabled = enabled;

        }

        public void StartGame()
        {
            SetGameState(GameState.Playing);
            SetSystemsEnabled(true);
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ArrangeArmySystem>().Enabled = false;
        }
    }
}