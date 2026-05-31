// SPDX-License-Identifier: MIT
// Compatible with OpenZeppelin Stellar Soroban Contracts ^0.7.1
#![no_std]

use soroban_sdk::{Address, BytesN, contract, contractimpl, contractmeta, Env, String};
use stellar_access::ownable::{self as ownable, Ownable};
use stellar_contract_utils::upgradeable::{self as upgradeable, Upgradeable};
use stellar_macros::only_owner;
use stellar_tokens::non_fungible::{
    Base, enumerable::{NonFungibleEnumerable, Enumerable}, NonFungibleToken
};

contractmeta!(key="security_contact", val="mono@scryingst.one");

#[contract]
pub struct UnityToken;

#[contractimpl]
impl UnityToken {
    pub fn __constructor(e: &Env, owner: Address) {
        let uri = String::from_str(e, "https://gateway.pinata.cloud/ipfs/bafkreiekr4c6ujajelhtk26frwhytmdgpkvoe76tdewktott7nn63ton3a");
        let name = String::from_str(e, "UnityToken");
        let symbol = String::from_str(e, "SCRYX");
        Base::set_metadata(e, uri, name, symbol);
        ownable::set_owner(e, &owner);
    }

    pub fn mint(e: &Env, to: Address) -> u32 {
        to.require_auth();
        Enumerable::sequential_mint(e, &to)
    }
}

#[contractimpl(contracttrait)]
impl NonFungibleToken for UnityToken {
    type ContractType = Enumerable;

    // Open edition: every token shares the same metadata JSON, so return the
    // collection URI as-is instead of appending the token id.
    fn token_uri(e: &Env, token_id: u32) -> String {
        let _ = Base::owner_of(e, token_id);
        Base::base_uri(e)
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
