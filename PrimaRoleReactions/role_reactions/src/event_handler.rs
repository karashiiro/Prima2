use crate::typemaps::RoleReactionsDatabaseContainer;
use serenity::model::prelude::*;
use serenity::prelude::{Context, EventHandler};
use serenity::{async_trait, model::gateway::Ready};
use std::collections::hash_map::RandomState;

pub struct Handler;

async fn get_member_from_partial(ctx: &Context, guild: &GuildId, partial_member: &Option<PartialMember>) -> Option<Member> {
    match partial_member.as_ref().and_then(|pm| pm.user.as_ref()) {
        None => None,
        Some(user) => Some(guild.member(&ctx.http, user).await.unwrap()),
    }
}

async fn reaction_activate(ctx: &Context, reaction: &Reaction) {
    let data = ctx.data.read().await;
    let db = data
        .get::<RoleReactionsDatabaseContainer>()
        .expect("Expected RoleReactionsDatabaseContainer in TypeMap");

    if let Some(guild) = reaction.guild_id {
        if let Some(mut member) = get_member_from_partial(ctx, &guild, &reaction.member).await {
            if let ReactionType::Custom { id, .. } = reaction.emoji {
                let role_reaction = db.get_role_reaction(*reaction.channel_id.as_u64(), *id.as_u64())
                    .await
                    .unwrap_or_else(|error| {
                        println!("Failed to fetch role reaction from database: {:?}", error);
                        None
                    });

                if role_reaction.is_none() {
                    return
                }

                let role_id = role_reaction.and_then(|rr| Some(RoleId(rr.role_id))).unwrap();
                let roles = guild.roles(&ctx.http).await;
                roles.get(&role_id)
                    .and_then(|role| {
                        if member.roles.contains(&role_id) {
                            Some(member.remove_role(&ctx.http, role_id))
                        } else {
                            Some(member.add_role(&ctx.http, role_id))
                        }
                    })
                    .await
                    .unwrap_or_else(|error| {
                        println!("Failed to modify roles: {:?}", error);
                    })
            }
        }
    }
}

#[async_trait]
impl EventHandler for Handler {
    async fn reaction_add(&self, ctx: Context, added_reaction: Reaction) {
        reaction_activate(&ctx, &added_reaction);
    }

    async fn reaction_remove(&self, ctx: Context, removed_reaction: Reaction) {
        reaction_activate(&ctx, &removed_reaction);
    }

    async fn ready(&self, _: Context, ready: Ready) {
        println!(
            "Logged in as {}#{}!",
            ready.user.name, ready.user.discriminator
        );
    }
}
