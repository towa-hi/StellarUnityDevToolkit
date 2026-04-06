using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace StellarSDK
{
    public static class AsyncDelay
    {
        public static Task Delay(int millisecondsDelay)
        {
            return WaitForSecondsAsync(millisecondsDelay / 1000f);
        }

        static Task WaitForSecondsAsync(float seconds)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            CoroutineRunner.instance.StartCoroutine(WaitForSecondsCoroutine(seconds, tcs));
            return tcs.Task;
        }

        static IEnumerator WaitForSecondsCoroutine(float seconds, TaskCompletionSource<bool> tcs)
        {
            yield return new WaitForSeconds(seconds);
            tcs.SetResult(true);
        }
    }

    public class CoroutineRunner : MonoBehaviour
    {
        static CoroutineRunner ins;

        public static CoroutineRunner instance
        {
            get
            {
                if (!ins)
                {
                    GameObject go = new("CoroutineRunner");
                    ins = go.AddComponent<CoroutineRunner>();
                    DontDestroyOnLoad(go);
                }
                return ins;
            }
        }
    }
}
