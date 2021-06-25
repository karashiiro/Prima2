use crate::typemaps::RoleReactionsDatabaseContainer;
use db_access::role_reaction_info::RoleReactionInfo;
use serenity::builder::CreateInteractionResponseData;
use serenity::model::prelude::{
    ApplicationCommandInteractionData, Interaction, InteractionData, InteractionResponseType,
};
use serenity::prelude::Context;
use serenity::utils::Colour;

async fn create_interaction_response<F>(ctx: &Context, interaction: &Interaction, f: F)
where
    F: FnOnce(&mut CreateInteractionResponseData) -> &mut CreateInteractionResponseData,
{
    interaction
        .create_interaction_response(&ctx.http, |response| {
            response
                .kind(InteractionResponseType::ChannelMessageWithSource)
                .interaction_response_data(f)
        })
        .await
        .unwrap_or_else(|error| println!("Failed to send interaction response: {:?}", error));
}

async fn read_role_reaction_info(
    ctx: &Context,
    interaction: &Interaction,
    data: &ApplicationCommandInteractionData,
) -> Option<RoleReactionInfo> {
    let mut options_iterator = data.options.iter();

    let channel_id = options_iterator
        .find(|&o| o.name == "channel")
        .unwrap()
        .value
        .as_ref()
        .unwrap()
        .as_str()
        .unwrap();

    let emoji_id = options_iterator
        .find(|&o| o.name == "emoji_id")
        .unwrap()
        .value
        .as_ref()
        .unwrap()
        .as_str()
        .unwrap();
    if let Err(_) = emoji_id.parse::<u64>() {
        create_interaction_response(&ctx, &interaction, |m| {
            m.content("Failed to parse emoji. Make sure the ID is correct, and that the emoji you are using is not a Unicode emote.")
        })
            .await;
        return None;
    }

    let role_id = options_iterator
        .find(|&o| o.name == "role")
        .unwrap()
        .value
        .as_ref()
        .unwrap()
        .as_str()
        .unwrap();

    Some(RoleReactionInfo {
        guild_id: interaction.guild_id.unwrap().to_string(),
        channel_id: channel_id.to_string(),
        emoji_id: emoji_id.to_string(),
        role_id: role_id.to_string(),
    })
}

pub async fn role_reactions(ctx: &Context, interaction: &Interaction) {
    if !interaction.member.as_ref().unwrap().permissions.unwrap().manage_roles() {
        return;
    }

    let data = ctx.data.read().await;
    let db = data
        .get::<RoleReactionsDatabaseContainer>()
        .expect("Expected RoleReactionsDatabaseContainer in TypeMap");

    let guild_id = *interaction.guild_id.unwrap().as_u64();

    match db.get_role_reactions(guild_id).await {
        Ok(role_reactions) => {
            if role_reactions.len() == 0 {
                create_interaction_response(&ctx, &interaction, |m| {
                    m.content("No role reactions are registered for this guild.")
                })
                .await;
                return;
            }

            let guild_name = interaction
                .guild_id
                .unwrap()
                .name(&ctx.cache)
                .await
                .unwrap();

            create_interaction_response(&ctx, &interaction, |m| {
                m.create_embed(|e| {
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
            .await;
        }
        Err(error) => {
            println!(
                "Failed to retrieve role reactions for this guild: {:?}",
                error
            );
            create_interaction_response(&ctx, &interaction, |m| {
                m.content("Failed to retrieve role reactions for this guild.")
            })
            .await;
        }
    }
}

pub async fn add_role_reaction(ctx: &Context, interaction: &Interaction) {
    if !interaction.member.as_ref().unwrap().permissions.unwrap().manage_roles() {
        return;
    }

    let data = ctx.data.read().await;
    let db = data
        .get::<RoleReactionsDatabaseContainer>()
        .expect("Expected RoleReactionsDatabaseContainer in TypeMap");

    match interaction.clone().data {
        None => {}
        Some(data) => match data {
            InteractionData::ApplicationCommand(c) => {
                let role_reaction = read_role_reaction_info(ctx, interaction, &c).await;
                if role_reaction.is_none() {
                    return;
                }

                match db.add_role_reaction(role_reaction.unwrap()).await {
                    Ok(_) => {
                        create_interaction_response(ctx, interaction, |m| {
                            m.content("Role reaction added.")
                        })
                        .await
                    }
                    Err(error) => {
                        println!("Failed to add role reaction: {:?}", error);
                        create_interaction_response(ctx, interaction, |m| {
                            m.content("Failed to add role reaction.")
                        })
                        .await;
                    }
                }
            }
            InteractionData::MessageComponent(_) => {}
        },
    }
}

pub async fn remove_role_reaction(ctx: &Context, interaction: &Interaction) {
    if !interaction.member.as_ref().unwrap().permissions.unwrap().manage_roles() {
        return;
    }

    let data = ctx.data.read().await;
    let db = data
        .get::<RoleReactionsDatabaseContainer>()
        .expect("Expected RoleReactionsDatabaseContainer in TypeMap");

    match interaction.clone().data {
        None => {}
        Some(data) => match data {
            InteractionData::ApplicationCommand(c) => {
                let role_reaction = read_role_reaction_info(ctx, interaction, &c).await;
                if role_reaction.is_none() {
                    return;
                }

                match db.remove_role_reaction(role_reaction.unwrap()).await {
                    Ok(_) => {
                        create_interaction_response(ctx, interaction, |m| {
                            m.content("Role reaction removed.")
                        })
                        .await
                    }
                    Err(error) => {
                        println!("Failed to remove role reaction: {:?}", error);
                        create_interaction_response(ctx, interaction, |m| {
                            m.content("Failed to remove role reaction.")
                        })
                        .await
                    }
                }
            }
            InteractionData::MessageComponent(_) => {}
        },
    }
}
