using Assets.Scripts.Class;
using Assets.Scripts.Monobehaviours.Managers;
using Assets.Scripts.Systems;
using Unity.Entities;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.Monobehaviours.UI
{
    public class GameOverlayUI : MonoBehaviour
    {
        private GameObject _instance;
        public Label PlayerTimerLabel { get; private set; }
        public Label EnemyTimerLabel { get; private set; }

        // Use this for initialization
        void Start()
        {
            _instance = GameObject.Find(GameConstants.GameoverlayUIName);
            _instance.SetActive(false);
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<CountdownSystem>().clockTick += UpdateClock;
        }

        public void UpdateClock( Team team, float time)
        {
            if (team == Team.Invader)
            {
                PlayerTimerLabel.text = GetTimeRemainingString(time);
            }
            else
            {
                EnemyTimerLabel.text = GetTimeRemainingString(time);
            }
        }

        void OnEnable()
        {
            var root = GetComponent<UIDocument>();
            var rootVisualElement = root.rootVisualElement;
            PlayerTimerLabel = rootVisualElement.Q<Label>(GameConstants.PlayertimerName);
            EnemyTimerLabel = rootVisualElement.Q<Label>(GameConstants.EnemytimerName);
            PlayerTimerLabel.text = GetTimeRemainingString(GameManager.PlayerClockDuration);
            EnemyTimerLabel.text = GetTimeRemainingString(GameManager.PlayerClockDuration);
        }

        public string GetTimeRemainingString(float TimeRemaining, bool forceHHMMSS = false)
        {
            float secondsRemainder = Mathf.Floor((TimeRemaining % 60) * 100) / 100.0f;
            int minutes = ((int)(TimeRemaining / 60)) % 60;
            int hours = (int)(TimeRemaining / 3600);

            if (forceHHMMSS) return System.String.Format("{0}:{1:00}:{2:00}", hours, minutes, secondsRemainder);

            return hours == 0 ? System.String.Format("{0:00}:{1:00.00}", minutes, secondsRemainder) : System.String.Format("{0}:{1:00}:{2:00}", hours, minutes, secondsRemainder);
        }
    }
}