![](EditorResources/text_tween.gif)

# TextTween

**TextTween** is a lightweight Unity library designed to animate [TextMesh Pro (TMP)](https://docs.unity3d.com/Packages/com.unity.textmeshpro@latest) texts with high performance. It leverages Unity's **Job System** and **Burst Compiler** to deliver smooth and efficient character-level animations.

## ✨ Features

- 🚀 High-performance character animation using **Jobs** and **Burst**
- 🔠 Fine-grained control over individual TMP characters
- 🎮 Easy to integrate into existing Unity projects
- 🧩 Lightweight and dependency-free (except TMP)

## 📦 Installation

You can add the package via PackageManager with the URL:
   ```
   git@github.com:AlicanHasirci/TextTween.git
   ```

## 🚀 Usage

![](EditorResources/image_00.png)

1. Start by adding **TweenManager** to your text.
2. Bind the text to **Text** property.
3. Add modifier components to a game object and add them to the list of **Modifiers**. Re-arrange the modifiers to change the order of modification.
4. By changing the **Offset** value, you can adjust the char animation overlap. '0' for all letters animating together, '1' for all letters to animate one by one. 

### Modifiers

#### 1.Transform Modifier
![](EditorResources/transform.gif)
![](EditorResources/transform_ss.png)

Allows you to modify letters position, scale or rotation according to curve over progress of TweenManager.

- Curve: Add easing to progress propagated by tween manager.
- Type: Shows the value to modify(position, rotation or scale).
- Scale: Dimension mask for scale operation.
- Intensity: Amount of change per axis.
- Pivot: Pivot point of transformation.

#### 2.Color Modifier
![](EditorResources/color.gif)
![](EditorResources/color_ss.png)

Lets to change the color of letters over time.

- Gradient: The colors that will be interpolated according to progress.

#### 3.Warp Modifier
![](EditorResources/warp.gif)
![](EditorResources/warp_ss.png)

Warps the lines of text according to intensity and curve provided over progress. The intensity is multiplied by the curve value and applied to letters as Y displacement.

- Intensity: Amount of displacement
- Warp Curve: Curve to be used by modifier

## Performance

| Text | Color Modifier Op/s | Transform Modifier Op/s | Warp Modifier Op/s |
| ---- | ------------------- | ----------------------- | ------------------ |
| A | 22,481 | 1,340,276 | 1,358,647 |
| Ax10 | 22,475 | 1,344,406 | 1,360,844 |
| Ax100 | 22,502 | 1,345,724 | 1,360,727 |
| Ax1,000 | 22,509 | 1,340,891 | 1,359,820 |
| Ax10,000 | 22,496 | 1,340,688 | 1,358,385 |
| Ax1,000,000 | 22,504 | 1,287,208 | 1,360,404 |

## Contributing

This project uses [CSharpier](https://csharpier.com/) with the default configuration to enable an enforced, consistent style. If you would like to contribute, recommendation is to ensure that changed files are ran through CSharpier prior to merge. This can be done automatically through editor plugins, or, minimally, by installing a [pre-commit hook](https://pre-commit.com/#3-install-the-git-hook-scripts)