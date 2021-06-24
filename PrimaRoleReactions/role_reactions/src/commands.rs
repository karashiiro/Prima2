use crate::typemaps::RoleReactionsDatabaseContainer;
use db_access::role_reaction_info::RoleReactionInfo;
use serenity::{
    framework::standard::{
        macros::{command, group},
        Args, CommandResult,
    },
    model::prelude::Message,
    prelude::Context,
};

#[group]
#[only_in(guilds)]
#[commands(role_reactions, add_role_reaction, remove_role_reaction)]
pub struct RoleReactions;

async fn read_role_reaction_info(
    ctx: &Context,
    message: &Message,
    mut args: Args,
) -> Option<RoleReactionInfo> {
    let guild_id = *message.guild_id.unwrap().as_u64();
    let channel_id = args.single::<u64>();
    let emoji_id = args.single::<u64>();
    let role_id = args.single::<u64>();

    let mut error_message: Option<&str> = None;
    if let Err(_) = role_id {
        error_message = Some("Failed to parse role ID.");
    }

    if let Err(_) = emoji_id {
        error_message = Some("Failed to parse emoji ID. Only non-Unicode emoji are supported.");
    }

    if let Err(_) = channel_id {
        error_message = Some("Failed to parse channel ID.");
    }

    if let Some(error) = error_message {
        println!("{}", error);
        if let Err(why) = message.channel_id.say(&ctx.http, error).await {
            println!("Error sending message: {:?}", why);
        }
    }

    Some(RoleReactionInfo {
        guild_id,
        channel_id: channel_id.unwrap(),
        emoji_id: emoji_id.unwrap(),
        role_id: role_id.unwrap(),
    })
}

#[command("rolereactions")]
async fn role_reactions(ctx: &Context, message: &Message) -> CommandResult {
    let data = ctx.data.read().await;
    let db = data
        .get::<RoleReactionsDatabaseContainer>()
        .expect("Expected RoleReactionsDatabaseContainer in TypeMap");

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
            if let Err(why) = message
                .channel_id
                .say(&ctx.http, "Failed to fetch role reactions for this guild.")
                .await
            {
                println!("Error sending message: {:?}", why);
            }
        }
    }

    Ok(())
}

#[command("addrolereaction")]
async fn add_role_reaction(ctx: &Context, message: &Message, args: Args) -> CommandResult {
    let data = ctx.data.read().await;
    let db = data
        .get::<RoleReactionsDatabaseContainer>()
        .expect("Expected RoleReactionsDatabaseContainer in TypeMap");

    if let Some(role_reaction) = read_role_reaction_info(ctx, message, args).await {
        match db.add_role_reaction(role_reaction).await {
            Ok(_) => {}
            Err(error) => {
                println!("Failed to add role reaction: {:?}", error);
                if let Err(why) = message
                    .channel_id
                    .say(&ctx.http, "Failed to add role reaction.")
                    .await
                {
                    println!("Error sending message: {:?}", why);
                }
            }
        }
    }

    Ok(())
}

#[command("removerolereaction")]
async fn remove_role_reaction(ctx: &Context, message: &Message, args: Args) -> CommandResult {
    let data = ctx.data.read().await;
    let db = data
        .get::<RoleReactionsDatabaseContainer>()
        .expect("Expected RoleReactionsDatabaseContainer in TypeMap");

    if let Some(role_reaction) = read_role_reaction_info(ctx, message, args).await {
        match db.remove_role_reaction(role_reaction).await {
            Ok(_) => {}
            Err(error) => {
                println!("Failed to remove role reaction: {:?}", error);
                if let Err(why) = message
                    .channel_id
                    .say(&ctx.http, "Failed to remove role reaction.")
                    .await
                {
                    println!("Error sending message: {:?}", why);
                }
            }
        }
    }

    Ok(())
}
