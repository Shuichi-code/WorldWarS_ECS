using System;
using Assets.Scripts.Monobehaviours.Managers;
using Assets.Scripts.Systems;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.Monobehaviours.UI
{
    public class ChargedAbilityUI : MonoBehaviour
    {
        private GameManager gameManager;
        private GameObject chargedAbilityUI;

        void Start()
        {
            gameManager = GameManager.GetInstance();
            chargedAbilityUI = GameObject.Find(GameConstants.ChargedAbilityUIName);
            chargedAbilityUI.SetActive(false);
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ChargeAbilitySystem>().ChargedAbilityActiveEvent += ActivatingChargeButton;
        }

        private void ActivatingChargeButton(bool enabled)
        {
            chargedAbilityUI.SetActive(enabled);
        }

        // Update is called once per frame
        void Update()
        {
            var root = GetComponent<UIDocument>();
            var rootVisualElement = root.rootVisualElement;
            rootVisualElement.Q(GameConstants.ChargedAbilityBtnName)?.RegisterCallback<ClickEvent>(ev =>ActivateChargedAbility());
        }

        private void ActivateChargedAbility()
        {
            Debug.Log("Activating ability!");
            gameManager.ActivateAbility();
        }
    }
}
