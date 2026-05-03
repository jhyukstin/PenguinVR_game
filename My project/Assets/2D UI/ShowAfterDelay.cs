using System.Collections;
using UnityEngine;

public class ShowAfterDelay : MonoBehaviour
{
    public GameObject targetObject; // 활성화할 오브젝트
        public float delay = 2f;

        void Start()
        {
            StartCoroutine(Activate());
        }

        IEnumerator Activate()
        {
            yield return new WaitForSeconds(delay);
            targetObject.SetActive(true);
        }
}