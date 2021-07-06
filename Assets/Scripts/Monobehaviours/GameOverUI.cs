using Assets.Scripts.Class;
using Assets.Scripts.Components;
using Assets.Scripts.Systems;
using Assets.Scripts.Tags;
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

        private void Start()
        {
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ArbiterCheckingSystem>().OnGameWin += GameOverUI_OnGameWin;
            gameOverUI.SetActive(false);
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void GameOverUI_OnGameWin(Team winningTeam)
        {

            gameOverUI.SetActive(true);

            GameManager.GetInstance().SetGameState(GameState.Dead);
            SetSystemsEnabled(false);

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
            GameManager.GetInstance().DestroyBoardAndPieces();
            GameManager.GetInstance().CreateGameWorld();
            GameManager.GetInstance().SetGameState(GameState.Playing);
            SetSystemsEnabled(true);
            gameOverUI.SetActive(false);
        }


        private static void SetSystemsEnabled(bool enabled)
        {
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ArbiterCheckingSystem>().Enabled = enabled;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<CapturedSystem>().Enabled = enabled;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<DragToMouseSystem>().Enabled = enabled;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<HighlightCellSystem>().Enabled = enabled;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<PickUpSystem>().Enabled = enabled;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<RemoveTagsSystem>().Enabled = enabled;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<TurnSystem>().Enabled = enabled;
        }
    }
}