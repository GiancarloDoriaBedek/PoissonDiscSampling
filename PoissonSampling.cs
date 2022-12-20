using System.Collections.Generic;
using UnityEngine;

public class PoissonSampling
{
    private float[] _objectDiameters;
    private Vector2 _sampleRegionSize;
    private Vector2 _sampleRegionOffset;
    private int _sampleCountBeforeRejection;
    private int _seed;
    private List<Vector3> _points = new List<Vector3>();
    private List<Vector2> _spawnPoints = new List<Vector2>();
    private int[,] _grid;
    private float _smallestDiameter;
    private float _largestDiameter;
    private float _cellSize;
    private int _searchDepth; // Needed for offset magnitude calculation

    public float[] ObjectDiameters => _objectDiameters;
    public Vector2 SampleRegionSize => _sampleRegionSize;
    public Vector2 SampleRegionOffset => _sampleRegionOffset;
    public int SampleCountBeforeRejection => _sampleCountBeforeRejection;
    public int Seed => _seed;
    public List<Vector3> Points => _points;
    public float CellSize => _cellSize;


    public PoissonSampling(float[] objectDiameters, Vector2 sampleRegionSize, Vector2 sampleRegionOffset, int sampleCountBeforeRejection, int seed)
    {
        _objectDiameters = objectDiameters;
        _sampleRegionSize = sampleRegionSize;
        _sampleRegionOffset = sampleRegionOffset;
        _sampleCountBeforeRejection = sampleCountBeforeRejection;
        _seed = seed;

        InitializeGridElements();
    }


    private void InitializeGridElements()
    {
        _smallestDiameter = Mathf.Min(_objectDiameters);
        _largestDiameter = Mathf.Max(_objectDiameters);
        _cellSize = _smallestDiameter / Mathf.Sqrt(2);

        _grid = new int[Mathf.CeilToInt(_sampleRegionSize.x / _cellSize), Mathf.CeilToInt(_sampleRegionSize.y / _cellSize)];
        _spawnPoints.Add(_sampleRegionSize / 2); //o
        _searchDepth = Mathf.CeilToInt(Mathf.Max(_objectDiameters) * 2);
        Random.InitState(_seed);
    }

    /// <summary>
    /// Runs Poisson Disc Sampling algorithm. Coordinates are saved in "public List<Vector3> Points".
    /// Values 'x' and 'y' values of each point represent planar coordinates of that point. Value 'z'
    /// has embedded diameter value.
    /// </summary>
    /// <returns>List of generated points</returns>
    public List<Vector3> GeneratePoints()
    {
        while (_spawnPoints.Count > 0)
        {
            int spawnIndex = Random.Range(0, _spawnPoints.Count);
            Vector2 spawnCenter = _spawnPoints[spawnIndex];

            int randomRadiusIndex = Random.Range(0, _objectDiameters.Length);
            float currentRandomRadius = _objectDiameters[randomRadiusIndex];

            GenerateNewPoint(spawnIndex, spawnCenter, currentRandomRadius);
        }

        for (int i = 0; i < _points.Count; i++)
            _points[i] = new Vector3(_points[i].x + _sampleRegionOffset.x, _points[i].y + _sampleRegionOffset.y, _points[i].z);

        return _points;
    }

    /// <summary>
    /// Generates a new point some random distance and angle away from spawnCenter.
    /// </summary>
    /// <param name="spawnIndex">Index of a new point</param>
    /// <param name="spawnCenter">Initial spawn coordinate</param>
    /// <param name="currentRandomRadius">Radius of a new point</param>
    private void GenerateNewPoint(int spawnIndex, Vector2 spawnCenter, float currentRandomRadius)
    {
        for (int i = 0; i < _sampleCountBeforeRejection; i++)
        {
            Vector2 candidatePosition = RandomOffset(spawnCenter);

            if (IsValidPosition(candidatePosition) && IsWithoutCollisions(candidatePosition, currentRandomRadius))
            {
                AddPositionToPoints(candidatePosition, currentRandomRadius);
                return;
            }
        }

        _spawnPoints.RemoveAt(spawnIndex);
    }

    /// <summary>
    /// Embedds radius into z value of position vector and adds it as a valid position that will be used 
    /// to determine positions of later points.
    /// </summary>
    /// <param name="candidatePosition">Candidates coordinates</param>
    /// <param name="currentRandomRadius">Randomly selected radius from _objectDiameters field</param>
    void AddPositionToPoints(Vector2 candidatePosition, float currentRandomRadius)
    {
        Vector3 pointWithEmbeddedRadius = new Vector3(candidatePosition.x, candidatePosition.y, currentRandomRadius);
        _points.Add(pointWithEmbeddedRadius);
        _spawnPoints.Add(candidatePosition);
        _grid[(int)(candidatePosition.x / _cellSize), (int)(candidatePosition.y / _cellSize)] = _points.Count;
    }

    /// <summary>
    /// Generates random offset from a given position. Offset magnitude is determined by smallest and largest
    /// possible point diameters.
    /// </summary>
    /// <param name="position"></param>
    /// <returns>New position</returns>
    private Vector2 RandomOffset(Vector2 position)
    {
        float randomAngle = Random.value * Mathf.PI * 2;
        Vector2 randomDirection = new Vector2(Mathf.Sin(randomAngle), Mathf.Cos(randomAngle));
        float randomOffset = (float)Mathf.Sqrt(Random.Range(_smallestDiameter * _smallestDiameter, 4 * _largestDiameter * _largestDiameter));

        return position + randomDirection * randomOffset;
    }

    /// <summary>
    /// Checks if candidatePosition falls into allowed area.
    /// </summary>
    /// <param name="candidatePosition">Candidates coordinates</param>
    /// <param name="currentRandomRadius">Randomly selected radius from _objectDiameters field</param>
    /// <returns>True if candidatePosition is inside allowed region, otherwise returns false</returns>
    private bool IsValidPosition(Vector2 candidatePosition)
    {
        if (candidatePosition.x < 0 || candidatePosition.x > _sampleRegionSize.x || candidatePosition.y < 0 || candidatePosition.y > _sampleRegionSize.y)
            return false;

        return true;
    }

    /// <summary>
    /// Checks every field in a grid that could contain previously placed point 
    /// whoose radius could collide with current candidates radius.
    /// </summary>
    /// <param name="candidatePosition">Candidates coordinates</param>
    /// <param name="currentRandomRadius">Randomly selected radius from _objectDiameters field</param>
    /// <returns>True if candidatePosition is without collisions, otherwise false</returns>
    private bool IsWithoutCollisions(Vector2 candidatePosition, float currentRandomRadius)
    {
        // candidatePosition grid position
        Vector2Int candidateIndex = PositionToCellIndex(candidatePosition);

        int searchStartX = Mathf.Max(0, candidateIndex.x - _searchDepth);
        int searchEndX = Mathf.Min(candidateIndex.x + _searchDepth, _grid.GetLength(0) - 1);
        int searchStartY = Mathf.Max(0, candidateIndex.y - _searchDepth);
        int searchEndY = Mathf.Min(candidateIndex.y + _searchDepth, _grid.GetLength(1) - 1);

        for (int x = searchStartX; x <= searchEndX; x++)
        {
            for (int y = searchStartY; y <= searchEndY; y++)
            {
                int pointIndex = _grid[x, y] - 1;

                if (pointIndex != -1)
                {
                    Vector2 otherPoint = new Vector2(_points[pointIndex].x, _points[pointIndex].y);
                    float distanceSquared = (candidatePosition - otherPoint).sqrMagnitude;
                    float radiusSum = _points[pointIndex].z + currentRandomRadius;

                    if (distanceSquared < radiusSum * radiusSum)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private Vector2Int PositionToCellIndex(Vector2 position)
    {
        int cellX = (int)(position.x / _cellSize);
        int cellY = (int)(position.y / _cellSize);

        return new Vector2Int(cellX, cellY);
    }
}
