using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class GameManager : MonoBehaviour
{
    public float score—urrent = 0;
    public float scoreStart = 0;
    public Text scoreText;
    public Text multiplierText;

    private float lerp = 0;
    private float duration = 2f;

    public GameObject settingWindow;
    public GameObject recordsPanel;
    public GameObject settingsMusicPanel;

    private AudioManager audioManager;


    void Awake()
    {
        
    }

	private void Start()
	{
        audioManager = FindObjectOfType<AudioManager>();
    }

	private void OnGUI()
	{
        if(scoreText)
		{
            lerp += Time.deltaTime / duration;
            scoreStart = (int)Mathf.Lerp(scoreStart, score—urrent, lerp);
            scoreText.text = scoreStart.ToString();
        }
    }

	public void AddScore(float add, float mult)
	{
        lerp = 0;
        score—urrent += add * mult;

        multiplierText.text = mult.ToString();

        SaveGame();
    }

    public void ToogleSetting()
	{
        settingWindow.SetActive(!settingWindow.activeInHierarchy);
        settingWindow.transform.Find("Background").GetComponent<Animator>().SetTrigger("Setting");
    }

    public void ToggleRecords()
    {
        recordsPanel.SetActive(!recordsPanel.activeInHierarchy);
        recordsPanel.transform.Find("Background").GetComponent<Animator>().SetTrigger("Records");
    }

    public void ToggleSettingsMusic()
    {
        settingsMusicPanel.SetActive(!settingsMusicPanel.activeInHierarchy);
        settingsMusicPanel.transform.Find("Background").GetComponent<Animator>().SetTrigger("Music");
    }

    public void ExitGame()
    {
        MainScene.LoadSceneStart();
    }

    void SaveGame()
    {
        float temp_score = 0;
        float temp_score2 = 0;
        float first = MainScene.scoreFirst;
        float second = MainScene.scoreSecond;
        float third = MainScene.scoreThird;

        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/SaveDataScore.dat");
        SaveData data = new SaveData();

        if(score—urrent >= MainScene.scoreThird && score—urrent <= MainScene.scoreSecond)
		{
            third = score—urrent;
        }
        else if(score—urrent >= MainScene.scoreSecond && score—urrent <= MainScene.scoreFirst)
		{
            temp_score = MainScene.scoreSecond;

            second = score—urrent;
            third = temp_score >= MainScene.scoreThird ? temp_score : MainScene.scoreThird;
        }
        else if(score—urrent >= MainScene.scoreFirst)
		{
            temp_score = MainScene.scoreFirst;
            temp_score2 = MainScene.scoreSecond;

            first = score—urrent;
            second = temp_score >= MainScene.scoreSecond ? temp_score : MainScene.scoreSecond;
            third = temp_score2 >= MainScene.scoreThird ? temp_score2 : MainScene.scoreThird;
        }

        data.savedScoreFirst = first;
        data.savedScoreSecond = second;
        data.savedScoreThird = third;

        bf.Serialize(file, data);
        file.Close();
    }
}

[Serializable]
class SaveData
{
    public float savedScoreFirst;
    public float savedScoreSecond;
    public float savedScoreThird;
}
