using System.Collections.Generic;
using Unity.UIElements.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

public class UIBuilder : MonoBehaviour
{
    public enum OpeningType
    {
        Default,
        BlitzkriegLeft,
        BlitzkriegRight,
        Mothership,
        Box,
        Random
    }
    [SerializeField] private PanelRenderer panelRenderer;
    //[SerializeField] private Canvas parentCanvas;

    public CanvasGroup cg;
    public static string openingMode;


    //call OnLoad of UXML in panelRenedere
    private IEnumerable<Object> OnloadUXML()
    {
        var root = panelRenderer.visualTree;
        Label openingLabel = root.Q<Label>("Opening_Label");

        //Find & Subscribe Method onclick event On "Tap to Start" (Name in UXML is "Play" ) Button of Mainmenu Screen
        var tapToStart = root.Q<Button>("AcceptBtn"); // find Play button in Uxml(visualTree)
        if (tapToStart != null)
        {
            tapToStart.clicked += OnPlay; // subscribe event
        }

        //Find & Subscribe Method onclick event On "Exit" (Name in UXML is "Exit" ) Button of Mainmenu Screen
        var exit = root.Q<Button>("ReturnBtn");// find Exit button in Uxml(visualTree)
        if (exit != null)
        {
            exit.clicked += OnExit; // subscribe event
        }

        root.Query<Button>(className: "opening-choices").ForEach(ChooseOpening(openingLabel));
        return null;
    }

    private static System.Action<Button> ChooseOpening(Label openingLabel)
    {
        return (btn) =>
        {
            btn.clicked += () =>
            {
                openingLabel.text = "Your Opening is: " + btn.text;
                BoardManager.openingMode = btn.text;
            };
        };
    }

    // Call OnTap of Play button of MainmenuScreen
    private void OnPlay()
    {
        //Load the game with chosen opening.
        SceneManager.LoadScene(1);
        Debug.Log("Clicked on AcceptButton");
    }

    // Call OnTap of Exit button of MainmenuScreen
    private void OnExit()
    {
        //Go back to main menu
        Debug.Log("Clicked on ReturnButton");
    }

    private void OnEnable()
    {
        panelRenderer.postUxmlReload += OnloadUXML; //!!! THIS CODE NEEDS TO BE RUN ON OnEnable METHOD
    }
}