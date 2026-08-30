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
- `GameState`: gameplay phase enum (`NotStarted`, `WaitingForDrag`, `DraggingShape`, `ResolvingPlacement`, `GameOver`).
- `GameController`: single owner of gameplay logic (placement, line clear, scoring, and game-over checks).
- `BoardState`: immutable 8x8 occupancy snapshot packed into a `ulong`, used for placement and line-clear queries.
- `PlacementResolution`: result of testing a placement against a `BoardState`.
- `IntegerRng`: deterministic xorshift32 generator, so a game can be replayed from its seed.

## ShapeTray Lifecycle (Authoritative)

- Idle: each `ShapeTray` is snapped and scaled to its owning `ShapeOfferSlot`.
- Drag: the `ShapeTray` itself is what follows the mouse.
- Valid drop on board:
  - placement commits board occupancy,
  - `ShapeTile` children are reparented from the tray container onto their destination `BoardCell`,
  - the placed `ShapeTray` container is destroyed.
- Refill: once every `ShapeOfferSlot` is empty, the preview batch is promoted into the
  offer slots and a fresh preview batch is spawned behind it.
- Invalid drop: active `ShapeTray` snaps back to its `ShapeOfferSlot`.

## Existing Script Mapping

The migration is complete and the old compatibility shims (`Slot`, `Holder`, `ShapeSlot`,
`Shape`, `ShapeInstance`) have been deleted. Canonical names only:

- `Slot` -> `BoardCell`.
- `Holder` -> `ShapeOfferArea`.
- `ShapeSlot` -> `ShapeOfferSlot`.
- `Shape` / `ShapeInstance` -> `ShapeTray`.
- `Tile` remains `Tile` and represents visual cube tiles.
- `Board` remains `Board` and is the canonical board root.

## Scene Object Naming

- `BoardRoot`
- `ShapeTrayRoot`
- `GameplayUIRoot`

## Gameplay Constants

- `BlockBlastConstants.BoardSize = 8`
- `BlockBlastConstants.TraySize = 3`
- `ShapeDefinition.GridSize = 5` (the max shape bounds)

## Implementation Checklist

- [x] Milestone 1: naming locked.
- [x] Milestone 2: drag and drop shape placement.
- [x] Milestone 3: line clear and scoring.
- [x] Milestone 4: game-over check when no valid placements remain.
