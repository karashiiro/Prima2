use crate::event_handler::reactions::ReactionChange;
use serenity::model::interactions::Interaction;
use serenity::model::prelude::application_command::{
    ApplicationCommand, ApplicationCommandOptionType,
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
        match reactions::reaction_activate(&ctx, &added_reaction, ReactionChange::Add).await {
            Err(error) => println!("Error in reaction activation: {:?}", error),
            _ => {}
        }
    }

    async fn reaction_remove(&self, ctx: Context, removed_reaction: Reaction) {
        match reactions::reaction_activate(&ctx, &removed_reaction, ReactionChange::Remove).await {
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
                .description("Remove a role reaction from this guild.")
                .create_option(|o| {
                    o.name("channel")
                        .description("The channel to remove the role reaction from.")
                        .kind(ApplicationCommandOptionType::Channel)
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

        ApplicationCommand::create_global_application_command(&ctx.http, |command| {
            command
                .name("seteurekarole")
                .description(
                    "Sets a registered role reaction to be used as the Eureka special role.",
                )
                .create_option(|o| {
                    o.name("channel")
                        .description("The channel the existing role reaction is in.")
                        .kind(ApplicationCommandOptionType::Channel)
                        .required(true)
                })
                .create_option(|o| {
                    o.name("role")
                        .description("The role ID of the role.")
                        .kind(ApplicationCommandOptionType::Role)
                        .required(true)
                })
        })
        .await
        .unwrap();
        println!("Registered slash command /seteurekarole");

        ApplicationCommand::create_global_application_command(&ctx.http, |command| {
            command
                .name("setbozjarole")
                .description(
                    "Sets a registered role reaction to be used as the Bozja special role.",
                )
                .create_option(|o| {
                    o.name("channel")
                        .description("The channel the existing role reaction is in.")
                        .kind(ApplicationCommandOptionType::Channel)
                        .required(true)
                })
                .create_option(|o| {
                    o.name("role")
                        .description("The role ID of the role.")
                        .kind(ApplicationCommandOptionType::Role)
                        .required(true)
                })
        })
        .await
        .unwrap();
        println!("Registered slash command /setbozjarole");

        ApplicationCommand::create_global_application_command(&ctx.http, |command| {
            command
                .name("unseteurekarole")
                .description("Unsets a registered role reaction as the Eureka special role.")
                .create_option(|o| {
                    o.name("channel")
                        .description("The channel the role reaction is in.")
                        .kind(ApplicationCommandOptionType::Channel)
                        .required(true)
                })
                .create_option(|o| {
                    o.name("role")
                        .description("The role ID of the role.")
                        .kind(ApplicationCommandOptionType::Role)
                        .required(true)
                })
        })
        .await
        .unwrap();
        println!("Registered slash command /unseteurekarole");

        ApplicationCommand::create_global_application_command(&ctx.http, |command| {
            command
                .name("unsetbozjarole")
                .description("Unsets a registered role reaction as the Bozja special role.")
                .create_option(|o| {
                    o.name("channel")
                        .description("The channel the role reaction is in.")
                        .kind(ApplicationCommandOptionType::Channel)
                        .required(true)
                })
                .create_option(|o| {
                    o.name("role")
                        .description("The role ID of the role.")
                        .kind(ApplicationCommandOptionType::Role)
                        .required(true)
                })
        })
        .await
        .unwrap();
        println!("Registered slash command /unsetbozjarole");
    }

    async fn interaction_create(&self, ctx: Context, interaction: Interaction) {
        if let Some(c) = interaction.application_command() {
            let data = &c.data;

            println!(
                "Received slash command: /{} {}\n\tOptions: {}",
                data.name,
                data.options
                    .iter()
                    .map(|opt| opt.value.as_ref().unwrap().as_str().unwrap())
                    .collect::<Vec<&str>>()
                    .join(" "),
                data.options
                    .iter()
                    .map(|opt| opt.name.as_str())
                    .collect::<Vec<&str>>()
                    .join(" ")
            );

            match data.name.as_str() {
                "rolereactions" => slash_commands::role_reactions(&ctx, &c).await,
                "addrolereaction" => slash_commands::add_role_reaction(&ctx, &c).await,
                "removerolereaction" => slash_commands::remove_role_reaction(&ctx, &c).await,
                "seteurekarole" => slash_commands::declare_eureka_role(&ctx, &c).await,
                "setbozjarole" => slash_commands::declare_bozja_role(&ctx, &c).await,
                "unseteurekarole" => slash_commands::undeclare_eureka_role(&ctx, &c).await,
                "unsetbozjarole" => slash_commands::undeclare_bozja_role(&ctx, &c).await,
                _ => println!("Slash command was unknown."),
            }
        }
    }
}
