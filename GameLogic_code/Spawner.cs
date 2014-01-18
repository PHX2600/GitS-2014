using System;
using System.Collections.Generic;
using UnityEngine;
public class Spawner : MonoBehaviour
{
	public GameObject prefab;
	public int maximumInstances = 3;
	public float spawnTimer = 60f;
	public float earlySpawnTimer = 8f;
	private int earlyInstances;
	private float timeUntilSpawn = 0f;
	private List<GameObject> spawnedObjects = new List<GameObject>();
	private void Start()
	{
		this.earlyInstances = this.maximumInstances - 1;
	}
	private void Update()
	{
		if (!GameState.isServer)
		{
			return;
		}
		if (this.spawnedObjects.Count >= this.maximumInstances)
		{
			return;
		}
		this.timeUntilSpawn -= Time.deltaTime;
		if (this.timeUntilSpawn < 0f)
		{
			if (this.earlyInstances > 0)
			{
				this.timeUntilSpawn = this.earlySpawnTimer;
				this.earlyInstances--;
			}
			else
			{
				this.timeUntilSpawn = this.spawnTimer;
			}
			GameObject gameObject = (GameObject)UnityEngine.Object.Instantiate(this.prefab, base.gameObject.transform.position, base.gameObject.transform.rotation);
			gameObject.GetComponent<SpawnableObject>().spawner = this;
			GameState.instance.AddServerReplicatedObject(gameObject.GetComponent<Enemy>().enemyName, gameObject);
			this.spawnedObjects.Add(gameObject);
		}
	}
	public void RemoveInstance(GameObject obj)
	{
		this.spawnedObjects.Remove(obj);
	}
}
