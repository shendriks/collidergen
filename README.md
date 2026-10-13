# ColliderGen

[![CI](https://github.com/shendriks/collidergen/actions/workflows/ci.yml/badge.svg)](https://github.com/shendriks/collidergen/actions/workflows/ci.yml)
[![Dependabot Updates](https://github.com/shendriks/collidergen/actions/workflows/dependabot/dependabot-updates/badge.svg)](https://github.com/shendriks/collidergen/actions/workflows/dependabot/dependabot-updates)

ColliderGen is an offline tool that derives a convex-hull collider from a sprite. It reads the alpha channel of a PNG and 
builds a convex polygon around the solid pixels. The resulting polygon has at most 16 vertices.

A PNG can be a single frame or an animation strip: several frames of the same width, side by side. Each frame gets
its own collider. See "Animation strips" below for how frames are found.

How it works:

1. A pixel is solid if its alpha is at or above a threshold.
2. For each row, it takes the leftmost and rightmost solid pixel.
3. It builds a convex hull around these pixels. The hull follows the outer edges of the pixels, not
   their centers.
4. It removes vertices until the polygon is simple enough and has at most 16.

Every solid pixel counts, even if it is not connected to the rest. One hull per frame covers them all.

There are two ways to simplify:

- `--fit inner` cuts corners off (Visvalingam-Whyatt). The collider can be a little smaller than the sprite. This is
  good for hitboxes.
- `--fit outer` extends the sides next to an edge until they meet. The collider always covers every solid pixel. This
  is good for walls.

The [glossary](GLOSSARY.md) explains terms like frame, strip and origin.

A bit more detailed info can be found on the [project page](https://shendriks.dev/projects/collider-generator).

## Prerequisites

[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Install

Clone the repository.

### CLI as a global tool

```
dotnet pack src/ColliderGen.Cli -c Release -o artifacts
dotnet tool install --global --add-source artifacts ColliderGen.Tool
```

This adds the `collidergen` command. The examples below use it.

### CLI without installing

```
dotnet run --project src/ColliderGen.Cli -- <sprite.png> [options]
```

This does the same as `collidergen <sprite.png> [options]`. Use it in place of `collidergen` in the examples below.

### Library

Add a reference to the project:

```
dotnet add reference path/to/collidergen/src/ColliderGen/ColliderGen.csproj
```

## Usage

```
collidergen <sprite.png> [options]
collidergen --all <directory> [options]
```

Options:

| Flag             | Default                  | Meaning                                                                                                                                                           |
|------------------|--------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `--all`          | -                        | Process every `*.png` in this directory and its subdirectories                                                                                                    |
| `--threshold`    | `128`                    | A pixel is solid if its alpha (0-255) is at least this value                                                                                                      |
| `--epsilon`      | `0`                      | How much to simplify, in px². `inner` removes vertices whose triangle area is at most this. `outer` removes edges if that adds at most this much area             |
| `--max-vertices` | `16`                     | Maximum number of vertices, 3-16. With `outer`, a rectangle keeps 4 vertices even if you ask for 3                                                                |
| `--origin`       | `center`                 | `center` or `top-left`: where `(0,0)` is in each frame                                                                                                            |
| `--fit`          | `inner`                  | `inner` or `outer`. See above and "Outer fit" below                                                                                                               |
| `--frame-width`  | auto                     | Width of one frame in an animation strip. Each frame gets its own hull. See below                                                                                 |
| `--output`       | `<sprite>.collider.json` | Output file. Not allowed with `--all`                                                                                                                             |
| `--debug`        | off                      | Also write `<sprite>.collider-debug.png`: the sprite at 4x size with the polygon in red. Each frame gets 1 extra pixel of width and height so the outer edges fit |

**Outer fit.** Each step removes the edge that adds the least area. Its two neighboring sides are extended until they
meet. Parallel sides never meet, so a rectangle can't become a triangle. With `--fit outer --max-vertices 3`, a
rectangle keeps 4 vertices. Each step only picks the best edge at that moment, so the result is not always the
smallest possible polygon. A round sprite can get a slightly lopsided collider when `--max-vertices` is low.

**Animation strips.** A PNG can hold several frames of the same width, side by side from left to right. Each frame
gets its own hull, relative to its own origin. Without `--frame-width`, a sprite is split into square frames if its
width is an exact multiple (more than 1) of its height. For example, a 180x36 strip becomes 5 frames of 36x36. Any
other sprite is one frame. Use `--frame-width` for frames that are not square.

**Debug files.** `--all` skips `*.collider-debug.png` files, so you can run it again on a folder that already has
them.

**Errors.** Bad arguments print an error and the usage text, then exit with code 1. This covers unknown flags,
missing values, invalid values (like `--threshold 300` or `--fit wide`) and flags given twice.

Example:

```
collidergen sprites/ship.png
```

This writes `sprites/ship.collider.json` next to the sprite.

Batch example:

```
collidergen --all sprites
```

This writes a `.collider.json` next to every `.png` in `sprites` and its subfolders.

## Output format

A 36×36 sprite with `--max-vertices 4`:

```json
{
    "Frames": [
        {
            "Vertices": [
                {
                    "X": -18,
                    "Y": -18
                },
                {
                    "X": 18,
                    "Y": -18
                },
                {
                    "X": 2,
                    "Y": 17
                },
                {
                    "X": -2,
                    "Y": 17
                }
            ]
        }
    ]
}
```

`Frames` has one entry per frame. A still sprite has one entry. `Vertices` lists the points of the frame's polygon,
in pixels, relative to the origin. Values can have decimals: `--fit outer` puts vertices where sides meet, and a
`center` origin on an odd-sized frame gives half pixels.

## Library

The library takes raw RGBA bytes. It does not decode images. This example uses
[StbImageSharp](https://www.nuget.org/packages/StbImageSharp) to decode the PNG:

```csharp
using System.Numerics;
using ColliderGen;
using StbImageSharp;

using var stream = File.OpenRead("sprites/ship.png");
ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

SpriteCollider collider = SpriteColliderGenerator.Generate(
    new SpriteImage(image.Data, image.Width, image.Height),
    new ColliderOptions { AlphaThreshold = 128, Epsilon = 0f, MaxVertices = 16, Origin = ColliderOrigin.Center,
                          Fit = ColliderFit.Inner });

// One hull per animation frame, in frame-local pixel coordinates.
IReadOnlyList<Vector2> firstFrame = collider.Frames[0];
```

`Fit` works like `--fit`. `FrameWidth` is optional. If you leave it out, strips of square frames are found as described
above, and any other image is one frame. `collider.FrameWidth` and `collider.Origin` tell you what was used.
`StripLayout.Resolve(width, height, frameWidth)` uses the same rule and returns each frame's pixel offset and origin.

## Reference

- [Glossary](GLOSSARY.md) - Terms used in the code, tests and this README
- [Visvalingam-Whyatt algorithm](https://en.wikipedia.org/wiki/Visvalingam%E2%80%93Whyatt_algorithm) - Polygon simplification
- [Convex hull algorithms](https://en.wikipedia.org/wiki/Convex_hull_algorithms) - Monotone chain

## License

MIT, see [LICENSE](LICENSE).
