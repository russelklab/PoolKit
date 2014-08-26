using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[System.Serializable]
public sealed class PoolCollection 
{
	// fired when gameobject is spawned
	public event Action<GameObject> onSpawnedEvent;

	// fired when gameobject is despawned
	public event Action<GameObject> onDespawnedEvent;

	// prefab managed by this class
	public GameObject prefab;

	// total number of instance to create on start
	public int gameObjectToPreAllocate = 5;

	// total number of instance when pool is empty
	public int gameObjectToAllocateWhenEmpty = 1;

	// if true instance will not exceed the hard limit
	public bool imposeHardLimit = false;

	// if hard limit is true, limit the number of instances
	public int hardLimit = 5;

	// if true, excess prefabs will be culled at intervals
	public bool cullExcessPrefabs = false;
	
	// total of instance to keep in pool
	public int gameObjectsToBeMaintained = 5;

	// culling interval
	public float cullInterval = 10;

	// stores all the gameobject
	private Stack<GameObject> _gameObjectPool;
	
	// last time culling happend
	private float _timeOfLastCull = float.MinValue;

	// keep track of total instance spawned
	private int _spawnedInstanceCount = 0;


	// allocate the pool with gameobjects
	private void AllocateGameObjects (int count)
	{
		if (imposeHardLimit && _gameObjectPool.Count + count > hardLimit) {
			count = hardLimit - _gameObjectPool.Count;
		}

		for (int n = 0; n < count; n++) {
			GameObject go = GameObject.Instantiate(prefab.gameObject) as GameObject;
			go.name = prefab.name;
			go.transform.parent = PoolMan.instance.transform;
			go.SetActive(false);
			_gameObjectPool.Push(go);
		}
	}

	// pops an object off the stack
	private GameObject Pop ()
	{
		if (imposeHardLimit && _spawnedInstanceCount >= hardLimit) {
			return null;
		}

		if(_gameObjectPool.Count > 0) {
			_spawnedInstanceCount++;
			return _gameObjectPool.Pop();
		}
		
		AllocateGameObjects(gameObjectToAllocateWhenEmpty);

		return Pop();
	}

	public void Initialize ()
	{
		_gameObjectPool = new Stack<GameObject> (gameObjectToPreAllocate);
		AllocateGameObjects(gameObjectToPreAllocate);
	}

	public void CullExcessObjects ()
	{
		if(!cullExcessPrefabs || _gameObjectPool.Count <= gameObjectsToBeMaintained) {
			return;
		}
		
		if( Time.time > _timeOfLastCull + cullInterval ) {
			_timeOfLastCull = Time.time;

			for(int n = gameObjectsToBeMaintained; n <= _gameObjectPool.Count; n++) {
				GameObject.Destroy(_gameObjectPool.Pop());
			}
		}
	}

	// fetches a new instance from the pool
	public GameObject Spawn ()
	{
		GameObject go = Pop ();
		
		if(go != null) {
			if(onSpawnedEvent != null) {
				onSpawnedEvent( go );
			}
		}
		
		return go;
	}
	
	// returns an instance to the recycle bin
	public void Despawn (GameObject go)
	{
		go.SetActive(false);
		
		_spawnedInstanceCount--;
		_gameObjectPool.Push( go );
		
		if (onDespawnedEvent != null) {
			onDespawnedEvent(go);
		}
	}
}
