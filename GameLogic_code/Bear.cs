using System;
using UnityEngine;
public class Bear : Enemy
{
	private enum State
	{
		Dead,
		Idle,
		RandomWalk,
		RunToPlayer,
		Attack4Legs,
		Attack2Legs,
		StandUp,
		Drop,
		Arming,
		Armed
	}
	public float attackRunSpeed = 6f;
	public float walkSpeed = 2f;
	public float attackDamage = 40f;
	public bool twoLegAttack = true;
	public bool randomWalk = true;
	public bool allowArm = true;
	public float gunDamage = 8f;
	public float gunAttackTime = 0.5f;
	public GameObject gunModel;
	private Bear.State state;
	private float nextRandomWalk;
	private float attackTimeLeft;
	private bool attackHitPending = false;
	private float attackHitCountdown;
	private bool armed = false;
	private GameObject gunInstance = null;
	private void Start()
	{
		this.state = Bear.State.Idle;
		this.nextRandomWalk = 0f;
	}
	private bool LookForPlayers()
	{
		GameObject gameObject = base.LookForPlayerTarget();
		if (gameObject != null)
		{
			this.state = Bear.State.RunToPlayer;
			base.target = gameObject;
			this.agent.destination = base.target.transform.position;
			this.agent.speed = this.attackRunSpeed;
			return true;
		}
		return false;
	}
	private void UpdateTargetedPlayer()
	{
		if (base.target == null || (base.target.GetComponent<Player>() != null && base.target.GetComponent<Player>().health <= 0) || (base.target.GetComponent<Enemy>() != null && base.target.GetComponent<Enemy>().health <= 0))
		{
			this.state = Bear.State.Idle;
			base.target = null;
			base.SetState("attack", false);
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
				this.state = Bear.State.Idle;
				base.target = null;
				base.SetState("attack", false);
				this.randomWalk = true;
				this.nextRandomWalk = UnityEngine.Random.Range(1f, 5f);
				return;
			}
		}
		if (base.distanceToTarget < this.meleeRange * 1.25f)
		{
			this.Start4LegsAttack();
		}
		if (this.armed && base.distanceToTarget < 20f)
		{
			this.StartArming();
		}
	}
	private void Start4LegsAttack()
	{
		this.agent.Stop();
		if (UnityEngine.Random.Range(0, (!this.twoLegAttack) ? 3 : 4) == 3)
		{
			base.SetState("stand", true);
			this.state = Bear.State.StandUp;
			this.attackTimeLeft = 1f;
		}
		else
		{
			base.UpdateRandomState();
			base.SetState("attack", true);
			this.state = Bear.State.Attack4Legs;
			this.attackTimeLeft = 1f;
			this.attackHitPending = true;
			this.attackHitCountdown = 0.5f;
		}
	}
	private void UpdateAttack4Legs()
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
			this.state = Bear.State.RunToPlayer;
			if (base.target != null)
			{
				this.agent.destination = base.target.transform.position;
			}
			this.agent.speed = this.attackRunSpeed;
		}
	}
	private void Start2LegsAttack()
	{
		base.UpdateRandomState();
		base.SetState("attack", true);
		this.state = Bear.State.Attack2Legs;
		this.attackTimeLeft = 1f;
		this.attackHitPending = true;
		this.attackHitCountdown = 0.5f;
	}
	private void UpdateAttack2Legs()
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
			if (base.distanceToTarget >= 3f || UnityEngine.Random.Range(0, 4) == 0)
			{
				this.state = Bear.State.Drop;
				base.SetState("stand", false);
				this.attackTimeLeft = 1f;
				return;
			}
			this.Start2LegsAttack();
		}
	}
	private void UpdateStandUp()
	{
		this.attackTimeLeft -= Time.deltaTime;
		if (this.attackTimeLeft <= 0f)
		{
			this.Start2LegsAttack();
		}
	}
	private void UpdateDrop()
	{
		this.attackTimeLeft -= Time.deltaTime;
		if (this.attackTimeLeft <= 0f)
		{
			this.state = Bear.State.RunToPlayer;
			if (base.target != null)
			{
				this.agent.destination = base.target.transform.position;
			}
			this.agent.speed = this.attackRunSpeed;
		}
	}
	private void StartRandomWalk()
	{
		this.state = Bear.State.RandomWalk;
		this.agent.destination = base.transform.position + new Vector3(UnityEngine.Random.Range(-30f, 30f), 4f, UnityEngine.Random.Range(-30f, 30f));
		this.agent.speed = this.walkSpeed;
	}
	private void UpdateRandomWalk()
	{
		if (!this.agent.hasPath)
		{
			this.state = Bear.State.Idle;
			this.nextRandomWalk = UnityEngine.Random.Range(1f, 5f);
		}
	}
	private void StartArming()
	{
		this.state = Bear.State.Arming;
		base.SetState("stand", true);
		this.attackTimeLeft = 1f;
	}
	private void UpdateArming()
	{
		this.attackTimeLeft -= Time.deltaTime;
		base.RotateToTarget();
		if (this.attackTimeLeft <= 0f)
		{
			if (this.armed)
			{
				this.StartShooting();
			}
			else
			{
				this.state = Bear.State.Drop;
				base.SetState("stand", false);
				this.attackTimeLeft = 1f;
			}
		}
	}
	public void StartShooting()
	{
		this.state = Bear.State.Armed;
		base.SetState("armed", true);
		this.attackTimeLeft = UnityEngine.Random.Range(0.5f, 1.5f);
	}
	public void UpdateShooting()
	{
		this.attackTimeLeft -= Time.deltaTime;
		base.RotateToTarget();
		if (this.attackTimeLeft <= 0f)
		{
			if (base.target == null)
			{
				this.state = Bear.State.Drop;
				base.SetState("stand", false);
				base.SetState("armed", false);
				this.attackTimeLeft = 1f;
				return;
			}
			Player component = base.target.GetComponent<Player>();
			if (component == null)
			{
				this.state = Bear.State.Drop;
				base.SetState("stand", false);
				base.SetState("armed", false);
				this.attackTimeLeft = 1f;
				return;
			}
			component.Damage(new Damage(base.gameObject, null, this.gunDamage));
			Player.SendUpdateToAllPlayers(GameServerUpdate.CreateObjectStateUpdate(base.GetComponent<ReplicatedObject>(), "fire"));
			this.attackTimeLeft = UnityEngine.Random.Range(this.gunAttackTime * 0.75f, this.gunAttackTime * 1.25f);
		}
	}
	public override void UpdateAI()
	{
		base.SetState("walk", this.agent.velocity.magnitude > this.walkSpeed * 0.25f);
		base.SetState("run", this.agent.velocity.magnitude > this.walkSpeed * 1.25f);
		switch (this.state)
		{
		case Bear.State.Idle:
			if (!this.LookForPlayers() && this.randomWalk)
			{
				this.nextRandomWalk -= Time.deltaTime;
				if (this.nextRandomWalk <= 0f)
				{
					this.StartRandomWalk();
				}
			}
			break;
		case Bear.State.RandomWalk:
			if (!this.LookForPlayers())
			{
				this.UpdateRandomWalk();
			}
			break;
		case Bear.State.RunToPlayer:
			this.UpdateTargetedPlayer();
			break;
		case Bear.State.Attack4Legs:
			this.UpdateAttack4Legs();
			break;
		case Bear.State.Attack2Legs:
			this.UpdateAttack2Legs();
			break;
		case Bear.State.StandUp:
			this.UpdateStandUp();
			break;
		case Bear.State.Drop:
			this.UpdateDrop();
			break;
		case Bear.State.Arming:
			this.UpdateArming();
			break;
		case Bear.State.Armed:
			this.UpdateShooting();
			break;
		}
	}
	public override void Die(GameObject killer)
	{
		base.SetState("dead", true);
		this.state = Bear.State.Dead;
		base.Die(killer);
	}
	public override void Alert(GameObject player, Vector3 position)
	{
		if (base.target == player)
		{
			return;
		}
		this.state = Bear.State.RunToPlayer;
		base.target = player;
		this.agent.destination = position;
		this.agent.speed = this.attackRunSpeed;
	}
	public void SetArmed(bool val)
	{
		if (this.allowArm)
		{
			this.armed = val;
		}
		else
		{
			this.armed = false;
		}
		if (!val && this.state == Bear.State.Armed)
		{
			this.state = Bear.State.Drop;
			base.SetState("stand", false);
			base.SetState("armed", false);
			this.attackTimeLeft = 1f;
		}
	}
	public override void UpdateModel()
	{
		if (GameState.isServer)
		{
			return;
		}
		if (this.gunInstance == null)
		{
			if (this.animStates.ContainsKey("armed") && this.animStates["armed"])
			{
				Transform transform = null;
				Transform[] componentsInChildren = base.transform.GetComponentsInChildren<Transform>();
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					Transform transform2 = componentsInChildren[i];
					if (transform2.name == "GunAttachmentPoint")
					{
						transform = transform2;
					}
				}
				if (transform != null && this.gunModel != null)
				{
					this.gunInstance = (GameObject)UnityEngine.Object.Instantiate(this.gunModel);
					this.gunInstance.transform.parent = transform;
					this.gunInstance.transform.localPosition = new Vector3(0f, 0f, 0f);
					this.gunInstance.transform.localRotation = Quaternion.identity;
					this.gunInstance.transform.localScale = new Vector3(1f, 1f, 1f);
				}
			}
		}
		else
		{
			if (!this.animStates.ContainsKey("armed") || !this.animStates["armed"])
			{
				UnityEngine.Object.Destroy(this.gunInstance);
				this.gunInstance = null;
			}
		}
	}
	public override void StateUpdate(string name)
	{
		if (GameState.isServer)
		{
			return;
		}
		if (name == "fire")
		{
			LocalPlayerEvents.PlaySound("AssaultRifleFire", base.transform);
			if (this.gunInstance != null && this.gunInstance.GetComponent<ThirdPersonWeapon>() != null)
			{
				this.gunInstance.GetComponent<ThirdPersonWeapon>().Fire();
			}
		}
	}
}
