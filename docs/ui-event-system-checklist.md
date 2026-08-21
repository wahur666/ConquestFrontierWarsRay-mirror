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

## Next Slice

- [ ] Convert `SliderNode` to routed pointer events
- [ ] Add framework-owned pointer capture for drag interactions
- [ ] Convert `DropdownNode` to routed pointer events
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
  and same-target `Click` semantics. Pointer capture is still pending.
