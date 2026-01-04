using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using NonStopHallsGauntlet.Enemies;
using NonStopHallsGauntlet;
using UnityEngine.SceneManagement;
using NonStopHallsGauntlet.Player;

[HarmonyPatch]
public class Battle
{
    public static BattleScene scene;
    public static List<GameObject> enemyPool = new();

    public static int totalEnemies;
    private static int maxEnemies;
    public static int enemyHeal;

    public class EnemyDefinition
    {
        public Enemy Type;
        public string DisplayName;
        public string PrefabNameMatch;
        public int Count;
        public Vector3 SpawnMin;
        public Vector3 SpawnMax;
    }

    private static readonly Dictionary<Enemy, EnemyDefinition> Enemies =
    new()
    {
        {
            Enemy.Reed,
            new EnemyDefinition
            {
                Type = Enemy.Reed,
                DisplayName = "Reed",
                PrefabNameMatch = "Song Reed"
            }
        },
        {
            Enemy.Bellringer,
            new EnemyDefinition
            {
                Type = Enemy.Bellringer,
                DisplayName = "Bellringer",
                PrefabNameMatch = "Song Pilgrim 01"
            }
        },
        {
            Enemy.Choristor,
            new EnemyDefinition
            {
                Type = Enemy.Choristor,
                DisplayName = "Choristor",
                PrefabNameMatch = "Song Pilgrim 03"
            }
        },
        {
            Enemy.Maiden,
            new EnemyDefinition
            {
                Type = Enemy.Maiden,
                DisplayName = "Clawmaiden",
                PrefabNameMatch = "Song Handmaiden"
            }
        },
        {
            Enemy.Administrator,
            new EnemyDefinition
            {
                Type = Enemy.Administrator,
                DisplayName = "Admin",
                PrefabNameMatch = "Song Administrator"
            }
        },
        {
            Enemy.Bellbearer,
            new EnemyDefinition
            {
                Type = Enemy.Bellbearer,
                DisplayName = "Bellbearer",
                PrefabNameMatch = "Pilgrim 03 Song"
            }
        },
        {
            Enemy.Maestro,
            new EnemyDefinition
            {
                Type = Enemy.Maestro,
                DisplayName = "Maestro",
                PrefabNameMatch = "Song Pilgrim Maestro"
            }
        },
        {
            Enemy.Sentry,
            new EnemyDefinition
            {
                Type = Enemy.Sentry,
                DisplayName = "Sentry",
                PrefabNameMatch = "Song Heavy Sentry (2)"
            }
        }
    };

    private static bool IsHang04()
    {
        return SceneManager.GetActiveScene().name.Contains("Hang_04");
    }

    [HarmonyPatch(typeof(BattleScene), nameof(BattleScene.DoStartBattle))]
    [HarmonyPostfix]
    public static void Postfix_DoStartBattle(BattleScene __instance)
    {

        if (!IsHang04())
            return;

        scene = __instance;

        foreach (var def in Enemies.Values)
            def.Count = 0;

        enemyPool = BuildEnemyPool(__instance);

        foreach (BattleWave wave in __instance.waves)
        {
            wave.gameObject.SetActive(false);
        }

        maxEnemies = Plugin.numEnemies.Value;
        enemyHeal = Plugin.enemyHeal.Value;

        scene.StartCoroutine(InitOverlayAndStart());

        PlayerDeathSave();

        Plugin.Instance.replenishManagerGO.AddComponent<ReplenishManager>();
        PlayerSkills.Skills();
    }

    [HarmonyPatch(typeof(BattleScene), "DecrementEnemy")]
    [HarmonyPostfix]
    static void Postfix_EnemyDeath()
    {
        if (!IsHang04())
            return;

        totalEnemies--;
        if (totalEnemies <= 0)
        {
            scene.StartCoroutine(SpawnEndlessWave(scene, enemyPool));
        }
    }

    private static IEnumerator InitOverlayAndStart()
    {
        yield return new WaitForSeconds(1.0f);
        foreach (var def in Enemies.Values)
        {
            OverlayEnemies.Instance.TriggerOverlay(def.DisplayName);
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(0.1f);

        foreach (var def in Enemies.Values)
        {
            OverlayEnemies.Instance.SetOverlayText(
                def.DisplayName,
                def.Count.ToString()
            );
        }

        scene.StartCoroutine(SpawnEndlessWave(scene, enemyPool));
    }

    public static List<GameObject> BuildEnemyPool(BattleScene scene)
    {
        List<GameObject> pool = new();

        foreach (BattleWave wave in scene.waves)
        {
            if (!wave) continue;

            foreach (Transform child in wave.transform)
            {
                var def = Enemies.Values.FirstOrDefault(e =>
                    child.name == e.PrefabNameMatch);

                if (def == null)
                    continue;

                if (pool.Any(p => p.name == child.name))
                    continue;

                GameObject prefab = Object.Instantiate(child.gameObject);
                prefab.name = child.name;
                prefab.SetActive(false);

                prefab.AddComponent<EnemyHeal>();
                prefab.AddComponent<EnemyTag>().Type = def.Type;

                pool.Add(prefab);
            }
        }

        return pool;
    }

    public static IEnumerator SpawnEndlessWave(BattleScene scene, List<GameObject> pool)
    {
        yield return new WaitForSeconds(Random.Range(0.5f, 1.0f));

        int countToSpawn = Random.Range(2, maxEnemies + 1);

        float minX = 23f;
        float maxX = 39f;

        float spacing = (maxX - minX) / (countToSpawn - 1);

        totalEnemies = 0;

        GameObject waveGO = new("Wave_Endless");
        waveGO.transform.SetParent(scene.transform, false);

        for (int i = 0; i < countToSpawn; i++)
        {
            GameObject prefab = Object.Instantiate(pool[Random.Range(0, pool.Count)], waveGO.transform);

            var tag = prefab.GetComponent<EnemyTag>();
            var def = Enemies[tag.Type];

            float x = minX + spacing * i;

            if (tag.Type == Enemy.Choristor)
                prefab.transform.position = new Vector3(x + Random.Range(-1f, 1f), Random.Range(9f, 10f), 0f);
            else if (tag.Type == Enemy.Bellringer)
                prefab.transform.position = new Vector3(x + Random.Range(-1f, 1f), 7f, 0f);
            else if (tag.Type == Enemy.Sentry)
                prefab.transform.position = new Vector3(x + Random.Range(-1f, 1f), 25f, 0f);
            else
                prefab.transform.position = new Vector3(x + Random.Range(-1f, 1f), Random.Range(11f, 13f), 0f);

            prefab.SetActive(true);

            totalEnemies++;

            float enemyX = prefab.transform.GetPositionX();
            if (enemyX < 20f)
                prefab.transform.SetPositionX(20f);
            if (enemyX > 42f)
                prefab.transform.SetPositionX(40f);

            float enemyY = prefab.transform.GetPositionY();
            if (enemyY < 5f)
                prefab.transform.SetPositionY(9f);
        }

        yield return new WaitForSeconds(Random.Range(1.0f, 1.5f));

        BattleWave wave = waveGO.AddComponent<BattleWave>();
        wave.startDelay = 0.1f;
        wave.activateEnemiesOnStart = true;
        wave.Init(scene);

        scene.currentWave = 0;

        int dummy = 0;
        wave.WaveStarted(true, ref dummy);
    }
    public static void RegisterEnemyDeath(Enemy type)
    {
        if (!Enemies.TryGetValue(type, out var def))
            return;

        def.Count++;

        if (OverlayEnemies.Instance != null)
        {
            OverlayEnemies.Instance.SetOverlayText(
                def.DisplayName,
                def.Count.ToString()
            );
        }
    }

    private static void PlayerDeathSave()
    {
        HeroController hero = HeroController.instance;
        if (hero == null)
        {
            return;
        }
        hero.gameObject.AddComponent<PlayerDeathSave>();
    }

}

public class EnemyTag : MonoBehaviour
{
    public Enemy Type;
}