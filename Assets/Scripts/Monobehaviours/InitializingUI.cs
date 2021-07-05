using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Assets.Scripts.Monobehaviours
{
    public class InitializingUI : MonoBehaviour
    {
        public GameObject initializingUI;
        private string m_TitleScene = "TitleScene";
        private string openingListVE = "openingVisualElement";
        private string openingListName = "openingListView";
        private string acceptBtnName = "acceptBtn";
        private string returnBtnName = "returnBtn";

        private ListView openingListView;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>();
            var rootVisualElement = root.rootVisualElement;
            rootVisualElement.Q(acceptBtnName)?.RegisterCallback<ClickEvent>(ev => InitializeGame());
            rootVisualElement.Q(returnBtnName)?.RegisterCallback<ClickEvent>(ev => ReturnToTitle());
            var openingListVisualElement = rootVisualElement.Q(openingListVE);

            var openingList = new List<string>() { "Default", "Blitzkrieg-Left", "Blitzkrieg-Right", "Mothership", "Box" };

            openingListView = InitializeList(openingList, openingListName);
            openingListVisualElement.Add(openingListView);
        }

        private ListView InitializeList(List<string> items, string listName)
        {
            // The "makeItem" function will be called as needed
            // when the ListView needs more items to render
            Func<VisualElement> makeItem = () => new Label();

            // As the user scrolls through the list, the ListView object
            // will recycle elements created by the "makeItem"
            // and invoke the "bindItem" callback to associate
            // the element with the matching data item (specified as an index in the list)
            Action<VisualElement, int> bindItem = (e, i) => (e as Label).text = items[i];

            // Provide the list view with an explict height for every row
            // so it can calculate how many items to actually display
            const int itemHeight = 16;

            var listView = new ListView(items, itemHeight, makeItem, bindItem)
            {
                selectionType = SelectionType.Multiple,
                name = listName
            };

            listView.style.flexGrow = 1.0f;

            return listView;
        }

        private void ReturnToTitle()
        {
            SceneManager.LoadSceneAsync(m_TitleScene);
        }

        private void InitializeGame()
        {
            string chosenOpening = openingListView.selectedItem.ToString();
            Debug.Log("You have chosen the " + chosenOpening + " opening!");
            //hide the initializing UI
            initializingUI.SetActive(false);
            //arrange the player's piece according to the chosen opening.
            // have the enemy choose a random opening
        }
    }
}