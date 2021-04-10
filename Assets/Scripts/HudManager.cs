using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class HudManager : MonoBehaviour
{
    public GameManager gameManager;
    public Text scoreTxtInstance;
    public Text chrono;
    public float scoresTxtOffset = 30.0f;

    private Text[] scoreTxts;

    void Start()
    {
        Assert.IsNotNull(gameManager);
        Assert.IsNotNull(scoreTxtInstance);
        Assert.IsNotNull(chrono);

        int nbJoueurs = gameManager.PlayersCount();
        scoreTxts = new Text[nbJoueurs];

        scoreTxts[0] = scoreTxtInstance;
        for(int i = 1; i < nbJoueurs; i++)
        {
            scoreTxts[i] = Instantiate(scoreTxts[i-1], this.transform); // recopie le cadre de scores précédent
            scoreTxts[i].rectTransform.position += new Vector3(0, -scoresTxtOffset, 1); // se décale vers le bas, change Z au cas où il y aurait superposition
        }
    }

    // Update is called once per frame
    void Update()
    {
        chrono.text = "" + (int) gameManager.RemainingTime();

        for(int i = 0; i < scoreTxts.Length; i++)
        {
            scoreTxts[i].text = gameManager.PlayerName(i) + " : " + gameManager.PlayerScore(i);
        }
    }
}
