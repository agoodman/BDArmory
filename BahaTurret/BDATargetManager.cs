//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18449
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BahaTurret
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class BDATargetManager : MonoBehaviour
	{
		public static Dictionary<BDArmorySettings.BDATeams, List<TargetInfo>> TargetDatabase;

		string debugString = string.Empty;

		void Start()
		{
			TargetDatabase = new Dictionary<BDArmorySettings.BDATeams, List<TargetInfo>>();
			TargetDatabase.Add(BDArmorySettings.BDATeams.A, new List<TargetInfo>());
			TargetDatabase.Add(BDArmorySettings.BDATeams.B, new List<TargetInfo>());
			StartCoroutine(CleanDatabaseRoutine());
		}

		void Update()
		{
			if(BDArmorySettings.DRAW_DEBUG_LABELS)
			{
				debugString = string.Empty;
				debugString+= ("Team A's targets:");
				foreach(var targetInfo in TargetDatabase[BDArmorySettings.BDATeams.A])
				{
					if(targetInfo)
					{
						if(!targetInfo.Vessel)
						{
							debugString+= ("\n - A target with no vessel reference.");
						}
						else
						{
							debugString+= ("\n - "+targetInfo.Vessel.vesselName+", Engaged by "+targetInfo.numFriendliesEngaging);
						}
					}
					else
					{
						debugString+= ("\n - A null target info.");
					}
				}
				debugString+= ("\nTeam B's targets:");
				foreach(var targetInfo in TargetDatabase[BDArmorySettings.BDATeams.B])
				{
					if(targetInfo)
					{
						if(!targetInfo.Vessel)
						{
							debugString+= ("\n - A target with no vessel reference.");
						}
						else
						{
							debugString+= ("\n - "+targetInfo.Vessel.vesselName+", Engaged by "+targetInfo.numFriendliesEngaging);
						}
					}
					else
					{
						debugString+= ("\n - A null target info.");
					}
				}
			}
		}

		public static BDArmorySettings.BDATeams BoolToTeam(bool team)
		{
			return team ? BDArmorySettings.BDATeams.B : BDArmorySettings.BDATeams.A;
		}

		public static BDArmorySettings.BDATeams OtherTeam(BDArmorySettings.BDATeams team)
		{
			return team == BDArmorySettings.BDATeams.A ? BDArmorySettings.BDATeams.B : BDArmorySettings.BDATeams.A;
		}

		IEnumerator CleanDatabaseRoutine()
		{
			while(enabled)
			{
				yield return new WaitForSeconds(5);
			
				TargetDatabase[BDArmorySettings.BDATeams.A].RemoveAll(target => target == null);
				TargetDatabase[BDArmorySettings.BDATeams.A].RemoveAll(target => target.team == BDArmorySettings.BDATeams.A);
				TargetDatabase[BDArmorySettings.BDATeams.A].RemoveAll(target => !target.isThreat);

				TargetDatabase[BDArmorySettings.BDATeams.B].RemoveAll(target => target == null);
				TargetDatabase[BDArmorySettings.BDATeams.B].RemoveAll(target => target.team == BDArmorySettings.BDATeams.B);
				TargetDatabase[BDArmorySettings.BDATeams.B].RemoveAll(target => !target.isThreat);
			}
		}

		void RemoveTarget(TargetInfo target, BDArmorySettings.BDATeams team)
		{
			TargetDatabase[team].Remove(target);
		}

		public static void ReportVessel(Vessel v, MissileFire reporter)
		{
			if(!v) return;

			TargetInfo info = v.gameObject.GetComponent<TargetInfo>();
			if(!info)
			{
				TargetInfo newInfo = v.gameObject.AddComponent<TargetInfo>();
				newInfo.Vessel = v;
				info = newInfo;
			}
			BDArmorySettings.BDATeams team = BoolToTeam(reporter.team);
			if(!TargetDatabase[team].Contains(info))
			{
				TargetDatabase[team].Add(info);
			}
		}

		public static TargetInfo GetAirToAirTarget(MissileFire mf)
		{
			BDArmorySettings.BDATeams team = mf.team ? BDArmorySettings.BDATeams.B : BDArmorySettings.BDATeams.A;
			TargetInfo finalTarget = null;

			foreach(var target in TargetDatabase[team])
			{
				if(target && !target.isLanded && !target.isMissile)
				{
					if(finalTarget == null || target.numFriendliesEngaging < finalTarget.numFriendliesEngaging)
					{
						finalTarget = target;
					}
				}
			}

			return finalTarget;
		}
		 

		public static TargetInfo GetClosestTarget(MissileFire mf)
		{
			BDArmorySettings.BDATeams team = mf.team ? BDArmorySettings.BDATeams.B : BDArmorySettings.BDATeams.A;
			TargetInfo finalTarget = null;

			foreach(var target in TargetDatabase[team])
			{
				if(target && mf.CanSeeTarget(target.Vessel))
				{
					if(finalTarget == null || (target.IsCloser(finalTarget, mf)))
					{
						finalTarget = target;
					}
				}
			}

			return finalTarget;
		}

		public static List<TargetInfo> GetAllTargetsExcluding(List<TargetInfo> excluding, MissileFire mf)
		{
			List<TargetInfo> finalTargets = new List<TargetInfo>();
			BDArmorySettings.BDATeams team = BoolToTeam(mf.team);

			foreach(var target in TargetDatabase[team])
			{
				if(target && target.Vessel && mf.CanSeeTarget(target.Vessel) && !excluding.Contains(target))
				{
					finalTargets.Add(target);
				}
			}

			return finalTargets;
		}

		public static TargetInfo GetLeastEngagedTarget(MissileFire mf)
		{
			BDArmorySettings.BDATeams team = mf.team ? BDArmorySettings.BDATeams.B : BDArmorySettings.BDATeams.A;
			TargetInfo finalTarget = null;
			
			foreach(var target in TargetDatabase[team])
			{
				if(target && mf.CanSeeTarget(target.Vessel))
				{
					if(finalTarget == null || target.numFriendliesEngaging < finalTarget.numFriendliesEngaging)
					{
						finalTarget = target;
					}
				}
			}
		
			return finalTarget;
		}

		public static TargetInfo GetMissileTarget(MissileFire mf)
		{
			BDArmorySettings.BDATeams team = mf.team ? BDArmorySettings.BDATeams.B : BDArmorySettings.BDATeams.A;
			TargetInfo finalTarget = null;

			foreach(var target in TargetDatabase[team])
			{
				if(target && mf.CanSeeTarget(target.Vessel) && target.isMissile)
				{
					bool isHostile = false;
					if(target.missileModule && target.missileModule.targetMf && target.missileModule.targetMf.team == mf.team)
					{
						isHostile = true;
					}

					if(isHostile && ((finalTarget == null && target.numFriendliesEngaging < 2) || target.numFriendliesEngaging < finalTarget.numFriendliesEngaging))
					{
						finalTarget = target;
					}
				}
			}
			
			return finalTarget;
		}

		public static TargetInfo GetClosestMissileTarget(MissileFire mf)
		{
			BDArmorySettings.BDATeams team = BoolToTeam(mf.team);
			TargetInfo finalTarget = null;
			
			foreach(var target in TargetDatabase[team])
			{
				if(target && mf.CanSeeTarget(target.Vessel) && target.isMissile)
				{
					bool isHostile = false;
					if(target.missileModule && target.missileModule.targetMf && target.missileModule.targetMf.team == mf.team)
					{
						isHostile = true;
					}

					if(isHostile && (finalTarget == null || target.IsCloser(finalTarget, mf)))
					{
						finalTarget = target;
					}
				}
			}
			
			return finalTarget;
		}



		void OnGUI()
		{
			if(BDArmorySettings.DRAW_DEBUG_LABELS)	
			{
				GUI.Label(new Rect(600,100,600,600), debugString);	
			}
		}
	}
}

