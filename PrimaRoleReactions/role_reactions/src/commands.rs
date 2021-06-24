use serenity::{
    prelude::Context,
    model::prelude::Message,
    framework::standard::{
        Args, CommandResult,
        macros::{command, group},
    },
};
use crate::typemaps::RoleReactionsDatabaseContainer;
use db_access::role_reaction_info::RoleReactionInfo;

#[group]
#[only_in(guilds)]
#[commands(role_reactions, add_role_reaction, remove_role_reaction)]
pub struct RoleReactions;

fn read_role_reaction_info(message: &Message, mut args: Args) -> RoleReactionInfo {
    RoleReactionInfo {
        guild_id: *message.guild_id.unwrap().as_u64(),
        channel_id: args.single::<u64>().unwrap(),
        emoji_id: args.single::<u64>().unwrap(),
        role_id: args.single::<u64>().unwrap(),
    }
}

#[command("rolereactions")]
async fn role_reactions(ctx: &Context, message: &Message) -> CommandResult {
    let data = ctx.data.read().await;
    let db = data.get::<RoleReactionsDatabaseContainer>()
        .expect("Expected RoleReactionsDatabase in TypeMap");

    let guild_id = *message.guild_id.unwrap().as_u64();

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
            if let Err(why) = message.channel_id.say(&ctx.http, "Failed to fetch role reactions for this guild.").await {
                println!("Error sending message: {:?}", why);
            }
        }
    }

    Ok(())
}

#[command("addrolereaction")]
async fn add_role_reaction(ctx: &Context, message: &Message, args: Args) -> CommandResult {
    let data = ctx.data.read().await;
    let db = data.get::<RoleReactionsDatabaseContainer>()
        .expect("Expected RoleReactionsDatabase in TypeMap");

    let role_reaction = read_role_reaction_info(message, args);

    match db.add_role_reaction(role_reaction).await {
        Ok(_) => {}
        Err(error) => {
            println!("Failed to add role reaction: {:?}", error);
            if let Err(why) = message.channel_id.say(&ctx.http, "Failed to add role reaction.").await {
                println!("Error sending message: {:?}", why);
            }
        }
    }

    Ok(())
}

#[command("removerolereaction")]
async fn remove_role_reaction(ctx: &Context, message: &Message, args: Args) -> CommandResult {
    let data = ctx.data.read().await;
    let db = data.get::<RoleReactionsDatabaseContainer>()
        .expect("Expected RoleReactionsDatabase in TypeMap");

    let role_reaction = read_role_reaction_info(message, args);

    match db.remove_role_reaction(role_reaction).await {
        Ok(_) => {}
        Err(error) => {
            println!("Failed to remove role reaction: {:?}", error);
            if let Err(why) = message.channel_id.say(&ctx.http, "Failed to remove role reaction.").await {
                println!("Error sending message: {:?}", why);
            }
        }
    }

    Ok(())
}