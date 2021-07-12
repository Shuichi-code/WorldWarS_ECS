using Assets.Scripts.Class;
using Assets.Scripts.Monobehaviours.Managers;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.Monobehaviours.UI
{
    public class GameOverlayUI : MonoBehaviour
    {
        private Player player;

        private GameObject _instance;
        public Label PlayerTimerLabel { get; private set; }

        // Use this for initialization
        void Start()
        {
            player = GameManager.GetInstance().Player;
            _instance = GameObject.Find(GameConstants.GameoverlayUIName);
            _instance.SetActive(false);
        }

        void OnEnable()
        {
            var root = GetComponent<UIDocument>();
            var rootVisualElement = root.rootVisualElement;
            PlayerTimerLabel = rootVisualElement.Q<Label>(GameConstants.PlayertimerName);
            PlayerTimerLabel.text = "hello";
        }

        Label GetPlayerTimerLabel()
        {
            return PlayerTimerLabel;
        }
    }
}