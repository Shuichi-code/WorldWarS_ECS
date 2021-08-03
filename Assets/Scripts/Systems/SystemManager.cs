using Assets.Scripts.Monobehaviours.Managers;
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
            gameManager.SetSystemStatus += SetSystemStatus;
        }

        private void SetArrangementSystemStatus(bool enabled)
        {
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ArrangeArmySystem>().Enabled = enabled;
        }

        private void SetSystemStatus<T>(bool enabled) where T : SystemBase
        {
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<T>().Enabled = enabled;
        }

        private void SetSystemStatus(bool enabled)
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
    }
}