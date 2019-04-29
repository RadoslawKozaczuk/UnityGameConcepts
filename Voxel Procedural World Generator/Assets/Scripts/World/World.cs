﻿using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.World
{
	[CreateAssetMenu]
	public class World : ScriptableObject
	{
		public const int WorldSizeY = 4, ChunkSize = 32;
		const int OverallNumberOfGenerationSteps = 9;

		public static GameSettings Settings;

		public readonly int TotalBlockNumberX, TotalBlockNumberY, TotalBlockNumberZ;

		public ChunkData[,,] Chunks;
		public ChunkObject[,,] ChunkObjects;
		public BlockData[,,] Blocks;
		public Vector3 PlayerLoadedRotation, PlayerLoadedPosition;
        public WorldGeneratorStatus Status;
        public float TerrainProgressSteps;
        public float MeshProgressSteps;
        public float AlreadyGenerated;
		public string ProgressDescription;

		[SerializeField] TerrainGenerator _terrainGenerator;
		[SerializeField] MeshGenerator _meshGenerator;
		[SerializeField] Material _terrainTexture;
		[SerializeField] Material _waterTexture;

		Stopwatch _stopwatch = new Stopwatch();
		Scene _worldScene;
		int _progressStep = 1;

		World()
		{
			TotalBlockNumberX = Settings.WorldSizeX * ChunkSize;
			TotalBlockNumberY = WorldSizeY * ChunkSize;
			TotalBlockNumberZ = Settings.WorldSizeZ * ChunkSize;
		}

		void OnEnable()
		{
			_terrainGenerator = new TerrainGenerator(Settings);
			_meshGenerator = new MeshGenerator(Settings);
		}

        /// <summary>
		/// Generates block types with hp and hp level.
		/// Chunks and their objects (if first run = true).
		/// And calculates faces.
		/// </summary>
		public IEnumerator CreateWorld(bool firstRun, Action callback)
        {
            _stopwatch.Restart();
            ResetProgressBarVariables();

            ProgressDescription = "Initialization...";
            Status = WorldGeneratorStatus.NotReady;
            Blocks = new BlockData[Settings.WorldSizeX * ChunkSize, WorldSizeY * ChunkSize, Settings.WorldSizeZ * ChunkSize];
            AlreadyGenerated += _progressStep;
            yield return null;

            ProgressDescription = "Calculating heights...";
            var heights = _terrainGenerator.CalculateHeights();
            AlreadyGenerated += _progressStep;
            yield return null;

            ProgressDescription = "Calculating block types...";
            var types = _terrainGenerator.CalculateBlockTypes(heights);
            AlreadyGenerated += _progressStep;
            yield return null;

            ProgressDescription = "Job output deflattenization...";
            DeflattenizeOutput(ref types);
            AlreadyGenerated += _progressStep;
            yield return null;

            if (Settings.IsWater)
            {
                ProgressDescription = "Generating water...";
                _terrainGenerator.AddWater(ref Blocks);
            }
            AlreadyGenerated += _progressStep;
            yield return null;

            ProgressDescription = "Generating trees...";
            _terrainGenerator.AddTrees(ref Blocks, Settings.TreeProbability);
            AlreadyGenerated += _progressStep;
            yield return null;

            ProgressDescription = "Chunk data initialization...";
            if (firstRun)
                _worldScene = SceneManager.CreateScene(name);

            // chunkData need to be initialized earlier in order to allow main loop iterate over chunks before their meshes are ready
            // thanks to this we can display chunk as soon as they become ready 
            Chunks = new ChunkData[Settings.WorldSizeX, WorldSizeY, Settings.WorldSizeZ];
            for (int x = 0; x < Settings.WorldSizeX; x++)
                for (int z = 0; z < Settings.WorldSizeZ; z++)
                    for (int y = 0; y < WorldSizeY; y++)
                        Chunks[x, y, z] = new ChunkData(new Vector3Int(x, y, z), new Vector3Int(x * ChunkSize, y * ChunkSize, z * ChunkSize));

            ChunkObjects = new ChunkObject[Settings.WorldSizeX, WorldSizeY, Settings.WorldSizeZ];
            yield return null;

            ProgressDescription = "Calculating faces...";
            _meshGenerator.CalculateFaces(ref Blocks);
            AlreadyGenerated += _progressStep;
            yield return null;

            ProgressDescription = "World boundaries check...";
            _meshGenerator.WorldBoundariesCheck(ref Blocks);
            AlreadyGenerated += _progressStep;
            yield return null;

            ProgressDescription = "Creating chunks...";
            // create chunk objects one by one 
            for (int x = 0; x < Settings.WorldSizeX; x++)
                for (int z = 0; z < Settings.WorldSizeZ; z++)
                    for (int y = 0; y < WorldSizeY; y++)
                    {
                        ChunkData chunkData = Chunks[x, y, z];
                        _meshGenerator.CalculateMeshes(ref Blocks, chunkData.Position, out Mesh terrainMesh, out Mesh waterMesh);

                        if (firstRun)
                        {
                            var chunkObject = new ChunkObject();
                            CreateChunkGameObjects(ref chunkData, ref chunkObject, terrainMesh, waterMesh);
                            SceneManager.MoveGameObjectToScene(chunkObject.Terrain.gameObject, _worldScene);
                            SceneManager.MoveGameObjectToScene(chunkObject.Water.gameObject, _worldScene);
                            ChunkObjects[x, y, z] = chunkObject;
                        }
                        else
                        {
                            SetMeshesAndCollider(ref chunkData, ref ChunkObjects[x, y, z], terrainMesh, waterMesh);
                        }

                        Chunks[x, y, z].Status = ChunkStatus.NeedToBeRedrawn;
                        AlreadyGenerated++;

                        yield return null; // give back control
                    }

            Status = WorldGeneratorStatus.TerrainReady;

            _stopwatch.Stop();
            UnityEngine.Debug.Log($"It took {_stopwatch.ElapsedTicks / TimeSpan.TicksPerMillisecond} ms to generate all terrain.");

            ProgressDescription = "Generation Completed";
            callback?.Invoke();
        }

        /// <summary>
		/// Generates block types with hp and hp level.
		/// Chunks and their objects (if first run = true).
		/// And calculates faces.
		/// </summary>
		public IEnumerator LoadWorld(bool firstRun, Action callback = null)
        {
            ResetProgressBarVariables();
            _stopwatch.Restart();

            ProgressDescription = "Loading data...";
            Status = WorldGeneratorStatus.NotReady;
            var storage = new PersistentStorage(ChunkSize);
            SaveGameData save = storage.LoadGame();
            Settings.WorldSizeX = save.WorldSizeX;
            Settings.WorldSizeZ = save.WorldSizeZ;
            PlayerLoadedPosition = save.PlayerPosition;
            PlayerLoadedRotation = save.PlayerRotation;
            AlreadyGenerated += _progressStep;
            yield return null; // give back control

            ProgressDescription = "Creating game objects...";
            if (firstRun)
                _worldScene = SceneManager.CreateScene(name);

            Blocks = save.Blocks;
            Status = WorldGeneratorStatus.TerrainReady;
            AlreadyGenerated += _progressStep;
            yield return null; // give back control

            if(firstRun)
            {
                ProgressDescription = "Chunk data initialization...";
                // chunkData need to be initialized earlier in order to allow main loop iterate over chunks before their meshes are ready
                // thanks to this we can display chunk as soon as they become ready 
                Chunks = new ChunkData[Settings.WorldSizeX, WorldSizeY, Settings.WorldSizeZ];
                for (int x = 0; x < Settings.WorldSizeX; x++)
                    for (int z = 0; z < Settings.WorldSizeZ; z++)
                        for (int y = 0; y < WorldSizeY; y++)
                            Chunks[x, y, z] = new ChunkData(
                                new Vector3Int(x, y, z), 
                                new Vector3Int(x * ChunkSize, y * ChunkSize, z * ChunkSize));

                ChunkObjects = new ChunkObject[Settings.WorldSizeX, WorldSizeY, Settings.WorldSizeZ];
                AlreadyGenerated += _progressStep;
                yield return null; // give back control
            }

            ProgressDescription = "Calculating faces...";
            _meshGenerator.CalculateFaces(ref Blocks);
            AlreadyGenerated += _progressStep;
            yield return null; // give back control

            ProgressDescription = "World boundaries check...";
            _meshGenerator.WorldBoundariesCheck(ref Blocks);
            Status = WorldGeneratorStatus.FacesReady;
            AlreadyGenerated += _progressStep;
            yield return null; // give back control

            ProgressDescription = "Creating chunks...";
            // create chunk objects one by one 
            for (int x = 0; x < Settings.WorldSizeX; x++)
                for (int z = 0; z < Settings.WorldSizeZ; z++)
                    for (int y = 0; y < WorldSizeY; y++)
                    {
                        ChunkData chunkData = Chunks[x, y, z];
                        _meshGenerator.CalculateMeshes(ref Blocks, chunkData.Position, out Mesh terrainMesh, out Mesh waterMesh);

                        if (firstRun)
                        {
                            var chunkObject = new ChunkObject();
                            CreateChunkGameObjects(ref chunkData, ref chunkObject, terrainMesh, waterMesh);
                            SceneManager.MoveGameObjectToScene(chunkObject.Terrain.gameObject, _worldScene);
                            SceneManager.MoveGameObjectToScene(chunkObject.Water.gameObject, _worldScene);
                            ChunkObjects[x, y, z] = chunkObject;
                        }
                        else
                        {
                            SetMeshesAndCollider(ref chunkData, ref ChunkObjects[x, y, z], terrainMesh, waterMesh);
                        }

                        Chunks[x, y, z].Status = ChunkStatus.NeedToBeRedrawn;
                        AlreadyGenerated++;

                        yield return null; // give back control
                    }

            Status = WorldGeneratorStatus.AllReady;

            _stopwatch.Stop();
            UnityEngine.Debug.Log($"It took {_stopwatch.ElapsedTicks / TimeSpan.TicksPerMillisecond} ms to generate all terrain.");

            ProgressDescription = "Generation Completed";
            callback.Invoke();
        }

        /// <summary>
        /// Returns true if the block has been destroyed.
        /// </summary>
        public bool BlockHit(int blockX, int blockY, int blockZ, ref ChunkData chunkData)
		{
			ref BlockData b = ref Blocks[blockX, blockY, blockZ];

            if(b.Type == BlockTypes.Air)
            {
                UnityEngine.Debug.LogError("Block of type Air was hit which should have never happened. Probably wrong block coordinates calculation.");
                return false;
            }

            if (--b.Hp == 0)
            {
                b.Type = BlockTypes.Air;
                _meshGenerator.RecalculateFacesAfterBlockDestroy(ref Blocks, blockX, blockY, blockZ);
                chunkData.Status = ChunkStatus.NeedToBeRecreated;
                return true;
            }

            byte previousHpLevel = b.HealthLevel;
            b.HealthLevel = CalculateHealthLevel(b.Hp, LookupTables.BlockHealthMax[(int)b.Type]);

			if (b.HealthLevel != previousHpLevel)
                chunkData.Status = ChunkStatus.NeedToBeRedrawn;

            return false;
		}

		/// <summary>
		/// Returns true if a new block has been built.
		/// </summary>
		public bool BuildBlock(int blockX, int blockY, int blockZ, BlockTypes type, ChunkData chunkData)
		{
			ref BlockData b = ref Blocks[blockX, blockY, blockZ];

			if (b.Type != BlockTypes.Air) return false;

			_meshGenerator.RecalculateFacesAfterBlockBuild(ref Blocks, blockX, blockY, blockZ);

			b.Type = type;
			b.Hp = LookupTables.BlockHealthMax[(int)type];
			b.HealthLevel = 0;

			chunkData.Status = ChunkStatus.NeedToBeRecreated;

			return true;
		}

		public void RedrawChunksIfNecessary()
		{
			for (int x = 0; x < Settings.WorldSizeX; x++)
				for (int z = 0; z < Settings.WorldSizeZ; z++)
					for (int y = 0; y < WorldSizeY; y++)
					{
						ref ChunkData chunkData = ref Chunks[x, y, z];

                        if (chunkData.Status == ChunkStatus.NotReady)
                            continue;
                        else if (chunkData.Status == ChunkStatus.NeedToBeRecreated)
                        {
                            _meshGenerator.CalculateMeshes(ref Blocks, chunkData.Position, out Mesh terrainMesh, out Mesh waterMesh);
                            SetMeshesAndCollider(ref chunkData, ref ChunkObjects[x, y, z], terrainMesh, waterMesh);
                        }
                        else if (chunkData.Status == ChunkStatus.NeedToBeRedrawn) // used only for cracks
                        {
                            _meshGenerator.CalculateMeshes(ref Blocks, chunkData.Position, out Mesh terrainMesh, out _);
                            SetTerrainMesh(ref chunkData, ref ChunkObjects[x, y, z], terrainMesh);
                        }
					}
		}

		void ResetProgressBarVariables()
		{
			// Unity editor remembers the state of the asset classes so these values have to reinitialized
			_progressStep = 1;
			AlreadyGenerated = 0;

			MeshProgressSteps = Settings.WorldSizeX * WorldSizeY * Settings.WorldSizeZ;

			while (OverallNumberOfGenerationSteps * _progressStep * 2f < MeshProgressSteps)
				_progressStep++;

			TerrainProgressSteps = OverallNumberOfGenerationSteps * _progressStep;
		}

		byte CalculateHealthLevel(int hp, int maxHp)
		{
			float proportion = (float)hp / maxHp; // 0.625f

			// TODO: this requires information from MeshGenerator which breaks the encapsulation rule
			float step = (float)1 / 11; // _crackUVs.Length; // 0.09f
			float value = proportion / step; // 6.94f
			int level = Mathf.RoundToInt(value); // 7

			return (byte)(11 - level); // array is in reverse order so we subtract our value from 11
		}

		void DeflattenizeOutput(ref BlockTypes[] types)
		{
			for (int x = 0; x < TotalBlockNumberX; x++)
				for (int y = 0; y < TotalBlockNumberY; y++)
					for (int z = 0; z < TotalBlockNumberZ; z++)
					{
						var type = types[Utils.IndexFlattenizer3D(x, y, z, TotalBlockNumberX, TotalBlockNumberY)];

						ref BlockData b = ref Blocks[x, y, z];
						b.Type = type;
						b.Hp = LookupTables.BlockHealthMax[(int)type];
					}
		}

        /// <summary>
        /// Sets both meshes and mesh mesh collider.
        /// </summary>
        void SetMeshesAndCollider(ref ChunkData chunkData, ref ChunkObject chunkObject, Mesh terrainMesh, Mesh waterMesh)
        {
            var terrainFilter = chunkObject.Terrain.GetComponent<MeshFilter>();
            terrainFilter.mesh = terrainMesh;
            chunkObject.Terrain.GetComponent<MeshCollider>().sharedMesh = terrainMesh;

            var waterFilter = chunkObject.Water.GetComponent<MeshFilter>();
            waterFilter.mesh = waterMesh;

            chunkData.Status = ChunkStatus.Ready;
        }

        /// <summary>
        /// Apply terrain mesh to the given chunk.
        /// </summary>
        void SetTerrainMesh(ref ChunkData chunkData, ref ChunkObject chunkObject, Mesh terrainMesh)
		{
            var meshFilter = chunkObject.Terrain.GetComponent<MeshFilter>();
			meshFilter.mesh = terrainMesh;

			chunkData.Status = ChunkStatus.Ready;
		}

        /// <summary>
        /// Creates chunk objects together with all necessary components.
        /// After that's done apply mesh and mesh collider.
        /// </summary>
		void CreateChunkGameObjects(ref ChunkData chunkData, ref ChunkObject chunkObject, Mesh terrainMesh, Mesh waterMesh)
		{
			string name = chunkData.Coord.x.ToString() + chunkData.Coord.y + chunkData.Coord.z;
			chunkObject.Terrain = new GameObject(name + "_terrain");
			chunkObject.Terrain.transform.position = chunkData.Position;
			chunkObject.Water = new GameObject(name + "_water");
			chunkObject.Water.transform.position = chunkData.Position;

            MeshRenderer mrT = chunkObject.Terrain.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            mrT.material = _terrainTexture;

            var mfT = (MeshFilter)chunkObject.Terrain.AddComponent(typeof(MeshFilter));
            mfT.mesh = terrainMesh;

            var mc = chunkObject.Terrain.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
            mc.sharedMesh = terrainMesh;

            var mrW = chunkObject.Water.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            mrW.material = _waterTexture;

            var mfW = (MeshFilter)chunkObject.Water.AddComponent(typeof(MeshFilter));
            mfW.mesh = waterMesh;
        }
	}
}