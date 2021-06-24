use crate::typemaps::RoleReactionsDatabaseContainer;
use db_access::role_reaction_info::RoleReactionInfo;
use serenity::utils::Colour;
use serenity::{
    framework::standard::{
        macros::{command, group},
        Args, CommandResult,
    },
    model::prelude::Message,
    prelude::Context,
};
use std::fmt::Display;

#[group]
#[only_in(guilds)]
#[commands(role_reactions, add_role_reaction, remove_role_reaction)]
pub struct RoleReactions;

async fn reply_to(ctx: &Context, message: &Message, response: impl Display) {
    if let Err(why) = message.channel_id.say(&ctx.http, response).await {
        println!("Error sending message: {:?}", why);
    }
}

async fn read_role_reaction_info(
    ctx: &Context,
    message: &Message,
    mut args: Args,
) -> Option<RoleReactionInfo> {
    let guild_id = *message.guild_id.unwrap().as_u64();

    // These are parsed as u64s for validation, before being converted back to strings
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
        reply_to(ctx, message, error).await;
        return None;
    }

    Some(RoleReactionInfo {
        guild_id: guild_id.to_string(),
        channel_id: channel_id.unwrap().to_string(),
        emoji_id: emoji_id.unwrap().to_string(),
        role_id: role_id.unwrap().to_string(),
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
            if role_reactions.len() == 0 {
                reply_to(
                    ctx,
                    message,
                    "No role reactions are registered for this guild.",
                )
                .await;
                return Ok(());
            }

            let guild_name = message.guild_id.unwrap().name(&ctx.cache).await.unwrap();

            if let Err(why) = message
                .channel_id
                .send_message(&ctx.http, |m| {
                    m.embed(|e| {
                        let mut reactions_string = String::from("**Role reactions:**\n");
                        for reaction in role_reactions {
                            reactions_string.push_str(&*format!(
                                "<#{}> <:e:{}>: <@&{}>\n",
                                reaction.channel_id, reaction.emoji_id, reaction.role_id
                            ));
                        }

                        e.title(&*format!("{} Role Reactions", guild_name))
                            .color(Colour::from_rgb(52, 152, 219))
                            .description(reactions_string)
                    })
                })
                .await
            {
                println!("Error sending message: {:?}", why);
            }
        }
        Err(error) => {
            println!("Failed to fetch role reactions for guild: {:?}", error);
            reply_to(
                ctx,
                message,
                "Failed to fetch role reactions for this guild.",
            )
            .await;
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
            Ok(_) => reply_to(ctx, message, "Role reaction added.").await,
            Err(error) => {
                println!("Failed to add role reaction: {:?}", error);
                reply_to(ctx, message, "Failed to add role reaction.").await;
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
            Ok(_) => reply_to(ctx, message, "Role reaction removed.").await,
            Err(error) => {
                println!("Failed to remove role reaction: {:?}", error);
                reply_to(ctx, message, "Failed to remove role reaction.").await;
            }
        }
    }

    Ok(())
}
