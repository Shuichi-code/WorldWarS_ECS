using System;
using System.Net.Mime;
using Assets.Scripts.Monobehaviours.Managers;
using Assets.Scripts.Systems;
using Assets.Scripts.Systems.Special_Ability_Systems;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Assets.Scripts.Monobehaviours.UI
{
    public class ChargedAbilityUI : MonoBehaviour
    {
        private GameManager gameManager;
        private GameObject chargedAbilityUi;
        private VisualElement chargeButton;

        private void Start()
        {
            gameManager = GameManager.GetInstance();
            chargedAbilityUi = GameObject.Find(GameConstants.ChargedAbilityUIName);
            chargedAbilityUi.SetActive(false);
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ChargeAbilitySystem>().ChargedAbilityActiveEvent += ActivatingChargeButton;
        }

        private void ActivatingChargeButton(bool enabled)
        {
            chargedAbilityUi.SetActive(enabled);
        }

        // Update is called once per frame
        private void Update()
        {
            chargeButton = GetVisualElement(GameConstants.ChargedAbilityBtnName);
            chargeButton?.RegisterCallback<ClickEvent>(ev =>ActivateChargedAbility());
        }

        private VisualElement GetVisualElement(string visualElementName)
        {
            return GetComponent<UIDocument>().rootVisualElement.Q(visualElementName);
        }

        private void ActivateChargedAbility()
        {

            if (!World.DefaultGameObjectInjectionWorld.GetExistingSystem<ActivateAbilitySystem>().Enabled)
            {
                SystemManager.SetSystemStatus<ActivateAbilitySystem>(true);
                SystemManager.SetPickupSystems(false);
                chargeButton.GetFirstOfType<Button>().text = "Deactivate Ability";
            }
            else
            {
                SystemManager.SetSystemStatus<ActivateAbilitySystem>(false);
                SystemManager.SetPickupSystems(true);
                chargeButton.GetFirstOfType<Button>().text = "Use Charged Ability";
            }
        }
    }
}
