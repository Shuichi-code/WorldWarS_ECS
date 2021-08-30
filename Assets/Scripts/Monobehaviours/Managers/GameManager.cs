using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Tags;
using System;
using System.Linq;
using Unity.Collections;
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
        public Material highlightedPiece;

        public Mesh quadMesh;

        private static GameManager _instance;

        public readonly float startingXCoordinate = -4f;
        public readonly float startingYCoordinate = -4f;

        private EntityManager entityManager;
        private Entity gmEntity;

        private BoardManager boardManager;
        private PieceManager pieceManager;

        public const float PlayerClockDuration = float.MaxValue;

        public delegate void ActivateSystem(bool enabled);

        public event ActivateSystem SetArrangementSystemStatus;
        public event ActivateSystem SetActivateAbilitySystemStatus;
        public event ActivateSystem SetSystemStatus;

        public Player player { get; set; }

        public static GameManager GetInstance()
        {
            return _instance;
        }

        void Awake()
        {
            _instance = this;
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            player = new Player();
        }

        private void CreatePlayer<T>(Team team, Army army)
        {
            var entityQuery = entityManager.CreateEntityQuery(typeof(T));

            if (entityQuery.CalculateEntityCount() == 0)
            {
                var playerEntityArchetype = entityManager.CreateArchetype(
                    typeof(T)
                    , typeof(TeamComponent)
                    , typeof(TimeComponent)
                    , typeof(ArmyComponent)
                );
                var pEntity = entityManager.CreateEntity(playerEntityArchetype);
            }
            var playerEntity = entityQuery.GetSingletonEntity();
            entityManager.SetComponentData(playerEntity, new TeamComponent() { myTeam = team });
            entityManager.SetComponentData(playerEntity, new TimeComponent() { TimeRemaining = PlayerClockDuration });
            entityManager.SetComponentData(playerEntity, new ArmyComponent() { army = army });
        }

        private void Start()
        {
            CreateGameManagerEntity();
            SetGameState(GameState.WaitingToStart);
            SetSystemsEnabled(false);
            SetArrangementStatus(false);
            boardManager = BoardManager.GetInstance();
            pieceManager = PieceManager.GetInstance();
        }
        public void CreateGameWorld(FixedString32 chosenOpening)
        {
            var enemyTeam = SwapTeam(player.Team);
            var enemyArmy = Army.Russia;//RandomizeArmy();
            var enemyOpening = RandomizeOpening();

            CreatePlayer<PlayerTag>(player.Team, player.Army);
            CreatePlayer<EnemyTag>(enemyTeam, enemyArmy);

            boardManager.CreateBoard();

            pieceManager.CreatePlayerPieces(enemyOpening, enemyTeam, enemyArmy, false);
            pieceManager.CreatePlayerPieces(chosenOpening, player.Team, player.Army);
            SetArrangementStatus(true);
        }

        private static Army RandomizeArmy()
        {
            var armyList = Enum.GetValues(typeof(Army)).Cast<Army>().ToList();
            var random = new Random();
            var i = random.Next(armyList.Count);
            return armyList[i];
        }

        private void SetArrangementStatus(bool enabled)
        {
            SetArrangementSystemStatus?.Invoke(enabled);
            SetSystemsEnabled(!enabled);
            SetActivateAbilitySystemStatus?.Invoke(!enabled);
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
            entityManager.SetComponentData(gmEntity, new GameManagerComponent
            {
                gameState = gameState,
                teamToMove = team
            });
        }

        public void DestroyBoardAndPieces()
        {
            entityManager.DestroyEntity(entityManager.CreateEntityQuery(typeof(CellTag)));
            entityManager.DestroyEntity(entityManager.CreateEntityQuery(typeof(PieceTag)));
        }

        public static Team SwapTeam(Team team)
        {
            return team == Team.Invader ? Team.Defender : Team.Invader;
        }

        public void SetSystemsEnabled(bool enabled)
        {
            SetSystemStatus?.Invoke(enabled);
        }

        public void StartGame()
        {
            SetGameState(GameState.Playing);
            SetSystemsEnabled(true);
            SetArrangementStatus(false);
        }
    }
}