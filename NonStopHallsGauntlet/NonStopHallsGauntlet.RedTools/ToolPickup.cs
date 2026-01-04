using UnityEngine;

public class ToolPickup : MonoBehaviour
{
    public ToolItem Tool;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (Tool != null)
        {
            RedTools.ReplenishTool(Tool);
        }

        Destroy(transform.parent.gameObject);
    }
}
