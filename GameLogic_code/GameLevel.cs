using System;
using UnityEngine;
public class GameLevel : MonoBehaviour
{
	public string musicName;
	public string levelName;
	public string defaultQuestName;
	public bool arena = false;
	protected int currentEnemies = 0;
	private void Awake()
	{
		GameState.isArena = this.arena;
	}
	private void Start()
	{
		LocalPlayerEvents.PlayMusic(this.musicName);
		if (Application.loadedLevelName == "Space")
		{
			Physics.gravity = new Vector3(0f, -1.63500011f, 0f);
		}
		else
		{
			Physics.gravity = new Vector3(0f, -9.81f, 0f);
		}
	}
	protected virtual void UpdateMusic()
	{
	}
	public virtual void OnEnemyTargeting()
	{
		this.currentEnemies++;
		this.UpdateMusic();
	}
	public virtual void OnEnemyLost()
	{
		this.currentEnemies--;
		this.UpdateMusic();
	}
	public virtual void StartBoss()
	{
	}
	public virtual void EndBoss()
	{
	}
}
