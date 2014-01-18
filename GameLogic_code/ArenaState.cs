using System;
using System.Collections.Generic;
using UnityEngine;
public class ArenaState : MonoBehaviour
{
	public struct ArenaTeam
	{
		public int index;
		public List<Player> players;
		public int score;
	}
	public int goalScore = 100;
	public int maxPlayersPerTeam = 8;
	protected ArenaState.ArenaTeam[] teams;
	protected float gameRestartCountdown = 15f;
	public static ArenaState instance
	{
		get
		{
			return (ArenaState)UnityEngine.Object.FindObjectOfType(typeof(ArenaState));
		}
	}
	private void Awake()
	{
		this.teams = new ArenaState.ArenaTeam[2];
		this.teams[0].index = 0;
		this.teams[0].players = new List<Player>();
		this.teams[0].score = 0;
		this.teams[1].index = 1;
		this.teams[1].players = new List<Player>();
		this.teams[1].score = 0;
	}
	public ArenaState.ArenaTeam GetTeamByIndex(int i)
	{
		return this.teams[i];
	}
	protected int PickTeamForPlayer(Player player)
	{
		if (this.teams[0].players.Count >= this.maxPlayersPerTeam && this.teams[1].players.Count >= this.maxPlayersPerTeam)
		{
			if (this.teams[0].players.Count < this.teams[1].players.Count)
			{
				return 0;
			}
			return 1;
		}
		else
		{
			if (this.teams[0].players.Count >= this.maxPlayersPerTeam)
			{
				return 1;
			}
			if (this.teams[1].players.Count >= this.maxPlayersPerTeam)
			{
				return 0;
			}
			if (this.teams[0].players.Count - this.teams[1].players.Count >= 2)
			{
				return 1;
			}
			if (this.teams[1].players.Count - this.teams[0].players.Count >= 2)
			{
				return 0;
			}
			foreach (Player current in this.teams[0].players)
			{
				if (current.team == player.team)
				{
					int result = 0;
					return result;
				}
			}
			foreach (Player current2 in this.teams[1].players)
			{
				if (current2.team == player.team)
				{
					int result = 1;
					return result;
				}
			}
			if (this.teams[0].players.Count > this.teams[1].players.Count)
			{
				return 1;
			}
			return 0;
		}
	}
	public int AddPlayer(Player player)
	{
		int num = this.PickTeamForPlayer(player);
		this.teams[num].players.Add(player);
		return num;
	}
	public void RemovePlayer(Player player)
	{
		if (this.teams[0].players.Contains(player))
		{
			this.teams[0].players.Remove(player);
		}
		if (this.teams[1].players.Contains(player))
		{
			this.teams[1].players.Remove(player);
		}
	}
	public void Suicide(Player player)
	{
		Player.SendUpdateToAllPlayers(GameServerUpdate.CreateArenaTeamScoreUpdate(player.arenaTeamIndex, this.teams[player.arenaTeamIndex].score));
	}
	public void ScoreKill(Player player)
	{
		ArenaState.ArenaTeam[] expr_11_cp_0 = this.teams;
		int expr_11_cp_1 = player.arenaTeamIndex;
		expr_11_cp_0[expr_11_cp_1].score = expr_11_cp_0[expr_11_cp_1].score + 1;
		Player.SendUpdateToAllPlayers(GameServerUpdate.CreateArenaTeamScoreUpdate(player.arenaTeamIndex, this.teams[player.arenaTeamIndex].score));
	}
	public bool IsGameComplete()
	{
		return this.teams[0].score >= this.goalScore || this.teams[1].score >= this.goalScore;
	}
	public int GetTeamScore(int team)
	{
		return this.teams[team].score;
	}
	public void SetTeamScore(int team, int score)
	{
		this.teams[team].score = score;
	}
	private void Update()
	{
		if (!GameState.isServer)
		{
			return;
		}
		if (this.IsGameComplete())
		{
			this.gameRestartCountdown -= Time.deltaTime;
			if (this.gameRestartCountdown <= 0f)
			{
				for (int i = 0; i < 2; i++)
				{
					this.teams[i].score = 0;
					foreach (Player current in this.teams[i].players)
					{
						current.ResetArenaGame();
					}
				}
				Player.SendUpdateToAllPlayers(GameServerUpdate.CreateArenaTeamScoreUpdate(0, 0));
				Player.SendUpdateToAllPlayers(GameServerUpdate.CreateArenaTeamScoreUpdate(1, 0));
				this.gameRestartCountdown = 15f;
			}
		}
	}
}
