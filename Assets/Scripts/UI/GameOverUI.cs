using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using System;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    private Text gameOverText;
    // Start is called before the first frame update
    void Start()
    {
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ArbiterCheckingSystem>().OnGameWin += GameOverUI_OnGameWin;
        gameOverText = transform.Find("winnerText").GetComponent<Text>();
    }

    private void GameOverUI_OnGameWin(Color winningColor)
    {
        Debug.Log("The winner is: Team "+winningColor.ToString());
        //get the panel
        gameOverText.text = winningColor.ToString();
    }


    // Update is called once per frame
    void Update()
    {

    }
}
