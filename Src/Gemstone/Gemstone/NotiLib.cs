using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Gemstone.Gemstone
{
    public class NotiLib : MonoBehaviour
    {
        public static NotiLib Instance;
        private static List<GameObject> notifications = new List<GameObject>();
        private const float Y_OFFSET = 0.05f;

        public static void SendNotification(string message, float time)
        {
            if (Instance == null) return;

            var textNotifacation = new GameObject("NotificationLabel");
            textNotifacation.transform.SetParent(GorillaLocomotion.GTPlayer.Instance.bodyCollider.transform, false);
            textNotifacation.transform.localScale = Vector3.one * 0.0025f;
            notifications.Add(textNotifacation);
            int index = notifications.Count - 1;

            textNotifacation.transform.localPosition = new Vector3(0f, index * Y_OFFSET, 0.45f);
            textNotifacation.transform.LookAt(GorillaLocomotion.GTPlayer.Instance.headCollider.transform.position);
            textNotifacation.transform.Rotate(0f, 180f, 0f);

            var text = textNotifacation.AddComponent<TextMeshPro>();
            text.text = message;
            text.fontSize = 15f;
            text.alignment = TextAlignmentOptions.Center;
            //text.fontMaterial.shader = Shader.Find("GUI/Text Shader");
            text.color = Color.white;
            text.enableAutoSizing = true;
            text.rectTransform.sizeDelta = new Vector2(500f, 400f);
            text.transform.localScale = new Vector3(0.0025f, 0.0025f, 0.0025f);

            Instance.StartCoroutine(Instance.DestroyAfterTime(textNotifacation, time));
        }

        private IEnumerator DestroyAfterTime(GameObject obj, float time)
        {
            yield return new WaitForSeconds(time / 1000.0f);

            if (notifications.Contains(obj))
            {
                notifications.Remove(obj);
                Destroy(obj);
                FixNotifPos();
            }
        }

        private static void FixNotifPos()
        {
            for (int i = 0; i < notifications.Count; i++)
            {
                if (notifications[i] == null) continue;

                notifications[i].transform.localPosition = new Vector3(0f, i * Y_OFFSET, 0.45f);
            }
        }

        void Awake()
        {
            Instance = this;
        }
    }
}