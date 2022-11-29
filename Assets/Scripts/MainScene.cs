using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class MainScene : MonoBehaviour
{
    public static float scoreFirst = 0;
    public static float scoreSecond = 0;
    public static float scoreThird = 0;

    public Text scoreFirstText;
    public Text scoreSecondText;
    public Text scoreThirdText;


    private void Awake()
	{
        LoadGame();
    }

	private void Start()
	{
        scoreFirstText.text = scoreFirst.ToString();
        scoreSecondText.text = scoreSecond.ToString();
        scoreThirdText.text = scoreThird.ToString();
    }

	public static void LoadSceneGame()
	{
		SceneManager.LoadScene("Game");
	}

    public static void LoadSceneStart()
    {
        SceneManager.LoadScene("MainScene");
    }

    void LoadGame()
    {
        if (File.Exists(Application.persistentDataPath + "/SaveDataScore.dat"))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file =
              File.Open(Application.persistentDataPath + "/SaveDataScore.dat", FileMode.Open);
            SaveData data = (SaveData)bf.Deserialize(file);
            file.Close();
            scoreFirst = data.savedScoreFirst;
            scoreSecond = data.savedScoreSecond;
            scoreThird = data.savedScoreThird;
        }
    }
}
