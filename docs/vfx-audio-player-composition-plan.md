# VfxAudioPlayer Composition Plan

## Goal

Replace the legacy `VfxAudioPlayer` monolith with a game-level composite built
from smaller framework nodes and a thin layer of gameplay logic.

This note captures the current agreed direction before implementation.

## Current Problem

`VfxAudioPlayer` currently mixes several unrelated responsibilities:

- asset path discovery
- atlas frame loading
- txt timing parsing
- audio transport and playback state
- talking-head frame selection
- fuzz overlay frame stepping
- border layout rules
- final drawing

That makes it too specialized to move into `Framework` as a single reusable
component.

## Proposed Scene Composition

The replacement should be a game-specific composite node, not a framework-owned
all-in-one player.

Proposed structure:

- `TalkingHeadPlayer : Node2D`
  - `TalkingFace : AnimatedSprite`
  - `FuzzOverlay : AnimatedSprite` or region-addressable sprite
  - `BorderOverlay : Sprite`
  - `VoiceAudio : AudioPlayer`

## Responsibility Split

### `TalkingHeadPlayer`

Game-level composite root.

Owns:

- child node wiring
- clip assignment
- play / stop surface API
- synchronization of visual state from audio time
- optional enable/disable behavior for border and fuzz

Should not become a generic framework node.

### `TalkingFace`

Framework candidate.

Purpose:

- display one atlas-backed frame at a time
- support runtime frame/region switching
- later support broader animated sprite use cases beyond this talking-head case

This is the strongest extraction candidate from the legacy component.

### `FuzzOverlay`

Likely uses the same underlying capability as `TalkingFace`:

- one texture
- multiple addressable subregions
- runtime stepping through those regions

If `AnimatedSprite` is designed cleanly enough, the fuzz overlay can reuse it.
If not, it still needs a region-addressable sprite primitive rather than a plain
static sprite.

### `BorderOverlay`

Simple sprite layer.

It remains game-specific because the current border sizing rules are tied to the
legacy portrait dimensions and insets.

### `VoiceAudio`

Uses the existing framework `AudioPlayer`.

The legacy class should stop owning raw raylib `Music` directly and instead
delegate transport concerns to the framework audio node.

## Data and Logic Outside the Scene Tree

### TXT Timing Parser

Must stay out of `Framework`.

Reason:

- it is content-format specific
- it encodes gameplay/data conventions
- it should not define a reusable framework contract

This becomes a game/content-side parser that produces timeline data for the
composite player.

### Talking Head Clip Data

Recommended game-level data object or resource:

- face atlas image
- atlas frame metadata
- audio file
- timing rows
- optional fuzz sheet
- optional border texture/atlas
- legacy border layout constants if still needed

This keeps asset composition rules out of framework nodes.

## Framework Boundary

### Good Framework Candidates

- `AnimatedSprite`
- small reusable atlas-region support if needed by `AnimatedSprite`
- generic frame/region selection helpers only if they can be defined without
  talking-head-specific assumptions

### Not Good Framework Candidates

- `VfxAudioPlayer` as a monolithic node
- txt parsing for `m01bl03` style files
- asset search/path probing
- legacy talking-head border inset math
- clip conventions tied to one asset family
- fuzz overlay behavior as a built-in framework feature

## Mapping From Legacy `VfxAudioPlayer`

Legacy responsibility to new owner:

- asset discovery -> game/content loading layer
- frame atlas data -> clip resource + `AnimatedSprite`
- audio playback -> `AudioPlayer`
- timing-to-frame mapping -> gameplay/controller logic
- fuzz stepping -> fuzz visual node driven by controller logic
- border rendering -> border sprite + game-specific layout rules
- draw orchestration -> composite node hierarchy

## Implementation Order

Recommended order:

1. define the `AnimatedSprite` proposal
2. define the game-level clip/timeline data shape
3. build `TalkingHeadPlayer : Node2D`
4. wire `AudioPlayer` into the composite
5. migrate talking-face frame switching
6. migrate fuzz overlay stepping
7. migrate border overlay and layout rules
8. remove the legacy monolith after parity is verified

## Current Conclusion

The agreed direction is:

- keep the talking-head player as a game-level composite
- promote only the genuinely reusable visual primitive into `Framework`
- keep txt parsing and legacy asset semantics outside framework
- reuse the existing framework audio node rather than duplicating transport

