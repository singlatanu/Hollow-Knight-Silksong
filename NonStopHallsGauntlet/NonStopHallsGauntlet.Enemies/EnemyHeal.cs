using UnityEngine;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System.Collections.Generic;
using System.Linq;

namespace NonStopHallsGauntlet.Enemies
{
    internal class EnemyHeal : MonoBehaviour
    {
        private HealthManager hm;
        public int healAmount;
        private EnemyTag tag;


        private static readonly Dictionary<Enemy, string> HealStateByType = new()
        {
            { Enemy.Reed, "Alert" },
            { Enemy.Bellringer, "Walker" },
            { Enemy.Choristor, "Walker" },
            { Enemy.Maiden, "Fly" },
            { Enemy.Administrator, "Retreat?" },
            { Enemy.Bellbearer, "Out of Range" },
            { Enemy.Maestro, "Defending" },
            { Enemy.Sentry, "Walk" }
        };
        private static readonly HashSet<Enemy> UsesWaitRandom = new()
        {
            Enemy.Reed,
            Enemy.Maiden,
            Enemy.Administrator,
            Enemy.Sentry
        };
        private void Awake()
        {
            hm = GetComponent<HealthManager>();

            healAmount = Mathf.CeilToInt(Battle.enemyHeal * hm.hp / 100f);

            tag = GetComponent<EnemyTag>();
            if (tag == null)
                return;

            HealingPatch();
            HookDeath();
        }
        private void HealingPatch()
        {
            if (healAmount == 0)
                return;

            PlayMakerFSM fsm =
                tag.Type == Enemy.Bellringer || tag.Type == Enemy.Choristor
                    ? FSMUtility.LocateMyFSM(gameObject, "Attack")
                    : FSMUtility.LocateMyFSM(gameObject, "Control");

            if (fsm == null)
                return;

            if (!HealStateByType.TryGetValue(tag.Type, out string stateName))
                return;

            FsmState state = fsm.Fsm.GetState(stateName);
            if (state == null)
                return;

            foreach (var action in state.Actions)
            {
                if (UsesWaitRandom.Contains(tag.Type) && action is WaitRandom wr)
                {
                    wr.timeMin = 2.1f;
                    wr.timeMax = 2.2f;
                }
                else if (!UsesWaitRandom.Contains(tag.Type) && action is Wait wait)
                {
                    wait.time.Value = 2.2f;
                }
            }

            var actions = state.Actions.ToList();
            actions.Add(new Healing.HealBurst
            {
                target = new FsmOwnerDefault { OwnerOption = OwnerDefaultOption.UseOwner },
                maxHP = hm.hp,
                healAmount = healAmount
            });

            state.Actions = actions.ToArray();
        }
        private void HookDeath()
        {
            hm.OnDeath += OnDie;
        }

        private void OnDie()
        {
            if (tag == null)
                return;
            Battle.RegisterEnemyDeath(tag.Type);

            EnemyDeathEffects deathEffects = GetComponent<EnemyDeathEffects>();
            if (deathEffects != null)
            {
                deathEffects.CorpseEmitted += OnCorpseEmitted;
            }
        }

        private void OnCorpseEmitted(GameObject corpse)
        {
            if (corpse == null)
                return;

            var fadeCorpse = corpse.AddComponent<Fader>();
            fadeCorpse.lifetime = Random.Range(5f, 6f);
            fadeCorpse.fadeDuration = 1.0f;

            foreach (Transform child in corpse.transform)
            {
                if (child.name.Contains("Silk"))
                    continue;
                var fadeChild = child.gameObject.AddComponent<Fader>();
                fadeChild.lifetime = Random.Range(5f, 6f);
                fadeChild.fadeDuration = 1.0f;
            }
        }
    }
}