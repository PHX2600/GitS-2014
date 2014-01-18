using System;
using UnityEngine;
public class Quest : ScriptableObject
{
	public string internalName;
	public string displayName;
	public Objective[] objectives;
	public Objective GetObjectiveByName(string name)
	{
		Objective[] array = this.objectives;
		for (int i = 0; i < array.Length; i++)
		{
			Objective objective = array[i];
			if (objective.internalName == name)
			{
				return objective;
			}
		}
		return null;
	}
}
