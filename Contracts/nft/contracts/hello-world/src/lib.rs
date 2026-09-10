// SPDX-License-Identifier: MIT
// Compatible with OpenZeppelin Stellar Soroban Contracts ^0.7.1
#![no_std]

use soroban_sdk::{
    Address, BytesN, contract, contractimpl, contractmeta, contracttype, Env, String,
};
use stellar_access::ownable::{self as ownable, Ownable};
use stellar_contract_utils::upgradeable::{self as upgradeable, Upgradeable};
use stellar_macros::only_owner;
use stellar_tokens::non_fungible::{
    enumerable::{Enumerable, NonFungibleEnumerable},
    Base, NonFungibleToken,
};

#[cfg(test)]
mod test;

contractmeta!(key = "security_contact", val = "mono@scryingst.one");

const POINT_50: u32 = 50;
const POINT_100: u32 = 100;
const POINT_500: u32 = 500;
const TOKEN_ID_STRIDE: u32 = 1000;

#[contracttype]
enum DataKey {
    Uri50,
    Uri100,
    Uri500,
    Minter,
}

#[contract]
pub struct UnityToken;

fn require_valid_points(points: u32) {
    if points != POINT_50 && points != POINT_100 && points != POINT_500 {
        panic!("invalid asset points");
    }
}

fn points_from_token_id(token_id: u32) -> u32 {
    let points = token_id % TOKEN_ID_STRIDE;
    require_valid_points(points);
    points
}

fn uri_key(points: u32) -> DataKey {
    match points {
        POINT_50 => DataKey::Uri50,
        POINT_100 => DataKey::Uri100,
        POINT_500 => DataKey::Uri500,
        _ => panic!("invalid asset points"),
    }
}

fn uri_for_points(e: &Env, points: u32) -> String {
    e.storage().instance().get(&uri_key(points)).unwrap()
}

fn require_minter_or_owner(e: &Env) {
    let owner = ownable::get_owner(e).expect("owner should be set");
    if let Some(minter) = e.storage().instance().get::<_, Address>(&DataKey::Minter) {
        minter.require_auth();
    } else {
        owner.require_auth();
    }
}

#[contractimpl]
impl UnityToken {
    pub fn __constructor(
        e: &Env,
        owner: Address,
        uri_50: String,
        uri_100: String,
        uri_500: String,
    ) {
        let name = String::from_str(e, "UnityToken");
        let symbol = String::from_str(e, "SCRYX");
        Base::set_metadata(e, uri_50.clone(), name, symbol);
        e.storage().instance().set(&DataKey::Uri50, &uri_50);
        e.storage().instance().set(&DataKey::Uri100, &uri_100);
        e.storage().instance().set(&DataKey::Uri500, &uri_500);
        ownable::set_owner(e, &owner);
    }

    #[only_owner]
    pub fn set_minter(e: &Env, minter: Address) {
        e.storage().instance().set(&DataKey::Minter, &minter);
    }

    pub fn mint(e: &Env, to: Address, points: u32) -> u32 {
        require_minter_or_owner(e);
        require_valid_points(points);
        let seq = Enumerable::total_supply(e);
        let token_id = seq
            .checked_mul(TOKEN_ID_STRIDE)
            .unwrap()
            .checked_add(points)
            .unwrap();
        Enumerable::non_sequential_mint(e, &to, token_id);
        token_id
    }

    pub fn asset_points(e: &Env, token_id: u32) -> u32 {
        let _ = Base::owner_of(e, token_id);
        points_from_token_id(token_id)
    }
}

#[contractimpl(contracttrait)]
impl NonFungibleToken for UnityToken {
    type ContractType = Enumerable;

    fn token_uri(e: &Env, token_id: u32) -> String {
        let points = UnityToken::asset_points(e, token_id);
        uri_for_points(e, points)
    }
}

//
// Extensions
//

#[contractimpl(contracttrait)]
impl NonFungibleEnumerable for UnityToken {}

//
// Utils
//

#[contractimpl(contracttrait)]
impl Ownable for UnityToken {}

#[contractimpl]
impl Upgradeable for UnityToken {
    #[only_owner]
    fn upgrade(e: &Env, new_wasm_hash: BytesN<32>, _operator: Address) {
        upgradeable::upgrade(e, &new_wasm_hash);
    }
}
