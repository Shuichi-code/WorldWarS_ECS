using Assets.Scripts.Class;
using Assets.Scripts.Monobehaviours.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.Monobehaviours.UI
{
    public class PlacingPiecesUI : MonoBehaviour
    {
        private string playButtonName = "playBtn";
        private string returnButtonName = "backBtn";
        private GameObject placePieceUi;
        private GameObject initializingUI;
        private GameManager gameManager;
        private GameObject gameOverlayUI;

        void Awake()
        {
            placePieceUi = this.gameObject;
            initializingUI = GameObject.Find(GameConstants.InitializingUIName);
            gameOverlayUI = GameObject.Find(GameConstants.GameoverlayUIName);
        }
        void OnEnable()
        {
            var root = GetComponent<UIDocument>();
            var rootVisualElement = root.rootVisualElement;
            rootVisualElement.Q(playButtonName)?.RegisterCallback<ClickEvent>(ev => StartGame());
            rootVisualElement.Q(returnButtonName)?.RegisterCallback<ClickEvent>(ev => ReturnToInitializing());
        }

        void Start()
        {
            gameManager = GameManager.GetInstance();
        }

        public void ReturnToInitializing()
        {
            gameManager.SetGameState(GameState.WaitingToStart);
            placePieceUi.SetActive(false);
            initializingUI.SetActive(true);
            gameManager.DestroyBoardAndPieces();
        }

        private void StartGame()
        {

            placePieceUi.SetActive(false);
            gameOverlayUI.SetActive(true);
            gameManager.StartGame();
        }
    }
}
