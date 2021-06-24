use serenity::model::interactions::{
    ApplicationCommand, ApplicationCommandOptionType, Interaction, InteractionData,
};
use serenity::model::prelude::Reaction;
use serenity::prelude::{Context, EventHandler};
use serenity::{async_trait, model::gateway::Ready};

mod reactions;
mod slash_commands;

pub struct Handler;

#[async_trait]
impl EventHandler for Handler {
    async fn reaction_add(&self, ctx: Context, added_reaction: Reaction) {
        match reactions::reaction_activate(&ctx, &added_reaction).await {
            Err(error) => println!("Error in reaction activation: {:?}", error),
            _ => {}
        }
    }

    async fn reaction_remove(&self, ctx: Context, removed_reaction: Reaction) {
        match reactions::reaction_activate(&ctx, &removed_reaction).await {
            Err(error) => println!("Error in reaction activation: {:?}", error),
            _ => {}
        }
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

        if let Some(data) = interaction.clone().data {
            if let InteractionData::ApplicationCommand(c) = data {
                println!(
                    "Received slash command: /{} {}",
                    c.name,
                    c.options
                        .iter()
                        .map(|opt| opt.value.as_ref().unwrap().as_str().unwrap())
                        .collect::<Vec<&str>>()
                        .join(" ")
                );
            }
        }

        match interaction.clone().data {
            None => {}
            Some(data) => match data {
                InteractionData::ApplicationCommand(c) => match c.name.as_str() {
                    "rolereactions" => slash_commands::role_reactions(&ctx, &interaction).await,
                    "addrolereaction" => {
                        slash_commands::add_role_reaction(&ctx, &interaction).await
                    }
                    "removerolereaction" => {
                        slash_commands::remove_role_reaction(&ctx, &interaction).await
                    }
                    _ => {
                        println!("Slash command was unknown.")
                    }
                },
                InteractionData::MessageComponent(_) => {}
            },
        }
    }
}
