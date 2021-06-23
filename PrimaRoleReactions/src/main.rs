#![allow(non_snake_case)] // This disables this lint for the entire crate - most code should live in the library, not here
use std::env;

use serenity::prelude::*;

use role_reactions::Handler;

#[tokio::main]
async fn main() {
    let token = env::var("PRIMA_BOT_TOKEN").expect("Expected a token in the environment");

    let mut client = Client::builder(&token)
        .event_handler(Handler)
        .await
        .expect("Err creating client");

    if let Err(why) = client.start().await {
        println!("Client error: {:?}", why);
    }
}
