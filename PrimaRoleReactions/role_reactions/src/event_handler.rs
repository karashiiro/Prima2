use crate::typemaps::RoleReactionsDatabaseContainer;
use db_access::role_reaction_info::RoleReactionInfo;
use serenity::builder::CreateInteractionResponseData;
use serenity::model::interactions::{
    ApplicationCommand, ApplicationCommandInteractionData, ApplicationCommandOptionType,
    Interaction, InteractionData, InteractionResponseType,
};
use serenity::model::prelude::{Member, Reaction, ReactionType, RoleId};
use serenity::prelude::{Context, EventHandler};
use serenity::utils::Colour;
use serenity::Error;
use serenity::{async_trait, model::gateway::Ready};

pub struct Handler;

async fn reaction_activate(ctx: &Context, reaction: &Reaction) -> Result<(), Error> {
    let data = ctx.data.read().await;
    let db = data
        .get::<RoleReactionsDatabaseContainer>()
        .expect("Expected RoleReactionsDatabaseContainer in TypeMap");

    if let Some(guild) = reaction.guild_id {
        let mut member: Member;
        if let Some(user) = reaction.member.as_ref().and_then(|pm| pm.user.as_ref()) {
            member = guild.member(&ctx.http, user).await?;
        } else {
            println!("Failed to fetch guild member.");
            return Ok(());
        }

        if let ReactionType::Custom { id, .. } = reaction.emoji {
            if let Some(role_reaction) = db
                .get_role_reaction(*reaction.channel_id.as_u64(), *id.as_u64())
                .await
                .unwrap_or_else(|error| {
                    println!("Failed to get role reaction from database: {:?}", error);
                    None
                })
            {
                let role_id = RoleId(role_reaction.role_id.parse()?);
                let roles = guild.roles(&ctx.http).await?;

                let role = roles.get(&role_id);
                if role.is_none() {
                    println!("Failed to retrieve role. Does it exist?");
                    return Ok(());
                }

                if member.roles.contains(&role_id) {
                    member.remove_role(&ctx.http, role_id).await?;
                } else {
                    member.add_role(&ctx.http, role_id).await?;
                }
            }
        }
    }

    Ok(())
}

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

async fn role_reactions(ctx: &Context, interaction: &Interaction) {
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

async fn add_role_reaction(ctx: &Context, interaction: &Interaction) {
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

async fn remove_role_reaction(ctx: &Context, interaction: &Interaction) {
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

#[async_trait]
impl EventHandler for Handler {
    async fn reaction_add(&self, ctx: Context, added_reaction: Reaction) {
        reaction_activate(&ctx, &added_reaction)
            .await
            .unwrap_or_else(|error| println!("Error in reaction activation: {:?}", error));
    }

    async fn reaction_remove(&self, ctx: Context, removed_reaction: Reaction) {
        reaction_activate(&ctx, &removed_reaction)
            .await
            .unwrap_or_else(|error| println!("Error in reaction activation: {:?}", error));
    }

    async fn ready(&self, ctx: Context, ready: Ready) {
        println!(
            "Logged in as {}#{}!",
            ready.user.name, ready.user.discriminator
        );

        ApplicationCommand::create_global_application_command(&ctx.http, |command| {
            command
                .name("rolereactions")
                .description("Retrieve the list of role reactions for this guild.")
        })
        .await
        .unwrap();
        println!("Registered slash command /rolereactions");

        ApplicationCommand::create_global_application_command(&ctx.http, |command| {
            command
                .name("addrolereaction")
                .description("Add a role reaction to this guild.")
                .create_option(|o| {
                    o.name("channel")
                        .description("The channel to add a role reaction to.")
                        .kind(ApplicationCommandOptionType::Channel)
                        .required(true)
                })
                .create_option(|o| {
                    o.name("emoji_id")
                        .description("The ID of the emoji to add a reaction with.")
                        .kind(ApplicationCommandOptionType::String)
                        .required(true)
                })
                .create_option(|o| {
                    o.name("role")
                        .description("The role to add a reaction for.")
                        .kind(ApplicationCommandOptionType::Role)
                        .required(true)
                })
        })
        .await
        .unwrap();
        println!("Registered slash command /addrolereaction");

        ApplicationCommand::create_global_application_command(&ctx.http, |command| {
            command
                .name("removerolereaction")
                .description("Remove a role reaction to this guild.")
                .create_option(|o| {
                    o.name("channel")
                        .description("The channel to remove the role reaction from.")
                        .kind(ApplicationCommandOptionType::Channel)
                        .required(true)
                })
                .create_option(|o| {
                    o.name("emoji_id")
                        .description("The ID of the emoji used to add the reaction.")
                        .kind(ApplicationCommandOptionType::String)
                        .required(true)
                })
                .create_option(|o| {
                    o.name("role")
                        .description("The role to remove the reaction for.")
                        .kind(ApplicationCommandOptionType::Role)
                        .required(true)
                })
        })
        .await
        .unwrap();
        println!("Registered slash command /removerolereaction");
    }

    async fn interaction_create(&self, ctx: Context, interaction: Interaction) {
        if interaction.guild_id.is_none() {
            return;
        }

        match interaction.clone().data {
            None => {}
            Some(data) => match data {
                InteractionData::ApplicationCommand(c) => match c.name.as_str() {
                    "rolereactions" => role_reactions(&ctx, &interaction).await,
                    "addrolereaction" => add_role_reaction(&ctx, &interaction).await,
                    "removerolereaction" => remove_role_reaction(&ctx, &interaction).await,
                    _ => {}
                },
                InteractionData::MessageComponent(_) => {}
            },
        }
    }
}
