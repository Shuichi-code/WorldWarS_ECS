using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Systems;
using Assets.Scripts.Tags;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;
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
        private EntityManager entityManager;
        private Label playerTimerUILabel;
        private Label enemyTimerUILabel;
        private Entity gmEntity;
        private GameObject gameOverUI;
        private BoardManager boardManager;
        private PieceManager pieceManager;

        public const float PlayerClockDuration = 5f;

        public static GameManager GetInstance()
        {
            return _instance;
        }

        void Awake()
        {
            _instance = this;
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void CreatePlayer<T>(Team team)
        {
            var entityQuery = entityManager.CreateEntityQuery(typeof(T));

            if (entityQuery.CalculateEntityCount() == 0)
            {
                var playerEntityArchetype = entityManager.CreateArchetype(
                    typeof(T)
                    ,typeof(TeamComponent)
                    ,typeof(TimeComponent)
                );
                var pEntity = entityManager.CreateEntity(playerEntityArchetype);
            }
            var playerEntity = entityQuery.GetSingletonEntity();
            entityManager.SetComponentData<TeamComponent>(playerEntity, new TeamComponent() { myTeam = team });
            entityManager.SetComponentData<TimeComponent>(playerEntity, new TimeComponent() { TimeRemaining = PlayerClockDuration });
        }

        private void Start()
        {
            gameOverUI = GameObject.Find(GameConstants.GameoverlayUIName);
            CreateGameManagerEntity();
            SetGameState(GameState.WaitingToStart);
            SetSystemsEnabled(false);
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ArrangeArmySystem>().Enabled = false;
            boardManager = BoardManager.GetInstance();
            pieceManager = PieceManager.GetInstance();
        }
        public void CreateGameWorld(FixedString32 chosenOpening, Team team)
        {
            var enemyTeam = SwapTeam(team);
            CreatePlayer<PlayerTag>(team);
            CreatePlayer<EnemyTag>(enemyTeam);

            boardManager.CreateBoard();

            pieceManager.CreatePlayerPieces(RandomizeOpening(), enemyTeam);
            pieceManager.CreatePlayerPieces(chosenOpening, team);
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ArrangeArmySystem>().Enabled = true;
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
            var entityGmArchetype = entityManager.CreateArchetype(
                typeof(GameManagerComponent)
            );
            gmEntity = entityManager.CreateEntity(entityGmArchetype);

        }

        public void SetGameState(GameState gameState, Team team = Team.Invader)
        {
            entityManager.SetComponentData<GameManagerComponent>(gmEntity, new GameManagerComponent
            {
                gameState = gameState,
                teamToMove = team
            });
        }

        public void DestroyBoardAndPieces()
        {
            entityManager.DestroyEntity(entityManager.CreateEntityQuery(typeof(CellTag), typeof(PieceComponent)));
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
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<CountdownSystem>().Enabled = enabled;
        }

        public void StartGame()
        {
            SetGameState(GameState.Playing);
            SetSystemsEnabled(true);
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ArrangeArmySystem>().Enabled = false;
        }
    }
}