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
    async fn message(&self, ctx: Context, msg: Message) {
        if msg.content == "~ping" {
            if let Err(why) = msg.channel_id.say(&ctx.http, "Pong!").await {
                println!("Error sending message: {:?}", why);
            }
        }
    }

    async fn ready(&self, _: Context, ready: Ready) {
        println!("Logged in as {}!", ready.user.name);
    }
}
