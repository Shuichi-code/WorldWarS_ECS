using Assets.Scripts.Monobehaviours.Managers;
using Assets.Scripts.Systems.ArmySystems;
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class SystemManager : MonoBehaviour
    {
        public GameManager gameManager { get; set; }
        void OnEnable()
        {
            gameManager = GameManager.GetInstance();
            gameManager.SetArrangementSystemStatus += SetSystemStatus<ArrangeArmySystem>;
            gameManager.SetActivateAbilitySystemStatus += SetSystemStatus<ActivateAbilitySystem>;
            gameManager.SetSystemStatus += SetGameSystemStatus;
        }

        public static void SetSystemStatus<T>(bool enabled) where T : SystemBase
        {
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<T>().Enabled = enabled;
        }

        public static void SetSpecialAbilitySystems(bool enabled)
        {
            SetSystemStatus<ActivateAbilitySystem>(enabled);
            SetSystemStatus<SpecialAbilitySystem>(enabled);
        }
        private static void SetGameSystemStatus(bool enabled)
        {
            SetSystemStatus<ArbiterCheckingSystem>(enabled);
            SetSystemStatus<CapturedSystem>(enabled);
            SetSystemStatus<DragToMouseSystem>(enabled);
            SetSystemStatus<HighlightCellSystem>(enabled);
            SetSystemStatus<PickUpSystem>(enabled);
            SetSystemStatus<RemoveTagsSystem>(enabled);
            SetSystemStatus<TurnSystem>(enabled);
            SetSystemStatus<CountdownSystem>(enabled);
        }

        public static void SetPickupSystems(bool enabled)
        {
            SetSystemStatus<RemoveTagsSystem>(enabled);
            SetSystemStatus<PickUpSystem>(enabled);
        }
    }
}