use serenity::{
    async_trait,
    model::{gateway::Ready},
    prelude::*,
};

pub struct Handler;

#[async_trait]
impl EventHandler for Handler {
    async fn ready(&self, _: Context, ready: Ready) {
        println!("Logged in as {}#{}!", ready.user.name, ready.user.discriminator);
    }
}
