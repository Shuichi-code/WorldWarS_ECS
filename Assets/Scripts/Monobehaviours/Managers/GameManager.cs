using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Systems;
using Assets.Scripts.Tags;
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

        const float PlayerClockDuration = 5f;


        public Player Player { get; set; }
        public Player EnemyAI { get; set; }


        public static GameManager GetInstance()
        {
            return _instance;
        }

        void Awake()
        {
            Player = new Player( );
            _instance = this;
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void Start()
        {
            gameOverUI = GameObject.Find(GameConstants.GameoverlayUIName);
            CreateGameManagerEntity();
            SetGameState(GameState.WaitingToStart);
            SetSystemsEnabled(false);
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ArrangeArmySystem>().Enabled = false;

        }

        void Update()
        {

            if (GetGameState() != GameState.Playing) return;
            CheckIfTimerExpire(Player, EnemyAI);

            if (GetTeamToMove() == Team.Invader)
            {
                Player.TimeRemaining -= Time.deltaTime;
                playerTimerUILabel.text = Player.GetTimeRemainingString();
            }
            else
            {
                EnemyAI.TimeRemaining -= Time.deltaTime;
                enemyTimerUILabel.text = EnemyAI.GetTimeRemainingString();
            }
        }

        private void CheckIfTimerExpire(Player player, Player enemy)
        {
            if (!(player.TimeRemaining <= 0) && !(enemy.TimeRemaining <= 0)) return;
            var eventEntity = entityManager.CreateEntity(typeof(GameFinishedEventComponent));
            entityManager.SetComponentData(eventEntity, new GameFinishedEventComponent
            {
                winningTeam = player.TimeRemaining <= 0 ? enemy.Team : player.Team
            });

        }

        public void CreateGameWorld()
        {
            BoardManager.GetInstance().CreateBoard();
            if(EnemyAI == null)
                InitializeEnemyAi();
            ResetPlayerClocks();
            PieceManager.GetInstance().CreatePlayerPieces(EnemyAI);
            PieceManager.GetInstance().CreatePlayerPieces(Player);
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ArrangeArmySystem>().Enabled = true;
        }

        private void ResetPlayerClocks()
        {
            Player.TimeRemaining = PlayerClockDuration;
            EnemyAI.TimeRemaining = PlayerClockDuration;
        }

        private void InitializeEnemyAi()
        {
            EnemyAI = new Player
            {
                ChosenOpening = RandomizeOpening(),
                Team = SwapTeam(Player.Team),
                TimeRemaining = PlayerClockDuration
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

        public GameState GetGameState()
        {
            return entityManager.GetComponentData<GameManagerComponent>(gmEntity).gameState;
        }

        public Team GetTeamToMove()
        {
            return entityManager.GetComponentData<GameManagerComponent>(gmEntity).teamToMove;
        }

        public void DestroyBoardAndPieces()
        {
            entityManager.DestroyEntity(entityManager.CreateEntityQuery(typeof(CellTag)));
            entityManager.DestroyEntity(entityManager.CreateEntityQuery((typeof(PieceComponent))));
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
            playerTimerUILabel = GetPlayerTimerLabel(gameOverUI, GameConstants.PlayertimerName);
            enemyTimerUILabel = GetPlayerTimerLabel(gameOverUI, GameConstants.EnemytimerName);
            SetSystemsEnabled(true);
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ArrangeArmySystem>().Enabled = false;
        }

        public Label GetPlayerTimerLabel(GameObject gameOverUI, string playerName)
        {
            return gameOverUI.GetComponent<UIDocument>().rootVisualElement.Q<Label>(playerName);
        }
    }
}