# Licenses and provenance

## Included in this repository

- Original AFTERTIDE C# code, shaders, procedural mesh definitions, UI, English
  quest text, gameplay configuration and SVG icon: Apache License 2.0.
- `Aftertide/Data/geography.json`: processed Natural Earth admin-0 data at
  1:10 million scale; **public domain**. Source commit
  `9380cca83db5f9aef52d5e762765100745f84b27`. The file records exact raw URL,
  source SHA256, clipping bounds, projection and the limits of local elevation.
  [Natural Earth terms](https://www.naturalearthdata.com/about/terms-of-use/).

## External build/runtime dependencies

Godot Engine/Godot .NET SDK: MIT and the engine's bundled third-party notices.
.NET SDK/runtime: Microsoft/.NET license notices. These components are not
relicensed by AFTERTIDE. Engine binaries and model weights are not vendored here.
If distributing an executable, include the notices of the exact engine/runtime.
NVIDIA-fork/Streamline binaries need their own reviewed distribution terms.

## Design references, not copied code or content

Unknown Horizons (GPL-2.0-or-later code, content-specific licenses) and
Cataclysm-DDA (project CC-BY-SA-3.0 plus component exceptions) were inspected for
event-driven objectives and survival/data separation. No source, scenario text,
recipes, artwork, font or audio from those games is included in this repository.
Wall World and Frostpunk are commercial gameplay references only.

No proprietary installed-game assets, generated AI images, third-party audio or
external font files are included. System/engine fallback fonts retain engine terms.
The Apache license choice applies to the original included work, not to those
reference games or any future asset pack.
