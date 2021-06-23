#![allow(non_snake_case)] // This disables this lint for the entire crate - most code should live in the library, not here
use std::env;
use serenity::prelude::*;
use db_access::database::RoleReactionsDatabase;
use role_reactions::event_handler::Handler;
use role_reactions::hooks::{before, after};
use role_reactions::commands::ROLEREACTIONS_GROUP;
use serenity::http::Http;
use std::collections::HashSet;
use serenity::framework::StandardFramework;
use role_reactions::typemaps::RoleReactionsDatabaseContainer;

#[tokio::main]
async fn main() {
    let token = env::var("PRIMA_BOT_TOKEN").expect("Expected a token in the environment");

    let http = Http::new_with_token(&token);
    let (owners, bot_id) = match http.get_current_application_info().await {
        Ok(info) => {
            let mut owners = HashSet::new();
            owners.insert(info.owner.id);

            match http.get_current_user().await {
                Ok(bot_id) => (owners, bot_id.id),
                Err(why) => panic!("Could not access the bot id: {:?}", why),
            }
        },
        Err(why) => panic!("Could not access application info: {:?}", why),
    };

    let db = RoleReactionsDatabase::new("Prima Role Reactions", "PrimaDb").await;

    let framework = StandardFramework::new()
        .configure(|c| c
            .with_whitespace(true)
            .on_mention(Some(bot_id))
            .prefix("~")
            .delimiters(vec![" "])
            .owners(owners))
        .before(before)
        .after(after)
        .group(&ROLEREACTIONS_GROUP);

    let mut client = Client::builder(&token)
        .event_handler(Handler)
        .framework(framework)
        .await
        .expect("Err creating client");

    {
        let mut data = client.data.write().await;
        data.insert::<RoleReactionsDatabaseContainer>(db);
    }

    if let Err(why) = client.start().await {
        println!("Client error: {:?}", why);
    }
}
