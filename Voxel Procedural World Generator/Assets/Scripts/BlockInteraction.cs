﻿using UnityEngine;

public class BlockInteraction : MonoBehaviour
{
	const float AttackRange = 3.0f;

    [SerializeField] Game _game;
	[SerializeField] AudioClip _stonehitSound;
	[SerializeField] Camera _weaponCamera;

	AudioSource _audioSource;
	BlockTypes _buildBlockType = BlockTypes.Stone;

	void Start() => _audioSource = GetComponent<AudioSource>();

	void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			HitBlock();
		}

		if (Input.GetMouseButtonDown(1))
		{
			BuildBlock();
		}
	}

	void HitBlock()
	{
		// Bit shift the index of the layer (8) to get a bit mask
		int layerMask = 1 << 8;

		// This would cast rays only against colliders in layer 8.
		// But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
		layerMask = ~layerMask;

		// Does the ray intersect any objects excluding the player layer
		if (!Physics.Raycast(
            _weaponCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0)),
			_weaponCamera.transform.forward, 
            out RaycastHit hit, 
            AttackRange, 
            layerMask))
			return;

		Vector3 hitBlock = hit.point - hit.normal / 2.0f; // central point

        // x and z for some reason lose 0.5 each so we have to add it manually
        hitBlock.x += 0.5f;
        hitBlock.z += 0.5f;

		_audioSource.PlayOneShot(_stonehitSound);
		_game.ProcessBlockHit(hitBlock);
	}

	void BuildBlock()
	{
		// Bit shift the index of the layer (8) to get a bit mask
		int layerMask = 1 << 8;

		// This would cast rays only against colliders in layer 8.
		// But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
		layerMask = ~layerMask;

		// Does the ray intersect any objects excluding the player layer
		if (!Physics.Raycast(_weaponCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0)),
			_weaponCamera.transform.forward, out RaycastHit hit, AttackRange, layerMask))
			return;

		Vector3 hitBlock = hit.point + hit.normal / 2.0f; // next to the one that we are pointing at

		_audioSource.PlayOneShot(_stonehitSound);
		_game.ProcessBuildBlock(hitBlock, _buildBlockType);
	}

	void CheckForBuildBlockType()
	{
		if (Input.GetKeyDown("1"))
		{
			_buildBlockType = BlockTypes.Grass;
			Debug.Log("Change build block type to Grass");
		}
		else if (Input.GetKeyDown("2"))
		{
			_buildBlockType = BlockTypes.Dirt;
			Debug.Log("Change build block type to Dirt");
		}
		else if (Input.GetKeyDown("3"))
		{
			_buildBlockType = BlockTypes.Stone;
			Debug.Log("Change build block type to Stone");
		}
		else if (Input.GetKeyDown("4"))
		{
			_buildBlockType = BlockTypes.Diamond;
			Debug.Log("Change build block type to Diamond");
		}
		else if (Input.GetKeyDown("5"))
		{
			_buildBlockType = BlockTypes.Bedrock;
			Debug.Log("Change build block type to Bedrock");
		}
		else if (Input.GetKeyDown("6"))
		{
			_buildBlockType = BlockTypes.Redstone;
			Debug.Log("Change build block type to Redstone");
		}
		else if (Input.GetKeyDown("7"))
		{
			_buildBlockType = BlockTypes.Sand;
			Debug.Log("Change build block type to Sand");
		}
		else if (Input.GetKeyDown("8"))
		{
			_buildBlockType = BlockTypes.Water;
			Debug.Log("Change build block type to Water");
		}
	}
}
