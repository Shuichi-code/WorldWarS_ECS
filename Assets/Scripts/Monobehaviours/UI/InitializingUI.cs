using Assets.Scripts.Class;
using System;
using System.Collections.Generic;
using Assets.Scripts.Monobehaviours.Managers;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using GameConstants = Assets.Scripts.Monobehaviours.Managers.GameConstants;

namespace Assets.Scripts.Monobehaviours
{
    public class InitializingUI : MonoBehaviour
    {
        public GameObject initializingUI;
        private string m_TitleScene = "TitleScene";
        private string openingListVEName = "openingVisualElement";
        private string openingListName = "openingListView";

        private ListView openingListView;
        private GameManager _gameManager;
        private GameObject placingPiecesUI;
        private GameObject gameOverlayUI;

        void Start()
        {
            placingPiecesUI = GameObject.Find(GameConstants.PlacingpiecesuiName);
            gameOverlayUI = GameObject.Find(GameConstants.GameoverlayUIName);
            _gameManager = GameManager.GetInstance();
        }
        void OnEnable()
        {
            var root = GetComponent<UIDocument>();
            var rootVisualElement = root.rootVisualElement;
            rootVisualElement.Q(GameConstants.AcceptBtnName)?.RegisterCallback<ClickEvent>(ev => InitializeGame());
            rootVisualElement.Q(GameConstants.ReturnBtnName)?.RegisterCallback<ClickEvent>(ev => ReturnToTitle());
            var openingListVisualElement = rootVisualElement.Q(openingListVEName);
            openingListView = VisualElementsUtility.InitializeList(new Dictionaries().openingList, openingListName);
            openingListView.selectedIndex = 0;
            openingListVisualElement.Add(openingListView);
        }

        private void ReturnToTitle()
        {
            SceneManager.LoadSceneAsync(m_TitleScene);
        }

        private void InitializeGame()
        {
            string chosenOpening = openingListView.selectedItem.ToString();
            //hide the initializing UI
            initializingUI.SetActive(false);

            _gameManager.SetGameState(GameState.PlacingPieces);
            var chosenOpening32 = (FixedString32) chosenOpening;
            _gameManager.CreateGameWorld(chosenOpening32);

            placingPiecesUI.SetActive(true);
        }
    }
}