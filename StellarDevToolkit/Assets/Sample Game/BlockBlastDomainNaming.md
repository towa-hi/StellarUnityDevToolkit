# Block Blast Domain Naming Spec

This document locks naming for the `Sample Game` Block Blast implementation.

## Canonical Glossary

- `Board`: 8x8 play grid root that owns board cells.
- `BoardCell`: one coordinate on the board and its occupancy state.
- `CellCoord`: coordinate value, represented with `Vector2Int`.
- `ShapeDefinition`: immutable shape layout as tile offsets in a max `5x5` local bounds.
- `ShapeTray`: runtime draggable prefab spawned from a `ShapeDefinition`; it is a container for a local `5x5` tile layout.
- `ShapeTile`: visual cube belonging to a `ShapeTray` (implemented with `Tile`).
- `ShapeOfferSlot`: one selection anchor that holds one `ShapeTray` when idle.
- `ShapeOfferArea`: logical collection of the 3 `ShapeOfferSlot` anchors.
- `GameState`: gameplay phase enum (`WaitingForDrag`, `DraggingShape`, `ResolvingPlacement`, `GameOver`).
- `BlockBlastGameController`: single owner of gameplay logic (placement, line clear, scoring, and game-over checks).

## ShapeTray Lifecycle (Authoritative)

- Idle: each `ShapeTray` is snapped and scaled to its owning `ShapeOfferSlot`.
- Drag: the `ShapeTray` itself is what follows the mouse.
- Valid drop on board:
  - placement commits board occupancy,
  - `ShapeTile` children are detached from the tray container,
  - detached tiles lerp to destination `BoardCell` visuals,
  - the placed `ShapeTray` container is destroyed.
- Refill: when a `ShapeOfferSlot` becomes empty, spawn a new `ShapeTray` into that slot.
- Invalid drop: active `ShapeTray` snaps back to its `ShapeOfferSlot`.

## Existing Script Mapping

- `Slot` -> `BoardCell` (kept as compatibility shim).
- `Holder` -> `ShapeOfferArea` (legacy compatibility naming).
- `ShapeSlot` -> `ShapeOfferSlot` (kept as compatibility shim).
- `Shape` / `ShapeInstance` -> `ShapeTray` runtime prefab concept.
- `Tile` remains `Tile` and represents visual cube tiles.
- `Board` remains `Board` and is the canonical board root.

## Scene Object Naming

- `BoardRoot`
- `ShapeTrayRoot`
- `GameplayUIRoot`

## Gameplay Constants

- `BoardSize = 8`
- `MaxShapeBounds = 5`
- `TraySize = 3`

## Implementation Checklist

- [x] Milestone 1: naming locked.
- [ ] Milestone 2: drag and drop shape placement.
- [ ] Milestone 3: line clear and scoring.
- [ ] Milestone 4: game-over check when no valid placements remain.
