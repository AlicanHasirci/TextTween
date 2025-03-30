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
    using Object = UnityEngine.Object;

    public sealed class PerformanceTests
    {
        private readonly List<GameObject> _spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in _spawned.Where(gameObject => gameObject != null))
            {
                Object.Destroy(gameObject);
            }
            _spawned.Clear();
        }

        [Test]
        public void Benchmark()
        {
            // Run over more than one seconds to stabilize things, then we take an average of total ops / second
            TimeSpan timeout = TimeSpan.FromSeconds(2);

            Debug.Log($"## Performance {Environment.NewLine}");
            Debug.Log(
                "| Text | Color Modifier Op/s | Transform Modifier Op/s | Warp Modifier Op/s |"
            );
            Debug.Log(
                "| ---- | ------------------- | ----------------------- | ------------------ |"
            );

            SetupGameObjects<ColorModifier>(
                out TweenManager color,
                out TextMeshPro colorTextObject
            );
            SetupGameObjects<TransformModifier>(
                out TweenManager transform,
                out TextMeshPro transformTextObject
            );
            SetupGameObjects<WarpModifier>(out TweenManager warp, out TextMeshPro warpTextObject);

            int[] textLengths = { 1, 10, 100, 1_000, 10_000, 1000_000 };
            foreach (int textLength in textLengths)
            {
                RunTest(
                    timeout,
                    GenerateText(textLength),
                    color,
                    colorTextObject,
                    transform,
                    transformTextObject,
                    warp,
                    warpTextObject
                );
            }
        }

        private void SetupGameObjects<T>(out TweenManager tweenManager, out TextMeshPro text)
            where T : CharModifier
        {
            GameObject tweenManagerObject = new(
                $"TweenManager - {typeof(T).Name}",
                typeof(TweenManager),
                typeof(T)
            );
            _spawned.Add(tweenManagerObject);
            GameObject textObject = new($"{typeof(T).Name} Text", typeof(TextMeshPro));
            _spawned.Add(textObject);

            text = textObject.GetComponent<TextMeshPro>();
            tweenManager = tweenManagerObject.GetComponent<TweenManager>();
            tweenManager._texts = new TMP_Text[] { text };
            T modifier = tweenManager.GetComponent<T>();
            SetupModifier(modifier);
            tweenManager._modifiers = new List<CharModifier> { modifier };
        }

        // Modifiers need to have their relevant properties setup, or they'll throw doing runtime stuff
        private static void SetupModifier(CharModifier modifier)
        {
            switch (modifier)
            {
                case ColorModifier colorModifier:
                {
                    Gradient gradient = new();
                    GradientColorKey[] colorKeys =
                    {
                        new GradientColorKey(Color.red, 0),
                        new GradientColorKey(Color.white, 1),
                    };
                    GradientAlphaKey[] alphaKeys =
                    {
                        new GradientAlphaKey(1, 0),
                        new GradientAlphaKey(1, 1),
                    };
                    gradient.SetKeys(colorKeys, alphaKeys);
                    colorModifier.Gradient = gradient;
                    break;
                }
                case TransformModifier transformModifier:
                {
                    transformModifier.Curve = GenerateAnimationCurve();
                    break;
                }
                case WarpModifier warpModifier:
                {
                    warpModifier.WarpCurve = GenerateAnimationCurve();
                    break;
                }
                default:
                    Assert.Fail($"Currently unsupported modifier type: {modifier.GetType()}");
                    break;
            }
        }

        private static AnimationCurve GenerateAnimationCurve()
        {
            AnimationCurve curve = new();
            curve.AddKey(new Keyframe(0, 0));
            curve.AddKey(new Keyframe(1, 1));
            return curve;
        }

        private static void RunTest(
            TimeSpan timeout,
            string text,
            TweenManager color,
            TextMeshPro colorTextObject,
            TweenManager transform,
            TextMeshPro transformTextObject,
            TweenManager warp,
            TextMeshPro warpTextObject
        )
        {
            string colorCount = RunPerfTest(color, colorTextObject);
            string transformCount = RunPerfTest(transform, transformTextObject);
            string warpCount = RunPerfTest(warp, warpTextObject);

            Debug.Log(
                $"| {RunLengthEncode(text)} | {colorCount} | {transformCount} | {warpCount} |"
            );

            return;

            string RunPerfTest(TweenManager tweenManager, TextMeshPro textObject)
            {
                textObject.text = text;
                // TODO: Figure out how to run this in a more fully-featured way such that we don't have to kick things

                // Need to kick stuff, we're not operating in a full Unity context right now
                textObject.ForceMeshUpdate(forceTextReparsing: true);
                // Need to kick the TweenManager too
                tweenManager.OnTextChanged(textObject);

                int count = 0;
                Stopwatch timer = Stopwatch.StartNew();
                do
                {
                    tweenManager.Progress = (float)(
                        timer.ElapsedMilliseconds / timeout.TotalMilliseconds
                    );
                    tweenManager.ForceUpdate();
                    ++count;
                } while (timer.Elapsed < timeout);

                return FormatNumber((int)Math.Floor(count / timeout.TotalSeconds));
            }
        }

        private static string FormatNumber(int number)
        {
            return number.ToString("N0");
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

            StringBuilder result = new();
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
                        result.Append('x').Append(FormatNumber(count));
                    }

                    currentChar = input[i];
                    count = 1;
                }
            }

            result.Append(currentChar);
            if (count > 1)
            {
                result.Append('x').Append(FormatNumber(count));
            }

            return result.ToString();
        }
    }
#endif
}
