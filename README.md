# PoissonDiscSampling
![infinite_forest](https://user-images.githubusercontent.com/105425086/208936646-27a57450-959d-43d7-b9a3-073931274d7e.png)

## Description
This is an implementation of Poisson disc sampling written in Unity Engine's C# environment. This implementation is made to solve the problem of organic looking pseudo-random placement of objects.

## Problem layout
One way to populate the world with objects is to place them in a grid or generate their coordinates randomly. Both of those methods come with their own set of problems to solve. For example, grid placement, as the name suggests, gives a rigid repetetive pattern, while pure pseudo-random generation of coordinates might look organic, but it has its own problems too. The fact that objects with volume greater than zero will be placed on generated positions, can cause overlap in their meshes, or more generally, the objects placed can end up undesirably close to each other. To mittigate the problem of overlap, we introduce minimal distance between generated coordinates. Simple implementation would be something like this:

1. Generate a random coordinate
2. Check Euclidian distance to every previously placed coordinate
3. If new coordinate is distant enough from the other ones, place it down
4. Go to step one

It is obvious that this approach would work, however it is not efficient at all. For every generated coordinate n, there needs to be n-1 distances checked. Not only that, but as we place more coordinates, the field becomes more saturated and chance of placing a new coordinate in a valid spot becomes lower. Therefore, as the number of coodinates gets larger, computation required to place each one of them goes from O(1*n^2) to O(m*n^2), where m is some positive integer which represents numbers of coordinates tried before a succesful one.

## Solution
Poisson disc sampling is a way to populate an area with tightly packed points in a way which looks organic, and it does that in linear time utilizing two previously mentioned ways of object scattering. Firstly, it uses a grid to separate coordinates in cells. If all coordinates are containd in cells of a grid, there is no need to check every point for collision. When generating a new point only cells that are in range of double minimal distance of newly generated point become suspects for collision and points contained in them need to be checked. This process greatly reduces the amount of computation required.

#### How the algorithm works
1. A grid is created such that every cell will contain at most one point.
2. The first point is randomly chosen, and put in the output list, processing list and grid.
3. Until the processing list is empty, do the following:
    1. Choose a random point from the processing list.
    2. For this point, generate up to k points (larger values of k give tighter packing of points, but make the algorithm run slower). For every generated point:
        1. Use the grid to check for points that are too close to this point.
        2. If there is none, add the point to the output list, processing list, and grid.
    3. Remove the point from the processing list.
4. Return the output list as the sample points.

## How to use
Define variables needed to generate point
```
private float[] objectDiameters;   // Discreet set of sizes
                                   // Larger ranges tend to need higher rejectionSamples values to pack points tightly
private Vector2 regionSize;        // Field size starting from regionOffset vetor towards positive coordinates
private vector2 regionOffset;      // Offset from origin (0, 0, 0) coordinate
private int rejectionSamples = 20; // Number of tries to place each point
int seed;                          // For consistency throughout multiple point generations
```

Define a container for generated points

```
private List<Vector3> points;
```

Initialize PoissonSampling object and generate points
```
PoissonSampling ps = new PoissonSampling(
    objectDiameters, 
    regionSize, 
    regionOffset, 
    rejectionSamples, 
    seed);
    
points = ps.GeneratePoints();
```

## References
- Robert Bridson, Fast Poisson Disk Sampling in Arbitrary Dimensions, University of British Columbia https://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf
