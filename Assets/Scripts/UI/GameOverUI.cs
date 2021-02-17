using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using System;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField]
    public Text gameOverText;
    [SerializeField]
    public GameObject gameOverCanvas;
    [SerializeField]
    public Button tryAgainButton;
    [SerializeField]
    public Button exitButton;
    EntityManager entityManager;

    // Start is called before the first frame update
    void Start()
    {
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ArbiterCheckingSystem>().OnGameWin += GameOverUI_OnGameWin;
        gameOverCanvas.SetActive(false);
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        tryAgainButton.onClick.AddListener(Reset);
    }

    private void GameOverUI_OnGameWin(Color winningColor)
    {
        //Get the GameStateEntity and turn the state into end
        GameManagerComponent gameManagerComponent = entityManager.CreateEntityQuery(typeof(GameManagerComponent)).GetSingleton<GameManagerComponent>();
        gameManagerComponent.state = GameManagerComponent.State.Dead;
        SetSystemsEnabled(false);
        Debug.Log(gameManagerComponent.state);

        //activate the canvas and print the winner
        gameOverCanvas.SetActive(true);
        gameOverText.text = "The winning team is: " + (winningColor == Color.white ? "White": "Black");
    }

    private void Reset()
    {
        Debug.Log("Try again button is pressed");
        entityManager.DestroyEntity(entityManager.CreateEntityQuery((typeof(PieceComponent))));
        GameManagerComponent gameManagerComponent = entityManager.CreateEntityQuery(typeof(GameManagerComponent)).GetSingleton<GameManagerComponent>();
        gameManagerComponent.teamToMove = Color.white;
        BoardManager.GetInstance().createBoard();
        BoardManager.GetInstance().createPieces(Color.white);
        BoardManager.GetInstance().createPieces(Color.black);
        SetSystemsEnabled(true);
        gameOverCanvas.SetActive(false);
    }
    private void SetSystemsEnabled(bool enabled)
    {
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<PiecePickupSystem>().Enabled = enabled;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
