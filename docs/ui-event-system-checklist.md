# UI Event System Checklist

Related proposal: [ui-event-consumption-proposal.md](./ui-event-consumption-proposal.md)

This file is the in-repo checklist for the UI event-system migration. Update it
when implementation moves, so the current state does not depend on chat history.

## Current Status

- [x] Keep the experimental dispatcher seam in `Core.UI`
- [x] Promote base routed pointer event contracts into `Framework`
- [x] Add a framework-owned per-frame UI input snapshot in `InputManager`
- [x] Route one real framework control (`ButtonNode`) through dispatched pointer events
- [x] Preserve `ButtonNode.HandleInput()` as a compatibility shim for existing scenes
- [x] Switch `ControlsScene` to dispatcher-driven button clicks
- [x] Support `Down` and `Up` routing for left, middle, and right mouse buttons
- [x] Preserve `Click` routing when press and release resolve to the same target
- [x] Convert `SliderNode` to routed pointer events
- [x] Add framework-owned pointer capture for drag interactions
- [x] Switch one slider-heavy scene (`VideoPlayerScene`) to dispatcher-driven controls
- [x] Convert `DropdownNode` to routed pointer events
- [x] Switch one dropdown consumer (`AudioPlayerScene` backend selector) to dispatcher-driven selection

## Next Slice

- [ ] Convert `ListViewNode` to routed pointer events
- [ ] Move more test scenes off manual control polling

## Later Work

- [ ] Add focused keyboard/gamepad UI event routing
- [ ] Add framework-owned focus state
- [ ] Add modal/input-scope blocking
- [ ] Remove public polling as the primary UI integration path

## Notes

- `UiEventSource` is still the active dispatcher shape and remains the current
  experiment seam.
- `HotRectEventScene` remains the debug scene for routed pointer-event traces.
- Right and middle mouse button support currently covers routed `Down`, `Up`,
  and same-target `Click` semantics.
- Pointer capture is now dispatcher-owned for routed drag interactions and is
  currently exercised by `SliderNode`.
- Expanded dropdown interaction is currently dispatcher-driven and consumes the
  next pointer press while open, which is acceptable for the current popup-like
  behavior until broader modal/input-scope work lands.
