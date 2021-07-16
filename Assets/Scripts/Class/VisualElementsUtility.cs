using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Assets.Scripts.Class
{
    public class VisualElementsUtility
    {
        public static ListView InitializeList(List<string> items, string listName)
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
    }
}