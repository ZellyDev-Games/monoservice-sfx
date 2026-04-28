# SFX Monoservice

A lightweight Unity sound effects service for projects using [`ZellyDevGames.Monoservice`](https://github.com/ZellyDev-Games/monoservice).

`SFX` provides pooled `AudioSource` playback for 2D and 3D sound effects, with per-clip concurrency limits to prevent audio spam.

## Features

- 2D sound effect playback
- 3D positional sound effect playback
- `AudioSource` object pooling
- Per-clip simultaneous instance limits
- Optional global per-clip fallback limit
- Monoservice integration through the `[Service]` attribute

## Installation

### Unity Package Manager

Add the package from Git URL:

```text
https://github.com/ZellyDev-Games/monoservice-sfx.git
```

Or add it manually to your Unity project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.github.zellydev-games.audio.sfx": "https://github.com/ZellyDev-Games/monoservice-sfx.git"
  }
}
```

## Dependencies

This package depends on:

```json
"com.github.zellydev-games.monoservice": "1.0.0"
```

The package is configured for Unity `6000.3`.

## Setup

Create a GameObject in your startup scene and add the `SFX` component.

Configure the component in the Inspector:

| Field | Description |
|---|---|
| `Audio Source Pool Size` | Number of pooled `AudioSource` objects created on startup. This is the maximum number of SFX that can physically play at the same time. |
| `Global Clip Limit` | Default maximum number of simultaneous instances allowed for any single clip key. Set to `0` or less to disable the global per-clip limit. |

At runtime, the service creates child GameObjects named `SFXSource_0`, `SFXSource_1`, etc. Each child has an `AudioSource` that is reused for playback.

## SFXEntry

Sound effects are represented by `SFXEntry`:

```csharp
using System;
using UnityEngine;

namespace ZellyDevGames.Audio
{
    [Serializable]
    public class SFXEntry
    {
        public string key;
        public AudioClip clip;
        public float spatialBlend;
        public float volume;
        public float simultaneousInstanceLimit;
    }
}
```

| Field | Description |
|---|---|
| `key` | Unique identifier used for clip limiting. Entries with the same key share the same active instance count. |
| `clip` | The `AudioClip` to play. |
| `spatialBlend` | Spatial blend used for 3D playback. `0` is fully 2D; `1` is fully 3D. |
| `volume` | Default playback volume used when no override is provided. |
| `simultaneousInstanceLimit` | Per-entry simultaneous instance cap. Set to `0` or less to disable the entry-specific limit. |

## Usage

Import the namespace:

```csharp
using ZellyDevGames.Audio;
```

### Play a 2D sound

```csharp
[SerializeField] private SFXEntry confirmSound;
[SerializeField] private SFX sfx;

public void Confirm()
{
    sfx.Play2D(confirmSound);
}
```

With a volume override:

```csharp
sfx.Play2D(confirmSound, 0.5f);
```

### Play a 3D positional sound

```csharp
[SerializeField] private SFXEntry explosionSound;
[SerializeField] private SFX sfx;

public void Explode(Vector3 position)
{
    sfx.Play3D(explosionSound, position);
}
```

With a volume override:

```csharp
sfx.Play3D(explosionSound, transform.position, 0.75f);
```

## Playback Behavior

When `Play2D` or `Play3D` is called:

1. The service validates the `SFXEntry` and its `AudioClip`.
2. The service checks the active instance count for the entry's `key`.
3. Playback is skipped if the entry-specific or global per-clip limit has been reached.
4. The service attempts to pop an `AudioSource` from the pool.
5. If the pool is empty, playback is skipped and the clip count is rolled back.
6. The clip is assigned to the source and played.
7. A coroutine waits until playback finishes.
8. The source is reset and returned to the pool.

## 2D vs 3D Playback

`Play2D` forces the source to fully 2D audio:

```csharp
source.spatialBlend = 0f;
```

`Play3D` positions the pooled source in world space and uses the entry's configured `spatialBlend`:

```csharp
sourceObject.transform.position = position;
source.spatialBlend = entry.spatialBlend;
```

## Clip Limiting

This package uses `SFXEntry.key` to track how many instances of a sound are currently playing.

There are two limit layers:

1. `SFXEntry.simultaneousInstanceLimit`
2. `SFX.globalClipLimit`

If both are greater than zero, the stricter limit wins.

Example:

```text
globalClipLimit = 4
entry.simultaneousInstanceLimit = 2
```

The sound can play at most 2 simultaneous instances.

```text
globalClipLimit = 4
entry.simultaneousInstanceLimit = 0
```

The sound can play at most 4 simultaneous instances.

```text
globalClipLimit = 0
entry.simultaneousInstanceLimit = 0
```

The sound has no per-clip instance limit, but it is still constrained by the total `AudioSource` pool size.

## Pool Size Guidance

Choose `audioSourcePoolSize` based on the maximum number of sound effects you expect to overlap at once.

For example:

- UI-heavy game: `8`-`16`
- Small action game: `16`-`32`
- SFX-dense combat game: `32`+

If all pooled sources are busy, additional playback requests are ignored.

## Notes and Limitations

- This service does not currently expose mixer group, pitch randomization, rolloff, min distance, max distance, or priority settings through `SFXEntry`.

## License

MIT License. See `LICENSE` for details.
