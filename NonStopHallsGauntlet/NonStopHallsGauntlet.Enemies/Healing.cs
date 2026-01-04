using UnityEngine;
using HutongGames.PlayMaker;

namespace NonStopHallsGauntlet.Enemies
{
    public class Healing : MonoBehaviour
    {
        public class HealBurst : FsmStateAction
        {
            public FsmOwnerDefault target;
            private HealthManager hm;
            private GameObject go;
            public int maxHP;
            public int healAmount;
            private Renderer rn;
            private Color originalColor;
            private float visualTimer;
            private float flashTimer;
            private float flashInterval;
            private bool flashOn;
            private bool doHeal;
            private MaterialPropertyBlock block;
            private int hpCheck;

            public override void OnEnter()
            {
                doHeal = false;

                go = Fsm.GetOwnerDefaultTarget(target);

                hm = go.GetComponent<HealthManager>();
                hpCheck = hm.hp;

                rn = go.GetComponentInChildren<Renderer>();

                originalColor = rn.sharedMaterial.color;

                block = new MaterialPropertyBlock();

                float healChance = 1f - ((float)hm.hp / maxHP);

                float rand = Random.value;

                if (rand >= healChance)
                {
                    Finish();
                    return;
                }

                doHeal = true;
                visualTimer = 1.5f;
                flashInterval = 0.2f;
                flashTimer = flashInterval;
                flashOn = true;
            }

            public override void OnUpdate()
            {
                if (!doHeal || rn == null)
                    return;

                if (hm.hp < hpCheck)
                {
                    Finish();
                    return;
                }

                hpCheck = hm.hp;

                visualTimer -= Time.deltaTime;
                if (visualTimer <= 0f)
                {
                    hm.hp = Mathf.Min(hm.hp + healAmount, maxHP);
                    block.SetColor("_Color", originalColor);
                    rn.SetPropertyBlock(block);
                    Finish();
                }

                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f)
                {
                    flashOn = !flashOn;
                    if (flashOn)
                    {
                        flashInterval = Mathf.Max(0.05f, flashInterval * 0.75f);
                    }
                    flashTimer = flashInterval;
                }

                block.SetColor("_Color",
                    flashOn ? new Color(0.1f, 0.1f, 0.1f, 1.0f) : originalColor);
                rn.SetPropertyBlock(block);
            }

            public override void OnExit()
            {
                if (rn != null)
                {
                    block.SetColor("_Color", originalColor);
                    rn.SetPropertyBlock(block);
                }
            }
        }
    }
}