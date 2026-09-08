use crate::{unpack_move, Error, GameLog, Move, PackedMoves, ShapeCatalogs};
use soroban_sdk::{Bytes, Env, Vec};

const BOARD_SIZE: i32 = 8;
const SHAPE_GRID_SIZE: i32 = 5;
const TRAY_SIZE: usize = 3;
const PIVOT_OFFSET: i32 = 2;
const FOOTPRINT_BIT_COUNT: u32 = 25;
const FOOTPRINT_MASK: u32 = (1 << FOOTPRINT_BIT_COUNT) - 1;
const BASE_POINTS_PER_LINE: i32 = 10;
const STREAK_SOFTENER: i32 = 4;
const MAX_STREAK_BONUS_PERCENT: i32 = 150;
const RNG_FALLBACK_SEED: u32 = 0x9E3779B9;
const MAX_REPLAY_MOVES: u32 = 10_000;

// Board row 0 and board column 0 as cell bitmasks; shift by y * BOARD_SIZE or by
// x to reach any other line.
const ROW_MASK: u64 = (1u64 << BOARD_SIZE) - 1;
const COL_MASK: u64 = {
    let mut mask = 0u64;
    let mut y = 0;
    while y < BOARD_SIZE {
        mask |= 1u64 << (y * BOARD_SIZE);
        y += 1;
    }
    mask
};

const TROMINO_PACKED_SHAPES: [u32; 6] = [
    14336, 135296, 143360, 12416, 6272, 137216,
];

const TETROMINO_PACKED_SHAPES: [u32; 19] = [
    30720, 4329600, 405504, 145408, 143488, 14464, 137344, 208896, 274560, 399360,
    143616, 79872, 397440, 14592, 135360, 276480, 135552, 14400, 200832,
];

struct IntegerRng {
    state: u32,
}

impl IntegerRng {
    fn new(seed: u32) -> Self {
        Self {
            state: if seed == 0 { RNG_FALLBACK_SEED } else { seed },
        }
    }

    fn next_u32(&mut self) -> u32 {
        let mut x = self.state;
        if x == 0 {
            x = RNG_FALLBACK_SEED;
        }
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        self.state = x;
        x
    }

    fn next_index(&mut self, count: i32) -> i32 {
        if count <= 0 {
            return 0;
        }
        (self.next_u32() % count as u32) as i32
    }
}

struct Sim {
    board: u64,
    rng: IntegerRng,
    offer: [Option<u32>; TRAY_SIZE],
    preview: [Option<u32>; TRAY_SIZE],
    score: u32,
    streak: i32,
}

impl Sim {
    fn new(seed: u64) -> Self {
        let mut rng = IntegerRng::new(seed as u32);
        let offer = generate_batch(&mut rng, TRAY_SIZE);
        let preview = generate_batch(&mut rng, TRAY_SIZE);
        Self {
            board: 0,
            rng,
            offer,
            preview,
            score: 0,
            streak: 0,
        }
    }

    fn apply_move(&mut self, mv: &Move) -> Result<(), Error> {
        let slot = mv.tray as usize;
        if slot >= TRAY_SIZE {
            return Err(Error::InvalidGameLog);
        }
        let shape = self.offer[slot].ok_or(Error::InvalidGameLog)?;
        let anchor_x = mv.x - PIVOT_OFFSET;
        let anchor_y = mv.y - PIVOT_OFFSET;
        if !can_place(self.board, shape, anchor_x, anchor_y) {
            return Err(Error::InvalidGameLog);
        }

        let mut placed_tiles = 0u64;
        for_each_tile(shape, |lx, ly| {
            placed_tiles |= cell_mask(anchor_x + lx, anchor_y + ly);
            true
        });

        let placed = self.board | placed_tiles;
        let (cleared_lines, clear_mask) = completed_lines(placed, placed_tiles);
        self.board = placed & !clear_mask;
        self.score = self
            .score
            .saturating_add(placement_score(cleared_lines, self.streak));
        self.streak = if cleared_lines > 0 { self.streak + 1 } else { 0 };
        self.offer[slot] = None;
        if self.offer.iter().all(|shape| shape.is_none()) {
            self.promote_preview();
        }
        Ok(())
    }

    fn promote_preview(&mut self) {
        for i in 0..TRAY_SIZE {
            if self.preview[i].is_some() {
                self.offer[i] = self.preview[i].take();
            }
        }
        fill_empty(&mut self.rng, &mut self.offer);
        fill_empty(&mut self.rng, &mut self.preview);
    }

    #[cfg(test)]
    fn first_valid_move(&self) -> Option<Move> {
        for (slot, shape) in self.offer.iter().enumerate() {
            let Some(shape) = shape else {
                continue;
            };
            for y in 0..BOARD_SIZE {
                for x in 0..BOARD_SIZE {
                    let ax = x - PIVOT_OFFSET;
                    let ay = y - PIVOT_OFFSET;
                    if can_place(self.board, *shape, ax, ay) {
                        return Some(Move {
                            x,
                            y,
                            tray: slot as u32,
                        });
                    }
                }
            }
        }
        None
    }
}

fn generate_batch(rng: &mut IntegerRng, count: usize) -> [Option<u32>; TRAY_SIZE] {
    let mut out = [None; TRAY_SIZE];
    if count == 0 {
        return out;
    }
    let tromino_slot = rng.next_index(count as i32) as usize;
    for i in 0..count {
        let pool = if i == tromino_slot {
            TROMINO_PACKED_SHAPES.as_slice()
        } else {
            TETROMINO_PACKED_SHAPES.as_slice()
        };
        out[i] = Some(pool[rng.next_index(pool.len() as i32) as usize]);
    }
    out
}

fn fill_empty(rng: &mut IntegerRng, slots: &mut [Option<u32>; TRAY_SIZE]) {
    let mut empties = [0usize; TRAY_SIZE];
    let mut n = 0;
    for i in 0..TRAY_SIZE {
        if slots[i].is_none() {
            empties[n] = i;
            n += 1;
        }
    }
    if n == 0 {
        return;
    }
    let batch = generate_batch(rng, n);
    for j in 0..n {
        slots[empties[j]] = batch[j];
    }
}

fn for_each_tile(shape: u32, mut visit: impl FnMut(i32, i32) -> bool) -> bool {
    let footprint = shape & FOOTPRINT_MASK;
    for y in 0..SHAPE_GRID_SIZE {
        for x in 0..SHAPE_GRID_SIZE {
            let bit = y * SHAPE_GRID_SIZE + x;
            if footprint & (1 << bit) != 0 && !visit(x, y) {
                return false;
            }
        }
    }
    true
}

fn in_bounds(x: i32, y: i32) -> bool {
    x >= 0 && x < BOARD_SIZE && y >= 0 && y < BOARD_SIZE
}

fn cell_mask(x: i32, y: i32) -> u64 {
    1u64 << (y * BOARD_SIZE + x)
}

fn can_place(board: u64, shape: u32, anchor_x: i32, anchor_y: i32) -> bool {
    for_each_tile(shape, |lx, ly| {
        let tx = anchor_x + lx;
        let ty = anchor_y + ly;
        in_bounds(tx, ty) && (board & cell_mask(tx, ty)) == 0
    })
}

// Only lines containing one of the tiles just placed can have completed: every
// line that filled up on an earlier move was cleared by that move, so the rest
// of the board never holds a full line and does not need to be scanned.
fn completed_lines(board: u64, placed_tiles: u64) -> (i32, u64) {
    let mut count = 0;
    let mut clear_mask = 0u64;
    for y in 0..BOARD_SIZE {
        let row = ROW_MASK << (y * BOARD_SIZE);
        if placed_tiles & row != 0 && board & row == row {
            count += 1;
            clear_mask |= row;
        }
    }
    for x in 0..BOARD_SIZE {
        let col = COL_MASK << x;
        if placed_tiles & col != 0 && board & col == col {
            count += 1;
            clear_mask |= col;
        }
    }
    (count, clear_mask)
}

fn placement_score(cleared_lines: i32, streak: i32) -> u32 {
    if cleared_lines <= 0 {
        return 0;
    }
    let clamped_streak = streak.max(0);
    let multiplier_percent =
        100 + (MAX_STREAK_BONUS_PERCENT * clamped_streak) / (STREAK_SOFTENER + clamped_streak);
    let line_score = cleared_lines * BASE_POINTS_PER_LINE;
    ((line_score * multiplier_percent) / 100) as u32
}

pub(crate) fn pack_move(cell_x: i32, cell_y: i32, tray: u32) -> u8 {
    (tray & 0b11) as u8 | ((cell_x as u8 & 0b111) << 2) | ((cell_y as u8 & 0b111) << 5)
}

pub(crate) fn shape_catalogs(e: &Env, _seed: u64) -> ShapeCatalogs {
    let mut trominos = Vec::new(e);
    for shape in TROMINO_PACKED_SHAPES {
        trominos.push_back(shape);
    }
    let mut tetrominos = Vec::new(e);
    for shape in TETROMINO_PACKED_SHAPES {
        tetrominos.push_back(shape);
    }
    ShapeCatalogs {
        trominos,
        tetrominos,
    }
}

// Offer batches are determined by seed and move count: two batches at start,
// then one more preview batch after every three placements (2 + n/3).
#[cfg(test)]
pub(crate) fn precompute_offer_batches(seed: u64, move_count: u32) -> u32 {
    let mut rng = IntegerRng::new(seed as u32);
    let batch_count = 2 + move_count / TRAY_SIZE as u32;
    for _ in 0..batch_count {
        let _ = generate_batch(&mut rng, TRAY_SIZE);
    }
    rng.state
}

pub(crate) fn validate_game_log(game_log: &GameLog) -> Result<u32, Error> {
    let packed = &game_log.packed_moves.packed;
    let move_count = packed.len();
    if move_count > MAX_REPLAY_MOVES {
        return Err(Error::InvalidGameLog);
    }
    let mut sim = Sim::new(game_log.seed);
    for i in 0..move_count {
        let packed_byte = packed.get(i).ok_or(Error::InvalidGameLog)?;
        sim.apply_move(&unpack_move(packed_byte))?;
    }
    // A log that stops before the board is stuck is accepted: the score only ever
    // accumulates, so a short replay can only score lower than the full game.
    Ok(sim.score)
}

#[cfg(test)]
pub(crate) fn greedy_game_log(e: &Env, seed: u64) -> GameLog {
    let mut sim = Sim::new(seed);
    let mut packed = Bytes::new(e);
    let mut guard = 0u32;
    while guard < MAX_REPLAY_MOVES {
        guard += 1;
        let Some(mv) = sim.first_valid_move() else {
            break;
        };
        packed.push_back(pack_move(mv.x, mv.y, mv.tray));
        sim.apply_move(&mv).expect("greedy move must be legal");
    }
    GameLog {
        final_score: sim.score,
        packed_moves: PackedMoves { packed },
        seed,
        submitted_ledger_seq: u64::MAX,
    }
}

#[cfg(test)]
pub(crate) fn recapture_legacy_unity_log(e: &Env, seed: u64, legacy: &[u64]) -> GameLog {
    let mut sim = Sim::new(seed);
    let mut packed = Bytes::new(e);
    for old in legacy {
        let shape = *old as u32;
        let anchor_x = ((*old >> 32) as u8) as i8 as i32;
        let anchor_y = ((*old >> 40) as u8) as i8 as i32;
        let cell_x = anchor_x + PIVOT_OFFSET;
        let cell_y = anchor_y + PIVOT_OFFSET;
        let tray = sim
            .offer
            .iter()
            .position(|slot| *slot == Some(shape))
            .expect("legacy shape must still be in the offer") as u32;
        let mv = Move {
            x: cell_x,
            y: cell_y,
            tray,
        };
        packed.push_back(pack_move(cell_x, cell_y, tray));
        sim.apply_move(&mv).expect("legacy move must be legal");
    }
    GameLog {
        final_score: sim.score,
        packed_moves: PackedMoves { packed },
        seed,
        submitted_ledger_seq: 0,
    }
}

#[cfg(test)]
mod rng_tests {
    use super::IntegerRng;

    #[test]
    fn next_u32_matches_xorshift32_from_seed_one() {
        let mut rng = IntegerRng::new(1);
        assert_eq!(rng.next_u32(), 270369);

        let mut rng = IntegerRng::new(1);
        assert_eq!(rng.next_index(3), 0);
    }
}
