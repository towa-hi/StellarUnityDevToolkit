#![no_std]
use soroban_sdk::{*};

#[contracterror]
#[derive(Copy, Clone, Debug, Eq, PartialEq, PartialOrd, Ord)]
pub enum Error {
    InvalidArgs = 1,
    AlreadyExists = 2,
}

#[contracttype]
#[derive(Clone, Debug, Eq, PartialEq)]
pub struct Player {
    pub name: String,
    pub score: u32,
    pub active: bool,
}

#[contracttype]
#[derive(Clone, Debug, Eq, PartialEq)]
pub struct Inventory {
    pub owner: Player,
    pub items: Vec<String>,
    pub quantities: Map<String, u32>,
}

#[contracttype]
#[derive(Clone, Debug, Eq, PartialEq)]
pub enum DataKey {
    Player(Address),
}

#[contract]
pub struct Contract;

#[contractimpl]
impl Contract {
    pub fn hello(e: &Env, to: String) -> Vec<String> {
        vec![&e, String::from_str(&e, "Hello"), to]
    }

    pub fn register_player(e: &Env, address: Address, name: String) -> Result<Player, Error> {
        let player = Player {
            name,
            score: 0,
            active: true,
        };
        let player_key = DataKey::Player(address);
        let persistent = e.storage().persistent();
        if persistent.has(&player_key) {
            return Err(Error::AlreadyExists);
        }
        persistent.set(&player_key, &player);
        Ok(player)
    }
    // --- Primitive round-trips ---

    pub fn echo_u32(_e: &Env, val: u32) -> u32 {
        val
    }

    pub fn echo_i32(_e: &Env, val: i32) -> i32 {
        val
    }

    pub fn echo_u64(_e: &Env, val: u64) -> u64 {
        val
    }

    pub fn echo_bool(_e: &Env, val: bool) -> bool {
        val
    }

    pub fn echo_string(_e: &Env, val: String) -> String {
        val
    }

    pub fn echo_bytes(_e: &Env, val: Bytes) -> Bytes {
        val
    }

    // --- Arithmetic to verify values survive the round-trip ---

    pub fn add_u32(_e: &Env, a: u32, b: u32) -> u32 {
        a + b
    }

    pub fn add_i32(_e: &Env, a: i32, b: i32) -> i32 {
        a + b
    }

    pub fn negate_i32(_e: &Env, val: i32) -> i32 {
        -val
    }

    // --- Vec ---

    pub fn echo_vec(_e: &Env, vals: Vec<u32>) -> Vec<u32> {
        vals
    }

    pub fn sum_vec(_e: &Env, vals: Vec<u32>) -> u32 {
        let mut total: u32 = 0;
        for v in vals.iter() {
            total += v;
        }
        total
    }

    pub fn make_vec(e: &Env, a: u32, b: u32, c: u32) -> Vec<u32> {
        vec![e, a, b, c]
    }

    // --- Map ---

    pub fn echo_map(_e: &Env, m: Map<String, u32>) -> Map<String, u32> {
        m
    }

    pub fn map_get(_e: &Env, m: Map<String, u32>, key: String) -> u32 {
        m.get(key).unwrap_or(0)
    }

    pub fn make_map(e: &Env, keys: Vec<String>, vals: Vec<u32>) -> Map<String, u32> {
        let mut m = Map::new(e);
        for i in 0..keys.len() {
            m.set(keys.get(i).unwrap(), vals.get(i).unwrap());
        }
        m
    }

    // --- Struct (serializes as SCMap with symbol keys) ---

    pub fn echo_player(_e: &Env, p: Player) -> Player {
        p
    }

    pub fn make_player(_e: &Env, name: String, score: u32) -> Player {
        Player {
            name,
            score,
            active: true,
        }
    }

    pub fn player_name(_e: &Env, p: Player) -> String {
        p.name
    }

    pub fn player_score(_e: &Env, p: Player) -> u32 {
        p.score
    }

    // --- Nested struct ---

    pub fn echo_inventory(_e: &Env, inv: Inventory) -> Inventory {
        inv
    }

    pub fn make_inventory(e: &Env, owner_name: String, items: Vec<String>) -> Inventory {
        let mut quantities = Map::new(e);
        for i in 0..items.len() {
            quantities.set(items.get(i).unwrap(), (i + 1) as u32);
        }
        let owner = Player {
            name: owner_name,
            score: 0,
            active: true,
        };
        Inventory {
            owner,
            items,
            quantities,
        }
    }

    // --- Bytes utilities ---

    pub fn bytes_len(_e: &Env, data: Bytes) -> u32 {
        data.len()
    }

    pub fn concat_bytes(e: &Env, a: Bytes, b: Bytes) -> Bytes {
        let mut result = Bytes::new(e);
        result.append(&a);
        result.append(&b);
        result
    }

    // --- Multi-return via Vec<String> (for easy C# deserialization) ---

    pub fn describe_player(e: &Env, p: Player) -> Vec<String> {
        let status = if p.active {
            String::from_str(e, "active")
        } else {
            String::from_str(e, "inactive")
        };
        vec![e, p.name, status]
    }

    // --- Edge cases ---

    pub fn echo_u32_zero(_e: &Env) -> u32 {
        0
    }

    pub fn echo_empty_vec(e: &Env) -> Vec<u32> {
        Vec::new(e)
    }

    pub fn echo_empty_string(e: &Env) -> String {
        String::from_str(e, "")
    }

    pub fn echo_true(_e: &Env) -> bool {
        true
    }

    pub fn echo_false(_e: &Env) -> bool {
        false
    }

    pub fn echo_max_u32(_e: &Env) -> u32 {
        u32::MAX
    }

    pub fn echo_min_i32(_e: &Env) -> i32 {
        i32::MIN
    }

    pub fn echo_max_u64(_e: &Env) -> u64 {
        u64::MAX
    }
}

mod test;
