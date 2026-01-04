using System.Collections;
using UnityEngine;

namespace NonStopHallsGauntlet.Player
{
	public class MoveToHighHall : MonoBehaviour
	{
		public static MoveToHighHall Instance;

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
				DontDestroyOnLoad(gameObject);
			}
			else
			{
				Destroy(gameObject);
			}
		}

		public void StartHighHallTeleport()
		{
			StartCoroutine(Teleport("Hang_04", "right1"));
		}

		private IEnumerator Teleport(string scene, string gate)
		{
			ScreenFaderUtils.Fade(ScreenFaderUtils.GetColour(), Color.black, 1f);
			yield return new WaitForSeconds(1f);

			AudioSourceFadeControl[] sources = FindObjectsByType<AudioSourceFadeControl>(
				FindObjectsInactive.Include, 0);
			foreach (AudioSourceFadeControl src in sources)
			{
				src.FadeDown();
			}

			GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
			{
				SceneName = scene,
				EntryGateName = gate,
				Visualization = 0,
				PreventCameraFadeOut = true
			});
		}
	}
}
