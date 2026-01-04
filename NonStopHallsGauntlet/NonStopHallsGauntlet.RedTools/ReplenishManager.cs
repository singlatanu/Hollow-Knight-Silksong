using UnityEngine;
using System.Collections.Generic;
using NonStopHallsGauntlet;

public class ReplenishManager : MonoBehaviour
{
    public float spawnInterval;
    public Vector2 spawnXRange = new Vector2(20f, 40f);
    public Vector2 spawnYRange = new Vector2(5f, 10f);

    float timer;

    private List<ToolItem> redTools;

    void Awake()
    {
        spawnInterval = Plugin.toolSpawnInterval.Value + Random.Range(-2f, 2f);

        timer = spawnInterval;

        redTools = RedTools.GetEquippedRedTools();
        if (redTools == null || redTools.Count == 0)
            return;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnRandomPickup();
            timer = spawnInterval;
        }
    }

    void SpawnRandomPickup()
    {
        List<ToolItem> spawnable = new List<ToolItem>();
        foreach (var tool in redTools)
        {
            if (RedTools.ToolNeedsReplenish(tool))
            {
                spawnable.Add(tool);
            }
        }

        if (spawnable.Count == 0)
            return;

        for (int i = 0; i < Random.Range(1, Plugin.numTools.Value + 1); i++)
        {
            ToolItem toolToSpawn = spawnable[Random.Range(0, spawnable.Count)];
            CreatePickup(toolToSpawn);
        }
    }

    void CreatePickup(ToolItem tool)
    {
        GameObject toolGO = new GameObject($"{tool.name}_Pickup");

        var sr = toolGO.AddComponent<SpriteRenderer>();
        sr.sprite = RedTools.GetInventorySprite(tool);
        sr.sortingOrder = 10;

        var rb = toolGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.linearDamping = 0.2f;
        rb.angularDamping = 0.7f;
        rb.linearVelocityX = Random.Range(-10f, 10f);
        rb.angularVelocity = Random.Range(-200f, 200f);
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = toolGO.AddComponent<CircleCollider2D>();
        col.radius = 0.2f;

        var pickUp = new GameObject("PickupTrigger");
        pickUp.transform.SetParent(toolGO.transform, false);

        var trigger = pickUp.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.5f;

        PhysicsMaterial2D bounceMat = new PhysicsMaterial2D();
        bounceMat.bounciness = 0.8f;
        bounceMat.friction = 0.2f;

        col.sharedMaterial = bounceMat;

        var pickup = pickUp.AddComponent<ToolPickup>();
        pickup.Tool = tool;

        toolGO.transform.position = new Vector3(
            Random.Range(spawnXRange.x, spawnXRange.y),
            Random.Range(spawnYRange.x, spawnYRange.y),
            0f
        );
        toolGO.SetActive(true);

        var fade = toolGO.AddComponent<Fader>();
        fade.lifetime = Plugin.toolDisappearTime.Value;
        fade.fadeDuration = 0.25f;
    }
}