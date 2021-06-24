use crate::typemaps::RoleReactionsDatabaseContainer;
use serenity::model::prelude::{Member, Reaction, ReactionType, RoleId};
use serenity::prelude::Context;
use serenity::Error;

pub async fn reaction_activate(ctx: &Context, reaction: &Reaction) -> Result<(), Error> {
    let data = ctx.data.read().await;
    let db = data
        .get::<RoleReactionsDatabaseContainer>()
        .expect("Expected RoleReactionsDatabaseContainer in TypeMap");

    if let Some(guild) = reaction.guild_id {
        let mut member: Member;
        if let Some(user) = reaction.user_id {
            member = guild.member(&ctx.http, user).await?;
        } else {
            println!("Failed to fetch guild member.");
            return Ok(());
        }

        if let ReactionType::Custom { id, .. } = reaction.emoji {
            if let Some(role_reaction) = db
                .get_role_reaction(reaction.channel_id.as_u64(), id.as_u64())
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
                    println!(
                        "Removed role {} from {}#{}",
                        role.unwrap().name,
                        member.user.name,
                        member.user.discriminator
                    );
                } else {
                    member.add_role(&ctx.http, role_id).await?;
                    println!(
                        "Added role {} to {}#{}",
                        role.unwrap().name,
                        member.user.name,
                        member.user.discriminator
                    );
                }
            }
        }
    }

    Ok(())
}
