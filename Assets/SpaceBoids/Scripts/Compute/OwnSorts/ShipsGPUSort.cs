using System;
using System.Linq;
using UnityEngine;
using static UnityEngine.Mathf;

public class ShipGPUSort
{
    const int threadGroupSize = 128;
    readonly ComputeShader sortCompute;
    private GraphicsBuffer shipsBuffer;
    ComputeBuffer lastIndicesBuffer;

    public ShipGPUSort()
    {
        sortCompute = Resources.Load<ComputeShader>("ShipBitonicMergeSort");
    }

    public void SetBuffers(GraphicsBuffer shipsBuffer, ComputeBuffer lastIndicesBuffer, ComputeBuffer originalIndicesBuffer)
    {
        this.shipsBuffer = shipsBuffer;
        this.lastIndicesBuffer = lastIndicesBuffer;

        sortCompute.SetBuffer(0, "memberData", shipsBuffer);
        sortCompute.SetBuffer(0, "lastIndices", lastIndicesBuffer);
        sortCompute.SetBuffer(0, "originalIndices", originalIndicesBuffer);
    }

    // Sorts given buffer of integer values using bitonic merge sort
    // Note: buffer size is not restricted to powers of 2 in this implementation
    public void Sort()
    {
        var newLastIndices = new int[2];
        lastIndicesBuffer.GetData(newLastIndices);
        newLastIndices[0] = -1;
        lastIndicesBuffer.SetData(newLastIndices);
        sortCompute.SetInt("numEntries", shipsBuffer.count);

        // Launch each step of the sorting algorithm (once the previous step is complete)
        // Number of steps = [log2(n) * (log2(n) + 1)] / 2
        // where n = nearest power of 2 that is greater or equal to the number of inputs
        int numStages = (int)Log(NextPowerOfTwo(shipsBuffer.count), 2);

        for (int stageIndex = 0; stageIndex < numStages; stageIndex++)
        {
            for (int stepIndex = 0; stepIndex < stageIndex + 1; stepIndex++)
            {
                // Calculate some pattern stuff
                int groupWidth = 1 << (stageIndex - stepIndex);
                int groupHeight = 2 * groupWidth - 1;
                sortCompute.SetInt("groupWidth", groupWidth);
                sortCompute.SetInt("groupHeight", groupHeight);
                sortCompute.SetInt("stepIndex", stepIndex);
                sortCompute.SetInt("stageIndex", stageIndex);
                sortCompute.SetInt("numStages", numStages);
                // Run the sorting step on the GPU
                sortCompute.Dispatch(0, CeilToInt(NextPowerOfTwo(shipsBuffer.count) / threadGroupSize), 1, 1);
            }
        }
    }
}