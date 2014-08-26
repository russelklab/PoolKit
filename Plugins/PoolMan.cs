using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public partial class PoolMan : MonoBehaviour 
{
	// acces to singleton
	public static PoolMan instance;

	// stores a collection of 
	public List<PoolCollection> poolCollection;

	// uses the GameObject instanceId as its key for fast look-ups
	private Dictionary<int,PoolCollection> _instanceIdToPoolCollection = new Dictionary<int, PoolCollection> ();

	// use the pool name to find the instance
	private Dictionary<string,int> _poolNameToInstanceId = new Dictionary<string,int>();

	[HideInInspector]
	public new Transform transform;

	void Awake ()
	{
		if (instance != null) {
			Destroy (gameObject);
		} else {
			transform = gameObject.transform;
			instance = this;
			initializePrefabPools ();
		}

		StartCoroutine("CullExcessObjects");
	}

	void OnApplicationQuit ()
	{
		instance = null;
	}
	
	// populats the lookup dictionaries
	private void initializePrefabPools()
	{
		if (poolCollection == null) {
			return;
		}


		for (int n = 0; n < poolCollection.Count; n++) {
			if (poolCollection[n] == null || poolCollection[n].prefab == null) {
				continue;
			}

			poolCollection[n].Initialize ();
			_instanceIdToPoolCollection.Add(poolCollection[n].prefab.GetInstanceID (), poolCollection[n]);
			_poolNameToInstanceId.Add(poolCollection[n].prefab.name, poolCollection[n].prefab.GetInstanceID ());
		}
	}

	private IEnumerator CullExcessObjects()
	{
		var waiter = new WaitForSeconds(5f);
		
		while(true) {
			for(int i = 0; i < poolCollection.Count; i++) {
				poolCollection[i].CullExcessObjects();
			}
			
			yield return waiter;
		}
	}

	// grab the item from the pool and return it
	private static GameObject Spawn (int gameObjectInstanceId, Vector3 position, Quaternion rotation)
	{
		if (instance._instanceIdToPoolCollection.ContainsKey(gameObjectInstanceId)) {
			GameObject go = instance._instanceIdToPoolCollection[gameObjectInstanceId].Spawn();
			
			if(go != null) {
				Transform trans = go.transform;
				trans.parent = null;
				trans.position = position;
				trans.rotation = rotation;
				
				go.SetActive(true);
			}
			
			return go;
		}
		
		return null;
	}
	
	// pulls an object out of the recycle bin
	public static GameObject Spawn(GameObject go, Vector3 position = default(Vector3), Quaternion rotation = default(Quaternion)) {
		if( instance._instanceIdToPoolCollection.ContainsKey(go.GetInstanceID())) {
			return Spawn(go.GetInstanceID(), position, rotation);
		} else {
			Debug.LogError("attempted to spawn go (" + go.name + ") but there is no recycle bin setup for it. Falling back to Instantiate");
			GameObject newGo = GameObject.Instantiate(go, position, rotation) as GameObject;
			newGo.transform.parent = null;
			
			return newGo;
		}
	}
	
	// pulls an object out of the recycle bin using the bin's name
	public static GameObject Spawn(string gameObjectName, Vector3 position = default(Vector3), Quaternion rotation = default(Quaternion))
	{
		int instanceId = -1;
		if(instance._poolNameToInstanceId.TryGetValue(gameObjectName, out instanceId)) {
			return Spawn(instanceId, position, rotation);
		} else {
			Debug.LogError( "attempted to spawn a GameObject from recycle bin (" + gameObjectName + ") but there is no recycle bin setup for it" );
			return null;
		}
	}
	
	// sticks the GameObject back into it's recycle bin. If the GameObject has no bin it is destroyed.
	public static void Despawn(GameObject go)
	{
		if(go == null) {
			return;
		}
		
		string goName = go.name;
		if(!instance._poolNameToInstanceId.ContainsKey(goName)) {
			Destroy(go);
		} else {
			instance._instanceIdToPoolCollection[instance._poolNameToInstanceId[goName]].Despawn(go);
			go.transform.parent = instance.transform;
		}
	}

	private IEnumerator InternalDespawnAfterDelay(GameObject go, float delayInSeconds)
	{
		yield return new WaitForSeconds(delayInSeconds);
		Despawn(go);
	}

	public static void ManagePoolCollection(PoolCollection poolCollection) {
		// make sure we can safely add the bin!
		if (instance._poolNameToInstanceId.ContainsKey(poolCollection.prefab.name)) {
			Debug.LogError( "Cannot manage the pool because there is already a GameObject with the name (" + poolCollection.prefab.name + ") being managed" );
			return;
		}
		
		instance.poolCollection.Add(poolCollection);
		poolCollection.Initialize();
		instance._instanceIdToPoolCollection.Add(poolCollection.prefab.GetInstanceID(), poolCollection);
		instance._poolNameToInstanceId.Add(poolCollection.prefab.name, poolCollection.prefab.GetInstanceID());
	}
	
	// sticks the GameObject back into it's recycle bin after a delay. If the GameObject has no bin it is destroyed.
	public static void DespawnAfterDelay(GameObject go, float delayInSeconds)
	{
		if(go == null) {
			return;
		}
		
		instance.StartCoroutine(instance.InternalDespawnAfterDelay(go, delayInSeconds));
	}

	// gets the recycle bin for the given GameObject name. Returns null if none exists.
	public static PoolCollection PoolCollectionForGameObjectName(string gameObjectName)
	{
		if (instance._poolNameToInstanceId.ContainsKey(gameObjectName)) {
			var instanceId = instance._poolNameToInstanceId[gameObjectName];
			return instance._instanceIdToPoolCollection[instanceId];
		}

		return null;
	}

	public static PoolCollection PoolCollectionForGameObject(GameObject go)
	{
		if( instance._instanceIdToPoolCollection.ContainsKey(go.GetInstanceID())) {
			return instance._instanceIdToPoolCollection[go.GetInstanceID()];
		}

		return null;
	}
}
