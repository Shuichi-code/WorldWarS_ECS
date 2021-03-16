using System.Collections.Generic;
using Unity.UIElements.Runtime;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

public class UIBuilder : MonoBehaviour
{
    [SerializeField] private PanelRenderer panelRenderer;
    private VisualElement rootElement;

    //call OnLoad of UXML in panelRenedere
    private IEnumerable<Object> OnloadUXML()
    {
        Debug.Log("Entering OnLoadUXML");
        var root = panelRenderer.visualTree;
        if (root != null)
        {
            Debug.Log("Root exists!");
        }
        //Find & Subscribe Method onclick event On "Tap to Start" (Name in UXML is "Play" ) Button of Mainmenu Screen
        var tapToStart = root.Q<Button>("DeployButton"); // find Play button in Uxml(visualTree)
        if (tapToStart != null)
        {
            Debug.Log("DeployButton found!");
            tapToStart.clicked += OnPlay; // subscribe event
        }

        //Find & Subscribe Method onclick event On "Exit" (Name in UXML is "Exit" ) Button of Mainmenu Screen
        var exit = root.Q<Button>("ReturnButton");// find Exit button in Uxml(visualTree)
        if (exit != null)
        {
            exit.clicked += OnExit; // subscribe event
        }

        return null;
    }

    // Call OnTap of Play button of MainmenuScreen
    private void OnPlay()
    {
        Debug.Log("Clicked on DeployButton");
    }

    // Call OnTap of Exit button of MainmenuScreen
    private void OnExit()
    {
        Debug.Log("Clicked on ReturnButton");
    }

    private void OnEnable()
    {
        panelRenderer.postUxmlReload += OnloadUXML; //!!! THIS CODE NEEDS TO BE RUN ON OnEnable METHOD
        //var deployButton = panelRenderer.visualTree.Q<Button>("DeployButton");
        //deployButton.clickable.clickedWithEventInfo += (evt) => Debug.Log("Button has been pressed");
        //deployButton.clickable.clicked += () => Debug.Log("Clicked!");
        //deployButton.RegisterCallback<MouseUpEvent>(ev => ButtonPressed());
    }

    private void ButtonPressed()
    {
        Debug.Log("Deploy Button has been pressed!");
    }
}