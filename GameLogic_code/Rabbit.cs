using System;
using UnityEngine;
public class Rabbit : Enemy
{
	private enum State
	{
		Dead,
		Idle,
		RandomWalk,
		RunToPlayer,
		Attack
	}
	public float walkSpeed = 3f;
	public float attackDamage = 4f;
	private Rabbit.State state;
	private float nextRandomWalk;
	private float attackTimeLeft;
	private bool attackHitPending = false;
	private float attackHitCountdown;
	private void Start()
	{
		this.state = Rabbit.State.Idle;
		this.nextRandomWalk = 0f;
	}
	private bool LookForPlayers()
	{
		GameObject gameObject = base.LookForPlayerTarget();
		if (gameObject != null)
		{
			this.state = Rabbit.State.RunToPlayer;
			base.target = gameObject;
			this.agent.destination = base.target.transform.position;
			this.agent.speed = this.walkSpeed;
			return true;
		}
		return false;
	}
	private void UpdateTargetedPlayer()
	{
		if (base.target == null || (base.target.GetComponent<Player>() != null && base.target.GetComponent<Player>().health <= 0) || (base.target.GetComponent<Enemy>() != null && base.target.GetComponent<Enemy>().health <= 0))
		{
			this.state = Rabbit.State.Idle;
			base.target = null;
			this.nextRandomWalk = UnityEngine.Random.Range(1f, 5f);
			return;
		}
		if (base.IsTargetStillVisible())
		{
			this.agent.destination = base.target.transform.position;
		}
		else
		{
			if (!this.agent.hasPath)
			{
				this.state = Rabbit.State.Idle;
				base.target = null;
				this.nextRandomWalk = UnityEngine.Random.Range(1f, 5f);
				return;
			}
		}
		if (base.distanceToTarget < this.meleeRange * 1.25f)
		{
			this.StartAttack();
		}
	}
	private void StartAttack()
	{
		this.agent.Stop();
		base.UpdateRandomState();
		base.SetState("attack", true);
		this.state = Rabbit.State.Attack;
		this.attackTimeLeft = 1f;
		this.attackHitPending = true;
		this.attackHitCountdown = 0.5f;
	}
	private void UpdateAttack()
	{
		this.attackTimeLeft -= Time.deltaTime;
		this.attackHitCountdown -= Time.deltaTime;
		base.RotateToTarget();
		if (this.attackHitPending && this.attackHitCountdown <= 0f)
		{
			this.attackHitPending = false;
			base.TryToMeleeTarget(this.attackDamage);
			base.SetState("attack", false);
		}
		if (this.attackTimeLeft <= 0f)
		{
			this.state = Rabbit.State.RunToPlayer;
			if (base.target != null)
			{
				this.agent.destination = base.target.transform.position;
			}
			this.agent.speed = this.walkSpeed;
		}
	}
	private void StartRandomWalk()
	{
		this.state = Rabbit.State.RandomWalk;
		this.agent.destination = base.transform.position + new Vector3(UnityEngine.Random.Range(-30f, 30f), 4f, UnityEngine.Random.Range(-30f, 30f));
		this.agent.speed = this.walkSpeed;
	}
	private void UpdateRandomWalk()
	{
		if (!this.agent.hasPath)
		{
			this.state = Rabbit.State.Idle;
			this.nextRandomWalk = UnityEngine.Random.Range(1f, 5f);
		}
	}
	public override void UpdateAI()
	{
		base.SetState("walk", this.agent.velocity.magnitude > this.walkSpeed * 0.25f);
		switch (this.state)
		{
		case Rabbit.State.Idle:
			if (!this.LookForPlayers())
			{
				this.nextRandomWalk -= Time.deltaTime;
				if (this.nextRandomWalk <= 0f)
				{
					this.StartRandomWalk();
				}
			}
			break;
		case Rabbit.State.RandomWalk:
			if (!this.LookForPlayers())
			{
				this.UpdateRandomWalk();
			}
			break;
		case Rabbit.State.RunToPlayer:
			this.UpdateTargetedPlayer();
			break;
		case Rabbit.State.Attack:
			this.UpdateAttack();
			break;
		}
	}
	public override void Die(GameObject killer)
	{
		base.UpdateRandomState();
		base.SetState("dead", true);
		this.state = Rabbit.State.Dead;
		base.Die(killer);
	}
	public override void Alert(GameObject player, Vector3 position)
	{
		if (base.target == player)
		{
			return;
		}
		this.state = Rabbit.State.RunToPlayer;
		base.target = player;
		this.agent.destination = position;
		this.agent.speed = this.walkSpeed;
	}
}
