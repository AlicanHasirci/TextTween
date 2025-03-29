namespace TextTween.Tests.Runtime
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using Modifiers;
    using NUnit.Framework;
    using TMPro;
    using UnityEngine;
    using Debug = UnityEngine.Debug;

    public sealed class PerformanceTests
    {
        private readonly List<GameObject> _spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in _spawned.Where(gameObject => gameObject != null))
            {
                GameObject.Destroy(gameObject);
            }
        }
        
        [Test]
        public void Benchmark()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(2);

            Debug.Log("| Text | Color Modifier Op/s | Transform Modifier Op/s | Warp Modifier Op/s |");
            Debug.Log("| ---- | ------------------- | ----------------------- | ------------------ |");


            SetupGameObjects<ColorModifier>(out TweenManager color, out TextMeshPro colorTextObject);
            SetupGameObjects<TransformModifier>(out TweenManager transform, out TextMeshPro transformTextObject);
            SetupGameObjects<WarpModifier>(out TweenManager warp, out TextMeshPro warpTextObject);

            RunTest(timeout, GenerateText(1), color, colorTextObject, transform, transformTextObject, warp, warpTextObject);
            RunTest(timeout, GenerateText(10), color, colorTextObject, transform, transformTextObject, warp, warpTextObject);
            RunTest(timeout, GenerateText(100), color, colorTextObject, transform, transformTextObject, warp, warpTextObject);
            RunTest(timeout, GenerateText(1_000), color, colorTextObject, transform, transformTextObject, warp, warpTextObject);
            RunTest(timeout, GenerateText(1_0000), color, colorTextObject, transform, transformTextObject, warp, warpTextObject);
            return;

            void SetupGameObjects<T>(out TweenManager tweenManager, out TextMeshPro text) where T: CharModifier
            {
                GameObject tweenManagerObject = new($"{typeof(T).Name}", typeof(TweenManager), typeof(T));
                _spawned.Add(tweenManagerObject);
                GameObject textObject = new($"{typeof(T).Name} Text", typeof(TextMeshPro));
                _spawned.Add(textObject);
                
                text = textObject.GetComponent<TextMeshPro>();
                tweenManager = tweenManagerObject.GetComponent<TweenManager>();
                TweenManager.TextField.Value.SetValue(tweenManager, new TMP_Text[]{text});
                TweenManager.ModifiersField.Value.SetValue(tweenManager, new List<CharModifier>(){tweenManager.GetComponent<T>()});
                tweenManager.CreateNativeArrays();
            }
        }

        private static void RunTest(TimeSpan timeout, string text, TweenManager color, TextMeshPro colorTextObject, TweenManager transform, TextMeshPro transformTextObject, TweenManager warp, TextMeshPro warpTextObject)
        {
            int colorCount = RunPerfTest(color, colorTextObject);
            int transformCount = RunPerfTest(transform, transformTextObject);
            int warpCount = RunPerfTest(warp, warpTextObject);
            
            Debug.Log($"| {RunLengthEncode(text)} | {colorCount} | {transformCount} | {warpCount} |");

            return;

            int RunPerfTest(TweenManager tweenManager, TextMeshPro textObject)
            {
                textObject.text = text;
                
                int count = 0;
                Stopwatch timer = Stopwatch.StartNew();
                do
                {
                    tweenManager.Progress = (float)(timer.ElapsedMilliseconds / timeout.TotalMilliseconds);
                    tweenManager.ForceUpdate();
                    ++count;
                } while (timer.Elapsed < timeout);

                return count;
            }
        }

        private static string GenerateText(int length)
        {
            return new string(Enumerable.Repeat('A', length).ToArray());
        }
        
        private static string RunLengthEncode(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            StringBuilder result = new ();
            char currentChar = input[0];
            int count = 1;

            for (int i = 1; i < input.Length; i++)
            {
                if (input[i] == currentChar)
                {
                    count++;
                }
                else
                {
                    result.Append(currentChar);
                    if (count > 1)
                    {
                        result.Append('x').Append(count);
                    }

                    currentChar = input[i];
                    count = 1;
                }
            }

            // Append the final character group
            result.Append(currentChar);
            if (count > 1)
            {
                result.Append('x').Append(count);
            }

            return result.ToString();
        }
    }
    #endif
}