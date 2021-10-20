using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Monobehaviours.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class Title : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var root = GetComponent<UIDocument>();
        var rootVisualElement = root.rootVisualElement;
        rootVisualElement.Q("startBtn")?.RegisterCallback<ClickEvent>(ev => BeginGame());
        rootVisualElement.Q("optionBtn")?.RegisterCallback<ClickEvent>(ev => OpenOptions());
        rootVisualElement.Q("exitBtn")?.RegisterCallback<ClickEvent>(ev => ExitGame());
    }

    private void OpenOptions()
    {
        throw new System.NotImplementedException();
    }

    private static void ExitGame()
    {
        Application.Quit();
        Debug.Log("Application Exiting!");
    }

    private static void BeginGame()
    {
        SceneManager.LoadScene("Main");
    }

    // Update is called once per frame
    void Update()
    {

    }
}
