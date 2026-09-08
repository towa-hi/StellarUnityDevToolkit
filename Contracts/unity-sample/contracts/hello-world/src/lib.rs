#![no_std]
use soroban_sdk::{*};

mod game;

#[contracterror]
#[derive(Copy, Clone, Debug, Eq, PartialEq, PartialOrd, Ord)]
pub enum Error {
    InvalidArgs = 1,
    AlreadyExists = 2,
    NotInitialized = 3,
    AlreadyInitialized = 4,
    ListingNotFound = 5,
    ListingInactive = 6,
    NotSeller = 7,
    PaymentTokenNotAllowed = 8,
    CollectionNotAllowed = 9,
    InvalidGameLog = 10,
}

#[contracttype]
#[derive(Clone, Debug, Eq, PartialEq)]
pub struct Player {
    pub name: String,
}

#[contracttype]
#[derive(Clone, Debug, Eq, PartialEq)]
pub struct GameLog {
    pub final_score: u32,
    pub packed_moves: PackedMoves,
    pub seed: u64,
    pub submitted_ledger_seq: u64,
}

// Packed move list matching C# GameState.PackedMoves (List<byte>).
// Each byte:
//   bits 0-1: tray index 0|1|2
//   bits 2-4: drop cell x (0-7)
//   bits 5-7: drop cell y (0-7)
#[contracttype]
#[derive(Clone, Debug, Eq, PartialEq)]
pub struct PackedMoves {
    pub packed: Bytes,
}

#[contracttype]
#[derive(Clone, Debug, Eq, PartialEq)]
pub struct Move {
    pub x: i32,
    pub y: i32,
    pub tray: u32,
}

#[contracttype]
#[derive(Clone, Debug, Eq, PartialEq)]
pub struct ShapeCatalogs {
    pub trominos: Vec<u32>,
    pub tetrominos: Vec<u32>,
}

pub fn unpack_move(packed: u8) -> Move {
    Move {
        tray: (packed & 0b11) as u32,
        x: ((packed >> 2) & 0b111) as i32,
        y: ((packed >> 5) & 0b111) as i32,
    }
}

pub fn packed_moves_to_moves(e: &Env, packed_moves: PackedMoves) -> Vec<Move> {
    let mut moves = Vec::new(e);
    for packed in packed_moves.packed.iter() {
        moves.push_back(unpack_move(packed));
    }
    moves
}

#[contracttype]
#[derive(Clone, Debug, Eq, PartialEq)]
pub struct Inventory {
    pub items: Vec<String>,
    pub quantities: Map<String, u32>,
}

#[contracttype]
#[derive(Clone, Debug, Eq, PartialEq)]
pub enum DataKey {
    Player(Address),
    Admin,
    PaymentTokens,
    NftCollections,
    NextListingId,
    Listing(u32),
    ActiveListings,
    GameLog(Address), // list of logs for a player
}

#[contracttype]
#[derive(Clone, Debug, Eq, PartialEq)]
pub struct MarketSellReq {
    pub seller: Address,
    pub asset_contract: Address,
    pub asset_id: u32,
    pub price: u32,
    pub payment_token: Address,
}

#[contracttype]
#[derive(Clone, Debug, Eq, PartialEq)]
pub struct Listing {
    pub id: u32,
    pub seller: Address,
    pub asset_contract: Address,
    pub asset_id: u32,
    pub price: u32,
    pub payment_token: Address,
    pub active: bool,
}

// Minimal client for the OpenZeppelin non-fungible token (SEP-50) contract so we
// can cross-call it without importing the OZ crate.
#[contractclient(name = "NftClient")]
pub trait Nft {
    fn transfer(e: Env, from: Address, to: Address, token_id: u32);
    fn owner_of(e: Env, token_id: u32) -> Address;
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
        };
        let player_key = DataKey::Player(address);
        let persistent = e.storage().persistent();
        if persistent.has(&player_key) {
            return Err(Error::AlreadyExists);
        }
        persistent.set(&player_key, &player);
        Ok(player)
    }

    pub fn register_game_log(e: &Env, address: Address, game_log: GameLog) -> Result<(), Error> {
        address.require_auth();

        let persistent = e.storage().persistent();
        let game_log_key = DataKey::GameLog(address.clone());
        let mut game_logs: Vec<GameLog> = persistent
            .get(&game_log_key)
            .unwrap_or_else(|| Vec::new(e));

        // Replay the packed moves from the seed. final_score is untrusted
        // until this check; a mismatch or illegal log is rejected.
        let computed_score = game::validate_game_log(&game_log)?;
        if computed_score != game_log.final_score {
            return Err(Error::InvalidGameLog);
        }

        // One log per seed: a replayed seed is a duplicate submission.
        for log in game_logs.iter() {
            if log.seed == game_log.seed {
                return Err(Error::AlreadyExists);
            }
        }

        game_logs.push_back(GameLog {
            submitted_ledger_seq: e.ledger().sequence() as u64,
            ..game_log
        });
        persistent.set(&game_log_key, &game_logs);

        // Auto-register so a log always has a player record behind it.
        let player_key = DataKey::Player(address);
        if !persistent.has(&player_key) {
            let player = Player {
                name: String::from_str(e, "unnamed player"),
            };
            persistent.set(&player_key, &player);
        }

        Ok(())
    }

    pub fn get_game_logs(e: &Env, address: Address) -> Vec<GameLog> {
        e.storage()
            .persistent()
            .get(&DataKey::GameLog(address))
            .unwrap_or_else(|| Vec::new(e))
    }

    pub fn start_game_check(e: &Env, seed: u64) -> ShapeCatalogs {
        game::shape_catalogs(e, seed)
    }

    pub fn packed_moves_to_moves(e: &Env, packed_moves: PackedMoves) -> Vec<Move> {
        crate::packed_moves_to_moves(e, packed_moves)
    }

    // --- Marketplace ---

    // Configure the admin plus the allowlists of acceptable payment tokens and
    // NFT collections. Can only be set once, and requires the admin's
    // authorization so the allowlists cannot be silently hijacked.
    pub fn init_market(
        e: &Env,
        admin: Address,
        payment_tokens: Vec<Address>,
        nft_collections: Vec<Address>,
    ) -> Result<(), Error> {
        let instance = e.storage().instance();
        if instance.has(&DataKey::Admin) {
            return Err(Error::AlreadyInitialized);
        }
        admin.require_auth();
        instance.set(&DataKey::Admin, &admin);
        instance.set(&DataKey::PaymentTokens, &payment_tokens);
        instance.set(&DataKey::NftCollections, &nft_collections);
        Ok(())
    }

    // List an NFT for sale. The asset is escrowed into this contract until the
    // listing is bought or cancelled.
    pub fn list(e: &Env, req: MarketSellReq) -> Result<u32, Error> {
        if !e.storage().instance().has(&DataKey::Admin) {
            return Err(Error::NotInitialized);
        }
        if !Self::is_allowed(e, &DataKey::NftCollections, &req.asset_contract) {
            return Err(Error::CollectionNotAllowed);
        }
        if !Self::is_allowed(e, &DataKey::PaymentTokens, &req.payment_token) {
            return Err(Error::PaymentTokenNotAllowed);
        }
        req.seller.require_auth();

        // Effects: record the listing before the external escrow transfer.
        let id = Self::next_listing_id(e);
        let listing = Listing {
            id,
            seller: req.seller.clone(),
            asset_contract: req.asset_contract.clone(),
            asset_id: req.asset_id,
            price: req.price,
            payment_token: req.payment_token.clone(),
            active: true,
        };
        e.storage().persistent().set(&DataKey::Listing(id), &listing);

        let mut active = Self::active_listing_ids(e);
        active.push_back(id);
        e.storage().instance().set(&DataKey::ActiveListings, &active);

        // Interaction: pull the NFT into escrow.
        NftClient::new(e, &req.asset_contract).transfer(
            &req.seller,
            &e.current_contract_address(),
            &req.asset_id,
        );

        Ok(id)
    }

    // Buy a listed NFT: pay the seller in the payment token, then release the
    // escrowed NFT to the buyer.
    pub fn buy(e: &Env, buyer: Address, listing_id: u32) -> Result<(), Error> {
        buyer.require_auth();

        let mut listing: Listing = e
            .storage()
            .persistent()
            .get(&DataKey::Listing(listing_id))
            .ok_or(Error::ListingNotFound)?;
        if !listing.active {
            return Err(Error::ListingInactive);
        }

        // Effects: close the listing before any external transfer so a malicious
        // counterparty contract cannot re-enter and buy it twice.
        listing.active = false;
        e.storage().persistent().set(&DataKey::Listing(listing_id), &listing);
        Self::remove_active(e, listing_id);

        // Interactions: charge the buyer in the listing's payment token, then
        // release the escrowed NFT.
        token::Client::new(e, &listing.payment_token).transfer(
            &buyer,
            &listing.seller,
            &(listing.price as i128),
        );

        NftClient::new(e, &listing.asset_contract).transfer(
            &e.current_contract_address(),
            &buyer,
            &listing.asset_id,
        );

        Ok(())
    }

    // Cancel a listing and return the escrowed NFT to the seller.
    pub fn cancel(e: &Env, listing_id: u32) -> Result<(), Error> {
        let mut listing: Listing = e
            .storage()
            .persistent()
            .get(&DataKey::Listing(listing_id))
            .ok_or(Error::ListingNotFound)?;
        if !listing.active {
            return Err(Error::ListingInactive);
        }
        listing.seller.require_auth();

        // Effects: close the listing before returning the escrowed NFT.
        listing.active = false;
        e.storage().persistent().set(&DataKey::Listing(listing_id), &listing);
        Self::remove_active(e, listing_id);

        // Interaction: return the escrowed NFT to the seller.
        NftClient::new(e, &listing.asset_contract).transfer(
            &e.current_contract_address(),
            &listing.seller,
            &listing.asset_id,
        );

        Ok(())
    }

    pub fn get_listing(e: &Env, listing_id: u32) -> Option<Listing> {
        e.storage().persistent().get(&DataKey::Listing(listing_id))
    }

    pub fn get_listings(e: &Env) -> Vec<Listing> {
        let active = Self::active_listing_ids(e);
        let mut out = Vec::new(e);
        for id in active.iter() {
            if let Some(listing) = e
                .storage()
                .persistent()
                .get::<DataKey, Listing>(&DataKey::Listing(id))
            {
                out.push_back(listing);
            }
        }
        out
    }

    fn is_allowed(e: &Env, key: &DataKey, addr: &Address) -> bool {
        let allowlist: Vec<Address> = e
            .storage()
            .instance()
            .get(key)
            .unwrap_or_else(|| Vec::new(e));
        allowlist.iter().any(|a| &a == addr)
    }

    fn next_listing_id(e: &Env) -> u32 {
        let instance = e.storage().instance();
        let id: u32 = instance.get(&DataKey::NextListingId).unwrap_or(0);
        instance.set(&DataKey::NextListingId, &(id + 1));
        id
    }

    fn active_listing_ids(e: &Env) -> Vec<u32> {
        e.storage()
            .instance()
            .get(&DataKey::ActiveListings)
            .unwrap_or_else(|| Vec::new(e))
    }

    fn remove_active(e: &Env, listing_id: u32) {
        let active = Self::active_listing_ids(e);
        let mut next = Vec::new(e);
        for id in active.iter() {
            if id != listing_id {
                next.push_back(id);
            }
        }
        e.storage().instance().set(&DataKey::ActiveListings, &next);
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

    pub fn echo_inventory(_e: &Env, inv: Inventory) -> Inventory {
        inv
    }

    pub fn make_inventory(e: &Env, items: Vec<String>) -> Inventory {
        let mut quantities = Map::new(e);
        for i in 0..items.len() {
            quantities.set(items.get(i).unwrap(), (i + 1) as u32);
        }
        Inventory {
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
