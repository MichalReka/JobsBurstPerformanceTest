using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;


public static class SimulationCommon
{
    public static List<SceneData> simulationOutput = new List<SceneData>();
    public static MoveMode moveMode = MoveMode.Jobs;
    public static int iterationsPerMode = 10;
    public static void HandleSimulationEnd(SceneData data)
    {
        simulationOutput.Add(data);
        if(simulationOutput.Count == (((int)moveMode)+1)*iterationsPerMode)
        {
            if(moveMode==MoveMode.Burst)
            {
                var json = JsonConvert.SerializeObject(simulationOutput,Formatting.Indented);
                System.IO.File.WriteAllText(@".\testJson.json", json);
                #if UNITY_EDITOR
                    // Application.Quit() does not work in the editor so
                    // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
            }
            else
            {
                moveMode = (MoveMode)(((int)moveMode) + 1);
            }
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}