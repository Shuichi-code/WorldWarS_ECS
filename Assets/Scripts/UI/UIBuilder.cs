using System.Collections;
using System.Collections.Generic;
using Unity.UIElements.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

public class UIBuilder : MonoBehaviour
{
    private Button deployButton;

    private void OnEnable()
    {
        var rootVisualElement = GameObject.Find("UIBuilderManager").GetComponent<PanelRenderer>().visualTree;

        deployButton = rootVisualElement.Q<Button>(name = "DeployButton");
        deployButton.RegisterCallback<MouseUpEvent>(ev => ButtonPressed());
    }

    private void ButtonPressed()
    {
        Debug.Log("Deploy Button has been pressed!");
    }
}
