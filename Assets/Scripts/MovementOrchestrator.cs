using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

public class MovementOrchestrator
{
    int cubeLimit;
    List<CubeMovement> cubeMovements;
    MoveMode moveMode;
    NativeArray<Vector3> cubesPositionsNativeArray;
    List<NativeArray<Vector3>> forces;
    List<NativeArray<float>> currentTimes;


    public MovementOrchestrator(List<CubeMovement> cubes, int cubeLimit)
    {
        this.cubeMovements = cubes;
        this.moveMode = SimulationCommon.moveMode;
        this.cubeLimit = cubeLimit;
        if(moveMode != MoveMode.Normal)
        {
            cubesPositionsNativeArray = new NativeArray<Vector3>(cubeLimit, Allocator.Persistent);
        }
    }
    ~MovementOrchestrator()
    {
        if(moveMode != MoveMode.Normal)
        {
            cubesPositionsNativeArray.Dispose();
            for(int i = 0;i<forces.Count;i++)
            {
                forces[i].Dispose();
                currentTimes[i].Dispose();
            }
        }
    }

    public void updatePositionsNativeArray()
    {
        int index = 0;
        foreach(var cube in cubeMovements)
        {
            cubesPositionsNativeArray[index] = cube.gameObject.transform.position;
        }
    }

    public void moveCubes()
    {
        List<JobHandle> handles = new List<JobHandle>();
        List<int> indexes = new List<int>();
        forces = new List<NativeArray<Vector3>>();
        currentTimes = new List<NativeArray<float>>();
        if(moveMode != MoveMode.Normal)
        {
            updatePositionsNativeArray();
            for(int i = 0; i < cubeMovements.Count; i++)
            {
                if(cubeMovements[i].ifDirectionHaveToChange())
                {
                    var force = new NativeArray<Vector3>(1, Allocator.TempJob);
                    var time = new NativeArray<float>(1, Allocator.TempJob);
                    if(moveMode == MoveMode.Jobs)
                    {
                        var job =  new ChangeDirectionJob()
                        {
                            currentJobForce = force,
                            currentMovementTime = time,
                            cubePositions = cubesPositionsNativeArray,
                            currentPosition = cubeMovements[i].gameObject.transform.position,
                            seed = (uint)Random.Range(0,uint.MaxValue),
                            fixedDeltaTime = Time.fixedDeltaTime
                        };
                        
                        JobHandle jobHandle = job.Schedule();
                        currentTimes.Add(time);
                        forces.Add(force);
                        handles.Add(jobHandle);
                        indexes.Add(i);
                    }
                    else
                    {
                        var job =  new ChangeDirectionJobBurst()
                        {
                            currentJobForce = force,
                            currentMovementTime = time,
                            cubePositions = cubesPositionsNativeArray,
                            currentPosition = cubeMovements[i].gameObject.transform.position,
                            seed = (uint)Random.Range(1,uint.MaxValue),
                            fixedDeltaTime = Time.fixedDeltaTime
                        };
                        
                        JobHandle jobHandle = job.Schedule();
                        currentTimes.Add(time);
                        forces.Add(force);
                        handles.Add(jobHandle);
                        indexes.Add(i);
                    }
                }
            }
            int currentIndex = 0;
            foreach(var handle in handles)
            {
                handle.Complete();
                cubeMovements[indexes[currentIndex]].changeDirection(forces[currentIndex][0],currentTimes[currentIndex][0]);
                forces[currentIndex].Dispose();
                currentTimes[currentIndex].Dispose();
                currentIndex++;
            }
        }
        else
        {
            foreach(var cubeMovement in cubeMovements)
            {
                if(cubeMovement.ifDirectionHaveToChange())
                {
                    changeDirectionSingleThreaded(cubeMovement);
                }
            }
        }
    }

    float randomizeDirection()
    {
        float roll = Random.Range(0,2) * 2;
        return roll - 1;
    }

    void changeDirectionSingleThreaded(CubeMovement cubeMovement)
    {
        float roll = Random.Range(0.0f,1.0f);
        Vector3 currentForce;
        float movementTime;
        if(roll<=0.5)
        {
            if(roll<=0.25)
            {
                currentForce = new Vector3(Random.Range(15,20) * randomizeDirection(),Random.Range(50,80),Random.Range(15,20) * randomizeDirection());
                movementTime = Random.Range(3,12) / Time.fixedDeltaTime;
            }
            else
            {
                currentForce = new Vector3(Random.Range(15,20) * randomizeDirection(),0,Random.Range(15,20) * randomizeDirection());
                movementTime = Random.Range(5,12) / Time.fixedDeltaTime;
            }
        }
        else
        {
            Vector3 vectorToFarthestCube = new Vector3(0,0,0);
            float maxDistance = -1;
            foreach(var cube in cubeMovements)
            {
                float distanceToConsider = (cube.gameObject.transform.position - cubeMovement.gameObject.transform.position).magnitude;
                if(distanceToConsider>maxDistance)
                {
                    vectorToFarthestCube = (cube.gameObject.transform.position - cubeMovement.gameObject.transform.position).normalized;
                    maxDistance = distanceToConsider;
                }
            }
            currentForce = vectorToFarthestCube*Random.Range(20,25);
            movementTime = Random.Range(5,8) / Time.fixedDeltaTime;
        }
        cubeMovement.changeDirection(currentForce,movementTime);

    }

    public struct ChangeDirectionJob : IJob
    {
        [ReadOnly]
        public NativeArray<Vector3> cubePositions;
        public NativeArray<Vector3> currentJobForce;
        public NativeArray<float> currentMovementTime;
        public Vector3 currentPosition;
        public uint seed;
        public float fixedDeltaTime;
        float randomizeDirection(Unity.Mathematics.Random random)
        {
            float roll = random.NextInt(0,2) * 2;
            return roll - 1;
        }

        public void Execute()
        {
            Unity.Mathematics.Random random = new Unity.Mathematics.Random(seed);
            float roll = random.NextFloat(0.0f,1.0f);
            if(roll<=0.5)
            {
                if(roll<=0.25)
                {
                    currentJobForce[0] = new Vector3(random.NextInt(15,20) * randomizeDirection(random),random.NextInt(50,80),random.NextInt(15,20) * randomizeDirection(random));
                    currentMovementTime[0] = random.NextInt(3,8) / fixedDeltaTime;
                }
                else
                {
                    currentJobForce[0] = new Vector3(random.NextInt(15,20) * randomizeDirection(random),0,random.NextInt(15,20) * randomizeDirection(random));
                    currentMovementTime[0] = random.NextInt(5,12) / fixedDeltaTime;
                }
            }
            else
            {
                Vector3 vectorToFarthestCube = new Vector3(0,0,0);
                float maxDistance = -1;
                foreach(var cubePosition in cubePositions)
                {
                    float distanceToConsider = (cubePosition - currentPosition).magnitude;
                    if(distanceToConsider>maxDistance)
                    {
                        vectorToFarthestCube = (cubePosition - currentPosition).normalized;
                        maxDistance = distanceToConsider;
                    }
                }
                currentJobForce[0] = vectorToFarthestCube*random.NextInt(20,25);
                currentMovementTime[0] = random.NextInt(5,8) / fixedDeltaTime;
            }
        }
    }

    [BurstCompile]
    public struct ChangeDirectionJobBurst : IJob
    {
        [ReadOnly]
        public NativeArray<Vector3> cubePositions;
        public NativeArray<Vector3> currentJobForce;
        public NativeArray<float> currentMovementTime;
        public Vector3 currentPosition;
        public uint seed;
        public float fixedDeltaTime;
        float randomizeDirection(Unity.Mathematics.Random random)
        {
            float roll = random.NextInt(0,2) * 2;
            return roll - 1;
        }

        public void Execute()
        {
            Unity.Mathematics.Random random = new Unity.Mathematics.Random(seed);
            float roll = random.NextFloat(0.0f,1.0f);
            if(roll<=0.5)
            {
                if(roll<=0.25)
                {
                    currentJobForce[0] = new Vector3(random.NextInt(15,20) * randomizeDirection(random),random.NextInt(50,80),random.NextInt(15,20) * randomizeDirection(random));
                    currentMovementTime[0] = random.NextInt(3,8) / fixedDeltaTime;
                }
                else
                {
                    currentJobForce[0] = new Vector3(random.NextInt(15,20) * randomizeDirection(random),0,random.NextInt(15,20) * randomizeDirection(random));
                    currentMovementTime[0] = random.NextInt(5,12) / fixedDeltaTime;
                }
            }
            else
            {
                Vector3 vectorToFarthestCube = new Vector3(0,0,0);
                float maxDistance = -1;
                foreach(var cubePosition in cubePositions)
                {
                    float distanceToConsider = (cubePosition - currentPosition).magnitude;
                    if(distanceToConsider>maxDistance)
                    {
                        vectorToFarthestCube = (cubePosition - currentPosition).normalized;
                        maxDistance = distanceToConsider;
                    }
                }
                currentJobForce[0] = vectorToFarthestCube*random.NextInt(20,25);
                currentMovementTime[0] = random.NextInt(5,8) / fixedDeltaTime;
            }
        }
    }
}
