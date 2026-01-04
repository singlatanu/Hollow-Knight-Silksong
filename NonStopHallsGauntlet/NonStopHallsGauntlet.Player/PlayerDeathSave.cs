using UnityEngine;
using System;
using System.IO;
using BepInEx;
using System.Collections;


namespace NonStopHallsGauntlet.Enemies
{
    internal class PlayerDeathSave : MonoBehaviour
    {
        private HeroController heroController;

        private void Awake()
        {
            heroController = GetComponent<HeroController>();
            PlayMakerFSM fsm = heroController.harpoonDashFSM;
            if (heroController != null)
                heroController.OnDeath += HandleDeath;
        }

        private void HandleDeath()
        {
            StartCoroutine(OnDie());
        }

        private IEnumerator OnDie()
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.001f, 0.1f));
            yield return new WaitForEndOfFrame();

            string folder = Path.Combine(Paths.PluginPath, "NonStopHallsGauntlet");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string path = Path.Combine(folder, $"screenshot_{timestamp}.png");

            int width = Screen.width;
            int height = Screen.height;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Destroy(tex);

            yield return new WaitForSeconds(2f);
            yield return new WaitForEndOfFrame();

            OverlayEnemies.Instance.ClearAllOverlays();

            ReplenishManager rm = Plugin.Instance.replenishManagerGO.GetComponent<ReplenishManager>();
            if (rm != null)
            {
                Destroy(rm);
            }
        }
    }
}
