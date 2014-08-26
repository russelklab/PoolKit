using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(PoolMan))]
public class PoolManEditor : Editor 
{
	private List<bool> _prefabFoldouts;
	private PoolMan _poolManTarget;

	public void OnEnable()
	{
		_poolManTarget = target as PoolMan;
		_poolManTarget.poolCollection = (target as PoolMan).poolCollection;
		
		_prefabFoldouts = new List<bool>();

		if(_poolManTarget.poolCollection != null) {
			for(int n = 0; n < _poolManTarget.poolCollection.Count; n++) {
				_prefabFoldouts.Add( true );
			}
		}
		
		clearNullReferences();
	}

	private void clearNullReferences()
	{
		if( _poolManTarget.poolCollection == null )
			return;
		
		int n = 0;
		while( n < _poolManTarget.poolCollection.Count )
		{
			if(_poolManTarget.poolCollection[n].prefab == null) {
				_poolManTarget.poolCollection.RemoveAt(_poolManTarget.poolCollection.Count - 1);
			} else {
				n++;
			}
		}
	}

	private void addRecycleBin( GameObject go )
	{
		if(_poolManTarget.poolCollection == null) {
			_poolManTarget.poolCollection = new List<PoolCollection>();
		}
		
		if(_poolManTarget.poolCollection != null) {
			foreach( var recycleBin in _poolManTarget.poolCollection )
			{
				if( recycleBin.prefab.gameObject.name == go.name )
				{
					EditorUtility.DisplayDialog( "Pool Man", "Pool Man already manages a GameObject with the name '" + go.name + "'.\n\nIf you are attempting to manage multiple GameObjects sharing the same name, you will need to first give them unique names.", "OK" );
					return;
				}
			}
		}
		
		PoolCollection newPrefabPool = new PoolCollection();
		newPrefabPool.prefab = go;
		
		_poolManTarget.poolCollection.Add(newPrefabPool);
		while(_poolManTarget.poolCollection.Count > _prefabFoldouts.Count) {
			_prefabFoldouts.Add(false);
		}
	}

	public override void OnInspectorGUI()
	{
		if(Application.isPlaying) {
			if( _prefabFoldouts.Count < _poolManTarget.poolCollection.Count) {
				for(var i = 0; i < _poolManTarget.poolCollection.Count - _prefabFoldouts.Count; i++)
					_prefabFoldouts.Add( false );
			}
			//base.OnInspectorGUI();
			//return;
		}
		
		GUILayout.Space( 15f );
		dropAreaGUI();
		
		if (_poolManTarget.poolCollection == null) {
			return;
		}
		
		GUILayout.Space( 5f );
		GUILayout.Label("Pools", EditorStyles.boldLabel);
		
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.BeginVertical();
		
		for(int n = 0; n < _poolManTarget.poolCollection.Count; n++) {
			PoolCollection prefabPool = _poolManTarget.poolCollection[n];
			
			// wrapper vertical allows us to style each element
			EditorGUILayout.BeginVertical( n % 2 == 0 ? "box" : "button" );
			
			// PrefabPool DropDown
			EditorGUILayout.BeginHorizontal();
			_prefabFoldouts[n] = EditorGUILayout.Foldout( _prefabFoldouts[n], prefabPool.prefab.name, EditorStyles.foldout );
			if( GUILayout.Button( "-", GUILayout.Width( 20f ) ) && EditorUtility.DisplayDialog( "Remove Collection", "Are you sure you want to remove this pool?", "Yes", "Cancel" ) )
				_poolManTarget.poolCollection.RemoveAt(_poolManTarget.poolCollection.Count - 1);
			EditorGUILayout.EndHorizontal();
			
			if( _prefabFoldouts[n] )
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space( 10f );
				EditorGUILayout.BeginVertical();
				
				// PreAlloc
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label( new GUIContent( "Preallocate Count", "Total items to create at scene start" ), EditorStyles.label, GUILayout.Width( 115f ) );
				prefabPool.gameObjectToPreAllocate = EditorGUILayout.IntField( prefabPool.gameObjectToPreAllocate );
				if(prefabPool.gameObjectToPreAllocate < 0)
					prefabPool.gameObjectToPreAllocate = 0;
				EditorGUILayout.EndHorizontal();
				
				// AllocBlock
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label( new GUIContent( "Allocate Block Count", "Once the bin limit is reached, this is how many new objects will be created as necessary" ), EditorStyles.label, GUILayout.Width( 115f ) );
				prefabPool.gameObjectToAllocateWhenEmpty = EditorGUILayout.IntField( prefabPool.gameObjectToAllocateWhenEmpty );
				if( prefabPool.gameObjectToAllocateWhenEmpty < 1 )
					prefabPool.gameObjectToAllocateWhenEmpty = 1;
				EditorGUILayout.EndHorizontal();
				
				// HardLimit
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label( new GUIContent( "Enable Hard Limit ", "If true, the bin will return null if a new item is requested and the Limit was reached" ), EditorStyles.label, GUILayout.Width( 115f ) );
				prefabPool.imposeHardLimit = EditorGUILayout.Toggle( prefabPool.imposeHardLimit );
				EditorGUILayout.EndHorizontal();
				
				if( prefabPool.imposeHardLimit )
				{				
					EditorGUILayout.BeginHorizontal();
					GUILayout.Space( 20f );
					GUILayout.Label( new GUIContent( "Limit", "Max number of items allowed in the bin when Hard Limit is true" ), EditorStyles.label, GUILayout.Width( 100f ) );
					prefabPool.hardLimit = EditorGUILayout.IntField( prefabPool.hardLimit );
					if( prefabPool.hardLimit < 1 )
						prefabPool.hardLimit = 1;
					EditorGUILayout.EndHorizontal();
				}
				
				
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label( new GUIContent( "Enable Culling", "If true, items in excess of Cull Above will be destroyed automatically" ), EditorStyles.label, GUILayout.Width( 115f ) );
				prefabPool.cullExcessPrefabs = EditorGUILayout.Toggle( prefabPool.cullExcessPrefabs );
				EditorGUILayout.EndHorizontal();
				
				
				if( prefabPool.cullExcessPrefabs )
				{
					EditorGUILayout.BeginHorizontal();
					GUILayout.Space( 20f );
					GUILayout.Label( new GUIContent( "Cull Above", "Max number of items to allow. If item count exceeds this they will be culled" ), EditorStyles.label, GUILayout.Width( 100f ) );
					prefabPool.gameObjectsToBeMaintained = EditorGUILayout.IntField( prefabPool.gameObjectsToBeMaintained );
					if( prefabPool.gameObjectsToBeMaintained < 0 )
						prefabPool.gameObjectsToBeMaintained = 0;
					if( prefabPool.imposeHardLimit && prefabPool.gameObjectsToBeMaintained > prefabPool.hardLimit )
						prefabPool.gameObjectsToBeMaintained = prefabPool.hardLimit;
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					GUILayout.Space( 20f );
					GUILayout.Label( new GUIContent( "Cull Delay", "Duration in seconds between cull checks. Note that the master cull check only occurs every 5 seconds" ), EditorStyles.label, GUILayout.Width( 100f ) );
					prefabPool.cullInterval = EditorGUILayout.FloatField( prefabPool.cullInterval );
					if( prefabPool.cullInterval < 0 )
						prefabPool.cullInterval = 0;
					EditorGUILayout.EndHorizontal();
				}
				
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();
			}
			
			
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
		}
		
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();
		
		if( GUI.changed )
			EditorUtility.SetDirty( target );
	}

	private void dropAreaGUI()
	{
		var evt = Event.current;
		var dropArea = GUILayoutUtility.GetRect(0f, 60f, GUILayout.ExpandWidth(true));
		GUI.Box( dropArea, "Drop a Prefab or GameObject here to create a new collection in your PoolMan" );
		
		switch( evt.type )
		{
		case EventType.DragUpdated:
		case EventType.DragPerform:
		{
			if( !dropArea.Contains( evt.mousePosition ) )
				break;
			
			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
			
			if( evt.type == EventType.DragPerform )
			{
				DragAndDrop.AcceptDrag();
				foreach( var draggedObject in DragAndDrop.objectReferences )
				{
					var go = draggedObject as GameObject;
					if( !go )
						continue;
					
					// TODO: perhaps we should only allow prefabs or perhaps allow GO's in the scene as well?
					// uncomment to allow only prefabs
					//						if( PrefabUtility.GetPrefabType( go ) == PrefabType.None )
					//						{
					//							EditorUtility.DisplayDialog( "Trash Man", "Trash Man cannot manage the object '" + go.name + "' as it is not a prefab.", "OK" );
					//							continue;
					//						}
					
					addRecycleBin( go );
				}
			}
			
			Event.current.Use();
			break;
		} // end DragPerform
		} // end switch
	}
}
