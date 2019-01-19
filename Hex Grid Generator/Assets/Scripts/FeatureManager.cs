using System;
using UnityEngine;

public class FeatureManager : MonoBehaviour
{
    [Serializable]
    public struct FeatureCollection
    {
        public Transform[] Prefabs;

        /// <summary>
        /// Unfortunately, the editor does not show arrays of arrays. So we cannot configure them. 
        /// To work around this, we have to create a serializable struct that encapsulates the nested array.
        /// This method takes care of the conversion from a choice to an array index and returns the prefab. 
        /// </summary>
        public Transform Pick(float choice) => Prefabs[(int)(choice * Prefabs.Length)];
    }

    // this is used for rotation randomization
    public const int HashGridSize = 256;
    public const float HashGridScale = 0.25f;

    /// <summary>
    /// Prefabs are ordered from the highest to the lowest probability.
    /// </summary>
    public FeatureCollection[] FeatureCollections;
    Transform _container;

    static HexHash[] _hashGrid;
    static readonly float[][] _featureThresholds = {
        new float[] { 0.0f, 0.0f, 0.4f },
        new float[] { 0.0f, 0.4f, 0.6f },
        new float[] { 0.4f, 0.6f, 0.8f }
    };

    public void Clear()
    {
        if (_container)
            Destroy(_container.gameObject);

        _container = new GameObject("Features Container").transform;
        _container.SetParent(transform, false);
    }

    public void Apply() { }

    public void AddFeature(Vector3 position)
    {
        HexHash hash = SampleHashGrid(position);
        if (hash.ExistanceProbability >= 0.5f) // this will eliminate about half of the features
            return;

        // we should somehow, probably randomly, support more than one feature
        int featureDensityLevel = 3;

        Transform prefab = PickPrefab(featureDensityLevel, hash.ExistanceProbability, hash.Choice);
        if (!prefab)
            return;

        Transform instance = Instantiate(prefab);
        position.y += instance.localScale.y * 0.5f;
        instance.localPosition = HexMetrics.Perturb(position);
        instance.localRotation = Quaternion.Euler(0f, 360f * hash.Rotation, 0f);

        float scale = (float)(0.8 + 0.4 * hash.Rotation);
        instance.localScale = new Vector3(scale, scale, scale);

        instance.SetParent(_container, false);
    }

    Transform PickPrefab(int level, float hash, float choice)
    {
        if (level > 0)
        {
            float[] thresholds = _featureThresholds[level - 1];
            for (int i = 0; i < thresholds.Length; i++)
                if (hash < thresholds[i])
                    return FeatureCollections[i].Pick(choice);
        }
        return null;
    }

    /// <summary>
    /// Inserts random values into the hashgrid.
    /// </summary>
    public static void InitializeHashGrid(int seed)
    {
        UnityEngine.Random.State currentState = UnityEngine.Random.state;

        _hashGrid = new HexHash[HashGridSize * HashGridSize];
        UnityEngine.Random.InitState(seed);
        for (int i = 0; i < _hashGrid.Length; i++)
            _hashGrid[i] = HexHash.Create();

        // Random generator need to restore its previous status 
        // in order to avoid all random event to be exactly the same
        UnityEngine.Random.state = currentState;
    }

    public HexHash SampleHashGrid(Vector3 position)
    {
        int x = (int)(position.x * HashGridScale) % HashGridSize;
        if (x < 0)
            x += HashGridSize;

        int z = (int)(position.z * HashGridScale) % HashGridSize;
        if (z < 0)
            z += HashGridSize;

        return _hashGrid[x + z * HashGridSize];
    }
}
