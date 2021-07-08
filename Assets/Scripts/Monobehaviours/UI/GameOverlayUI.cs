using Assets.Scripts.Class;
using Assets.Scripts.Monobehaviours.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.Monobehaviours.UI
{
    public class GameOverlayUI : MonoBehaviour
    {
        private Player player;
        // Use this for initialization
        void Start()
        {
            player = GameManager.GetInstance().Player;
        }

        void OnEnable()
        {
            var root = GetComponent<UIDocument>();
            var rootVisualElement = root.rootVisualElement;
            var playerTimer = rootVisualElement.Q<Label>("PlayerTimer");
            playerTimer.text = "hello";

        }
    }
}