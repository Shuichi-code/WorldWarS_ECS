using Assets.Scripts.Class;
using Assets.Scripts.Monobehaviours.Managers;
using Assets.Scripts.Systems;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Assets.Scripts.Monobehaviours
{
    [DisableAutoCreation]
    public class GameOverUI : MonoBehaviour
    {
        private Button tryAgainButton;
        private Button exitButton;
        private Label winnerLabel;

        [SerializeField] private GameObject gameOverUI;

        private EntityManager entityManager;
        private GameObject placePieceUi;
        private GameObject gameOverlayUi;
        private GameObject ArmySelectUI;
        private GameManager gameManager;
        private GameObject initializingUi;

        void Awake()
        {
            initializingUi = GameObject.Find(GameConstants.InitializingUIName);
            gameOverlayUi = GameObject.Find(GameConstants.GameoverlayUIName);
            ArmySelectUI = GameObject.Find(GameConstants.ArmySelectUIName);
        }

        private void Start()
        {
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ArbiterCheckingSystem>().OnGameWin += GameOverUI_OnGameWin;
            gameOverUI.SetActive(false);
            gameManager = GameManager.GetInstance();
        }

        private void GameOverUI_OnGameWin(Team winningTeam)
        {

            gameOverUI.SetActive(true);

            gameManager.SetGameState(GameState.Dead);
            gameManager.SetSystemsEnabled(false);

            //activate the canvas and print the winner
            winnerLabel.text = winnerLabel != null ? (winningTeam == Team.Invader ? "Invader" : "Defender") : "WinnerLabel is null.";
        }



        private void OnEnable()
        {
            var root = GetComponent<UIDocument>();
            var rootVisualElement = root.rootVisualElement;

            winnerLabel = rootVisualElement.Q<Label>("winnerLbl");
            tryAgainButton = rootVisualElement.Q<Button>("tryAgainBtn");
            exitButton = rootVisualElement.Q<Button>("exitBtn");

            if (tryAgainButton != null)
                tryAgainButton.RegisterCallback<ClickEvent>(ev => Reset());
            else
                Debug.Log("tryAgainButton is null!");
            if (exitButton != null)
                exitButton.RegisterCallback<ClickEvent>(ev => ExitGame());
            else
                Debug.Log("exitButton is null!");
        }

        private static void ExitGame()
        {
            Debug.Log("Exiting Game!");
            Application.Quit();
        }

        private void Reset()
        {
            //reset the pieces
            gameManager.DestroyBoardAndPieces();
            gameManager.SetSystemsEnabled(false);
            ArmySelectUI.SetActive(true);
            gameOverlayUi.SetActive(false);
            gameOverUI.SetActive(false);
        }
    }
}