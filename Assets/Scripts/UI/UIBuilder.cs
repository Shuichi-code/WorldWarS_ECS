using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.UIElements.Runtime;
using Button = UnityEngine.UIElements.Button;

public class UIBuilder : MonoBehaviour
{
    private PanelRenderer panelRenderer;

    private void Start()
    {
        panelRenderer = this.GetComponent<PanelRenderer>();
        panelRenderer.postUxmlReload += OnloadUXML;
    }

    //call OnLoad of UXML in panelRenedere
    IEnumerable<Object> OnloadUXML()
    {

        var root = panelRenderer.visualTree;

        //Find & Subscribe Method onclick event On "Tap to Start" (Name in UXML is "Play" ) Button of Mainmenu Screen
        var tapToStart = root.Q<Button>("DeployButton"); // find Play button in Uxml(visualTree)
        if (tapToStart != null)
        {
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
        if (panelRenderer != null)
        {
            Debug.Log("PanelRenderer exists!");
        }
        else
        {
            Debug.Log("PanelRenderer does not exist!");
        }
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
