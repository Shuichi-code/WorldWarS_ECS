
using System;
using Assets.Scripts.Class;
using Assets.Scripts.Monobehaviours.Managers;
using UnityEngine;
using UnityEngine.UIElements;

public class ArmySelectUI : MonoBehaviour
{
    private const string ArmyListVeName = "armyListVE";
    private const string ArmyListName = "armyList";
    private const string TeamListVeName = "teamListVE";
    private const string TeamListName = "teamList";
    private GameObject initializingUI;
    private ListView armyListView;
    private ListView teamListView;
    private GameManager gameManager;

    void Start()
    {
        gameManager = GameManager.GetInstance();
        initializingUI = GameObject.Find(GameConstants.InitializingUIName);
    }
    // Start is called before the first frame update
    void OnEnable()
    {
        var root = GetComponent<UIDocument>();
        var rootVisualElement = root.rootVisualElement;
        rootVisualElement.Q(GameConstants.AcceptBtnName)?.RegisterCallback<ClickEvent>(ev => AcceptArmy());
        rootVisualElement.Q(GameConstants.ReturnBtnName)?.RegisterCallback<ClickEvent>(ev => ReturnToTitle());
        var armyListVisualElement = rootVisualElement.Q(ArmyListVeName);
        var teamListVisualElement = rootVisualElement.Q(TeamListVeName);


        armyListView = VisualElementsUtility.InitializeList(new Dictionaries().armyList, ArmyListName);
        armyListVisualElement.Add(armyListView);
        teamListView = VisualElementsUtility.InitializeList(new Dictionaries().teamList, TeamListName);
        teamListVisualElement.Add(teamListView);
    }

    private void ReturnToTitle()
    {
        //Debug.Log("Clicked on return button.");
    }

    private void AcceptArmy()
    {
        string chosenArmy = armyListView.selectedItem.ToString();
        string chosenTeam = teamListView.selectedItem.ToString();

        gameManager.player.Army = (Army) Enum.Parse(typeof(Army), chosenArmy);
        gameManager.player.Team = (Team)Enum.Parse(typeof(Team), chosenTeam);
        initializingUI.SetActive(true);
        this.gameObject.SetActive(false);
        //Debug.Log("Clicked on accept button.");
    }
}
