#include "MarchingCubesConstants.hlsl"

struct Triangle
{
    float3 abc[3];
};

float3 CalculateNormal(Triangle t)
{
    return cross(t.abc[1] - t.abc[0], t.abc[2] - t.abc[0]);
}

uint CalculateVoxelIndex(uint3 threadId)
{
    return threadId.x + threadId.y * SLICE_WIDTH + threadId.z * SLICE_WIDTH * SLICE_HEIGHT;
}

uint CalculateTriangulationIndex(RWStructuredBuffer<int> voxels, uint3 threadId)
{
    int corners[] =
    {
        voxels[CalculateVoxelIndex(threadId + uint3(0, 0, 0))],
		voxels[CalculateVoxelIndex(threadId + uint3(1, 0, 0))],
		voxels[CalculateVoxelIndex(threadId + uint3(1, 1, 0))],
		voxels[CalculateVoxelIndex(threadId + uint3(0, 1, 0))],
		voxels[CalculateVoxelIndex(threadId + uint3(0, 0, 1))],
		voxels[CalculateVoxelIndex(threadId + uint3(1, 0, 1))],
		voxels[CalculateVoxelIndex(threadId + uint3(1, 1, 1))],
		voxels[CalculateVoxelIndex(threadId + uint3(0, 1, 1))],
    };

    uint index = 0;
    for (uint i = 0; i < 8; i++)
    {
        if (corners[i] != 0)
        {
            index |= 1 << i;
        }
    }

    return index;
}