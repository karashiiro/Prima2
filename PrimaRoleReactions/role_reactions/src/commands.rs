use serenity::{
    prelude::Context,
    model::prelude::Message,
    framework::standard::{
        CommandResult,
        macros::{command, group},
    },
};
use crate::typemaps::RoleReactionsDatabaseContainer;

#[group]
#[commands(role_reactions)]
pub struct RoleReactions;

#[command]
#[only_in(guilds)]
async fn role_reactions(ctx: &Context, message: &Message) -> CommandResult {
    let guild_id = *message.guild_id.unwrap().as_u64();

    let data = ctx.data.read().await;
    let db = data.get::<RoleReactionsDatabaseContainer>()
        .expect("Expected RoleReactionsDatabase in TypeMap");

    match db.get_role_reactions(guild_id).await {
        Ok(role_reactions) => {
            for _ in role_reactions {
                if let Err(why) = message.channel_id.say(&ctx.http, "A role reaction.").await {
                    println!("Error sending message: {:?}", why);
                }
            }
        }
        Err(error) => {
            println!("Failed to fetch role reactions for guild: {:?}", error);
        }
    }

    Ok(())
}