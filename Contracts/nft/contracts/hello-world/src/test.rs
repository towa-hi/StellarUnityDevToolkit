#![cfg(test)]

use super::*;
use soroban_sdk::{testutils::Address as _, Address, Env, String};

fn sample_uris(env: &Env) -> (String, String, String) {
    (
        String::from_str(env, "ipfs://50"),
        String::from_str(env, "ipfs://100"),
        String::from_str(env, "ipfs://500"),
    )
}

fn register_token<'a>(
    env: &'a Env,
    owner: &Address,
) -> UnityTokenClient<'a> {
    let (uri_50, uri_100, uri_500) = sample_uris(env);
    let contract_id = env.register(
        UnityToken,
        (owner.clone(), uri_50, uri_100, uri_500),
    );
    UnityTokenClient::new(env, &contract_id)
}

#[test]
fn mint_encodes_points_in_token_id_and_uri() {
    let env = Env::default();
    env.mock_all_auths();
    let owner = Address::generate(&env);
    let player = Address::generate(&env);
    let client = register_token(&env, &owner);

    let id_50 = client.mint(&player, &50);
    assert_eq!(id_50, 50);
    assert_eq!(client.asset_points(&id_50), 50);
    assert_eq!(client.token_uri(&id_50), String::from_str(&env, "ipfs://50"));
    assert_eq!(client.owner_of(&id_50), player);

    let id_100 = client.mint(&player, &100);
    assert_eq!(id_100, 1100);
    assert_eq!(client.asset_points(&id_100), 100);
    assert_eq!(
        client.token_uri(&id_100),
        String::from_str(&env, "ipfs://100")
    );

    let id_500 = client.mint(&player, &500);
    assert_eq!(id_500, 2500);
    assert_eq!(client.asset_points(&id_500), 500);
    assert_eq!(
        client.token_uri(&id_500),
        String::from_str(&env, "ipfs://500")
    );
}

#[test]
fn minter_can_mint() {
    let env = Env::default();
    env.mock_all_auths();
    let owner = Address::generate(&env);
    let minter = Address::generate(&env);
    let player = Address::generate(&env);
    let client = register_token(&env, &owner);

    client.set_minter(&minter);
    let token_id = client.mint(&player, &50);
    assert_eq!(token_id, 50);
    assert_eq!(client.owner_of(&token_id), player);
}

#[test]
#[should_panic(expected = "invalid asset points")]
fn mint_rejects_invalid_points() {
    let env = Env::default();
    env.mock_all_auths();
    let owner = Address::generate(&env);
    let player = Address::generate(&env);
    let client = register_token(&env, &owner);
    client.mint(&player, &75);
}

#[test]
#[should_panic]
fn mint_requires_authorization() {
    let env = Env::default();
    let owner = Address::generate(&env);
    let player = Address::generate(&env);
    let client = register_token(&env, &owner);
    client.mint(&player, &50);
}
