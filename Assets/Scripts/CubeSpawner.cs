using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
 public enum MoveMode
{
    Normal,
    Jobs,
    Burst
}

[Serializable]
public struct SceneData{
    public MoveMode mode;
    public int cubes;
}


public class CubeSpawner : MonoBehaviour
{
    public Text fpsText;
    public GameObject movingCube;
    int numberOfCubes = 0;
    float spawningInterval = 0.1f;
    int spawnRate = 40;
    int borderX = 130;
    int borderY = 130;
    int cubeLimit = 5000;
    float spawnTime = 0;
    float fpsBorder = 8;
    public float sceneTime;
    public readonly float sceneTimeOffset = 2;
    MovementOrchestrator movementOrchestrator;
    public List<CubeMovement> cubeMovements;

    public MoveMode moveMode;

    // Start is called before the first frame update
    void Start()
    {
        moveMode = SimulationCommon.moveMode;
        sceneTime = 0;
        spawnTime = spawningInterval;
        cubeMovements = new List<CubeMovement>();
        movementOrchestrator = new MovementOrchestrator(cubeMovements, cubeLimit);
    }

    float randomizeDirection()
    {
        float roll = UnityEngine.Random.Range(0,2) * 2;
        return roll - 1;
    }

    void spawnCubes()
    {
        for(int i = 0; i < spawnRate; i++)
        {
            GameObject cube = Instantiate(movingCube, new Vector3(UnityEngine.Random.Range(0,borderX) * randomizeDirection(),5,UnityEngine.Random.Range(0,borderY) * randomizeDirection()), Quaternion.identity);
            CubeMovement cubeMovement = cube.GetComponent<CubeMovement>();
            cubeMovement.Initialize();
            cubeMovements.Add(cubeMovement);
        }
    }

    void HandleSimulationEnd()
    {
        SceneData data = new SceneData{
            mode = SimulationCommon.moveMode,
            cubes = numberOfCubes
        };
        SimulationCommon.HandleSimulationEnd(data);
    }

    void FixedUpdate()
    {
        sceneTime = sceneTime + Time.fixedDeltaTime;
        if (sceneTime > sceneTimeOffset && Time.time > spawnTime)
        {
            spawnTime += spawningInterval;
            numberOfCubes = numberOfCubes + spawnRate;
            spawnCubes();
        }
        movementOrchestrator.moveCubes();
    }

    void Update () {
        var fps = Mathf.Ceil(1.0f / Time.deltaTime);
        fpsText.text = fps.ToString();
        if(fps<fpsBorder && sceneTime > sceneTimeOffset)
        {
            HandleSimulationEnd();
        }
     }
}
