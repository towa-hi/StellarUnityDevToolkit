#![cfg(test)]

use super::*;
use soroban_sdk::{vec, Env, Map, String};

#[test]
fn test_hello() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    let words = client.hello(&String::from_str(&env, "Dev"));
    assert_eq!(
        words,
        vec![
            &env,
            String::from_str(&env, "Hello"),
            String::from_str(&env, "Dev"),
        ]
    );
}

#[test]
fn test_echo_u32() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    assert_eq!(client.echo_u32(&42), 42);
    assert_eq!(client.echo_u32(&0), 0);
    assert_eq!(client.echo_u32(&u32::MAX), u32::MAX);
}

#[test]
fn test_echo_i32() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    assert_eq!(client.echo_i32(&-1), -1);
    assert_eq!(client.echo_i32(&0), 0);
    assert_eq!(client.echo_i32(&i32::MAX), i32::MAX);
    assert_eq!(client.echo_i32(&i32::MIN), i32::MIN);
}

#[test]
fn test_echo_u64() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    assert_eq!(client.echo_u64(&0), 0);
    assert_eq!(client.echo_u64(&u64::MAX), u64::MAX);
    assert_eq!(client.echo_u64(&123_456_789_000), 123_456_789_000);
}

#[test]
fn test_echo_bool() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    assert_eq!(client.echo_bool(&true), true);
    assert_eq!(client.echo_bool(&false), false);
}

#[test]
fn test_echo_string() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    let s = String::from_str(&env, "hello world");
    assert_eq!(client.echo_string(&s), s);

    let empty = String::from_str(&env, "");
    assert_eq!(client.echo_string(&empty), empty);
}

#[test]
fn test_echo_bytes() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    let data = Bytes::from_slice(&env, &[0xDE, 0xAD, 0xBE, 0xEF]);
    assert_eq!(client.echo_bytes(&data), data);
}

#[test]
fn test_add_u32() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    assert_eq!(client.add_u32(&10, &20), 30);
    assert_eq!(client.add_u32(&0, &0), 0);
}

#[test]
fn test_add_i32() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    assert_eq!(client.add_i32(&-10, &20), 10);
    assert_eq!(client.add_i32(&-5, &-3), -8);
}

#[test]
fn test_negate_i32() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    assert_eq!(client.negate_i32(&42), -42);
    assert_eq!(client.negate_i32(&-1), 1);
    assert_eq!(client.negate_i32(&0), 0);
}

#[test]
fn test_echo_vec() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    let v = vec![&env, 1u32, 2, 3, 4, 5];
    assert_eq!(client.echo_vec(&v), v);
}

#[test]
fn test_sum_vec() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    let v = vec![&env, 10u32, 20, 30];
    assert_eq!(client.sum_vec(&v), 60);
}

#[test]
fn test_make_vec() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    let result = client.make_vec(&7, &8, &9);
    assert_eq!(result, vec![&env, 7u32, 8, 9]);
}

#[test]
fn test_echo_map() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    let mut m = Map::new(&env);
    m.set(String::from_str(&env, "hp"), 100);
    m.set(String::from_str(&env, "mp"), 50);
    assert_eq!(client.echo_map(&m), m);
}

#[test]
fn test_map_get() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    let mut m = Map::new(&env);
    m.set(String::from_str(&env, "gold"), 999);
    assert_eq!(client.map_get(&m, &String::from_str(&env, "gold")), 999);
    assert_eq!(client.map_get(&m, &String::from_str(&env, "silver")), 0);
}

#[test]
fn test_make_map() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    let keys = vec![
        &env,
        String::from_str(&env, "a"),
        String::from_str(&env, "b"),
    ];
    let vals = vec![&env, 1u32, 2];
    let result = client.make_map(&keys, &vals);
    assert_eq!(result.get(String::from_str(&env, "a")), Some(1));
    assert_eq!(result.get(String::from_str(&env, "b")), Some(2));
}

#[test]
fn test_echo_player() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    let p = Player {
        name: String::from_str(&env, "Alice"),
        score: 100,
        active: true,
    };
    assert_eq!(client.echo_player(&p), p);
}

#[test]
fn test_make_player() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    let result = client.make_player(&String::from_str(&env, "Bob"), &50);
    assert_eq!(result.name, String::from_str(&env, "Bob"));
    assert_eq!(result.score, 50);
    assert_eq!(result.active, true);
}

#[test]
fn test_player_name() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    let p = Player {
        name: String::from_str(&env, "Charlie"),
        score: 0,
        active: false,
    };
    assert_eq!(client.player_name(&p), String::from_str(&env, "Charlie"));
}

#[test]
fn test_player_score() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    let p = Player {
        name: String::from_str(&env, "Dave"),
        score: 9999,
        active: true,
    };
    assert_eq!(client.player_score(&p), 9999);
}

#[test]
fn test_echo_inventory() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    let owner = Player {
        name: String::from_str(&env, "Eve"),
        score: 42,
        active: true,
    };
    let items = vec![
        &env,
        String::from_str(&env, "sword"),
        String::from_str(&env, "shield"),
    ];
    let mut quantities = Map::new(&env);
    quantities.set(String::from_str(&env, "sword"), 1u32);
    quantities.set(String::from_str(&env, "shield"), 2u32);

    let inv = Inventory {
        owner,
        items,
        quantities,
    };
    assert_eq!(client.echo_inventory(&inv), inv);
}

#[test]
fn test_make_inventory() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    let items = vec![
        &env,
        String::from_str(&env, "potion"),
        String::from_str(&env, "scroll"),
    ];
    let result = client.make_inventory(&String::from_str(&env, "Frank"), &items);
    assert_eq!(result.owner.name, String::from_str(&env, "Frank"));
    assert_eq!(result.owner.score, 0);
    assert_eq!(result.items.len(), 2);
    assert_eq!(
        result.quantities.get(String::from_str(&env, "potion")),
        Some(1)
    );
    assert_eq!(
        result.quantities.get(String::from_str(&env, "scroll")),
        Some(2)
    );
}

#[test]
fn test_bytes_len() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    let data = Bytes::from_slice(&env, &[1, 2, 3, 4, 5]);
    assert_eq!(client.bytes_len(&data), 5);

    let empty = Bytes::new(&env);
    assert_eq!(client.bytes_len(&empty), 0);
}

#[test]
fn test_concat_bytes() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    let a = Bytes::from_slice(&env, &[1, 2]);
    let b = Bytes::from_slice(&env, &[3, 4]);
    let result = client.concat_bytes(&a, &b);
    assert_eq!(result, Bytes::from_slice(&env, &[1, 2, 3, 4]));
}

#[test]
fn test_describe_player() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    let p = Player {
        name: String::from_str(&env, "Grace"),
        score: 77,
        active: true,
    };
    let desc = client.describe_player(&p);
    assert_eq!(desc.len(), 2);
    assert_eq!(desc.get(0), Some(String::from_str(&env, "Grace")));
    assert_eq!(desc.get(1), Some(String::from_str(&env, "active")));

    let inactive = Player {
        name: String::from_str(&env, "Hank"),
        score: 0,
        active: false,
    };
    let desc2 = client.describe_player(&inactive);
    assert_eq!(desc2.get(1), Some(String::from_str(&env, "inactive")));
}

#[test]
fn test_edge_cases() {
    let env = Env::default();
    let contract_id = env.register(Contract, ());
    let client = ContractClient::new(&env, &contract_id);

    assert_eq!(client.echo_u32_zero(), 0);
    assert_eq!(client.echo_empty_vec().len(), 0);
    assert_eq!(client.echo_empty_string(), String::from_str(&env, ""));
    assert_eq!(client.echo_true(), true);
    assert_eq!(client.echo_false(), false);
    assert_eq!(client.echo_max_u32(), u32::MAX);
    assert_eq!(client.echo_min_i32(), i32::MIN);
    assert_eq!(client.echo_max_u64(), u64::MAX);
}
