use serenity::{
    async_trait,
    model::{channel::Message, gateway::Ready},
    prelude::*,
};
use db_access::database::RoleReactionsDatabase;

pub struct Handler {
    db: RoleReactionsDatabase,
}

impl Handler {
    pub fn new(db: RoleReactionsDatabase) -> Self {
        Self { db }
    }
}

#[async_trait]
impl EventHandler for Handler {
    //noinspection SpellCheckingInspection
    async fn message(&self, ctx: Context, message: Message) {
        // Commands are only applicable to guilds
        if message.guild_id.is_none() {
            return;
        }

        let guild_id = *message.guild_id.unwrap().as_u64();

        if message.content == "~rolereactions" {
            match self.db.get_role_reactions(guild_id).await {
                Ok(role_reactions) => {
                    for _ in role_reactions {
                        if let Err(why) = message.channel_id.say(&ctx.http, "A role reaction.").await {
                            println!("Error sending message: {:?}", why);
                        }
                    }
                }
                Err(error) => {
                    println!("Failed to fetch role reactions for guild: {:?}", error);
                }
            }
        }
    }

    async fn ready(&self, _: Context, ready: Ready) {
        println!("Logged in as {}!", ready.user.name);
    }
}
