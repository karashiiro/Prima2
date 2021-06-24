use crate::typemaps::RoleReactionsDatabaseContainer;
use serenity::model::prelude::{Member, Reaction, ReactionType, RoleId};
use serenity::prelude::{Context, EventHandler};
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
                let role_id = RoleId(role_reaction.role_id);
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

    async fn ready(&self, _: Context, ready: Ready) {
        println!(
            "Logged in as {}#{}!",
            ready.user.name, ready.user.discriminator
        );
    }
}
