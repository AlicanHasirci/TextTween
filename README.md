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
| A | 18,459 | 53,887 | 76,252 |
| Ax10 | 17,105 | 27,607 | 44,653 |
| Ax100 | 10,412 | 5,461 | 9,936 |
| Ax1000 | 4,313 | 2,981 | 3,697 |
| Ax10000 | 372 | 290 | 355 |
| Ax100000 | 76 | 48 | 59 |
