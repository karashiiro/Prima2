use mongodb::{bson::doc, options::ClientOptions, Client, Collection, Database};

use crate::role_reaction_info::RoleReactionInfo;
use futures::TryStreamExt;

const ROLE_REACTION_COLLECTION: &str = "RoleReactions";

pub struct RoleReactionsDatabase {
    db: Database,
}

impl RoleReactionsDatabase {
    pub async fn new(application_name: &str, db_name: &str) -> Self {
        let mut client_options = ClientOptions::parse("mongodb://localhost").await.unwrap();
        client_options.app_name = Some(application_name.parse().unwrap());

        let client = Client::with_options(client_options).unwrap_or_else(|error| {
            panic!("Failed to connect to MongoDB server: {:?}", error);
        });

        client
            .database("admin")
            .run_command(doc! {"ping": 1}, None)
            .await
            .unwrap_or_else(|error| {
                panic!("Failed to connect to MongoDB server: {:?}", error);
            });

        let db = client.database(&db_name);

        Self { db }
    }

    pub async fn get_role_reactions(
        &self,
        guild_id: u64,
    ) -> Result<Vec<RoleReactionInfo>, mongodb::error::Error> {
        let collection = self.get_collection(ROLE_REACTION_COLLECTION);
        let filter = doc! { "guild_id": guild_id.to_string() };

        let mut cursor = collection.find(filter, None).await?;

        let mut results = vec![];
        while let Some(role_reaction) = cursor.try_next().await? {
            results.push(role_reaction);
        }

        Ok(results)
    }

    pub async fn get_role_reaction(
        &self,
        channel_id: u64,
        emote_id: u64,
    ) -> Result<Option<RoleReactionInfo>, mongodb::error::Error> {
        let collection = self.get_collection(ROLE_REACTION_COLLECTION);
        let filter =
            doc! { "channel_id": channel_id.to_string(), "emote_id": emote_id.to_string() };

        collection.find_one(filter, None).await
    }

    pub async fn add_role_reaction(
        &self,
        rr_info: RoleReactionInfo,
    ) -> Result<(), mongodb::error::Error> {
        let collection = self.get_collection(ROLE_REACTION_COLLECTION);
        let filter = doc! {
            "guild_id": rr_info.guild_id.to_string(),
            "channel_id": rr_info.channel_id.to_string(),
            "emoji_id": rr_info.emoji_id.to_string(),
            "role_id": rr_info.role_id.to_string(),
        };

        let existing = collection.find_one(filter, None).await?;
        match existing {
            None => {
                collection.insert_one(rr_info, None).await?;
            }
            Some(_) => {}
        }

        Ok(())
    }

    pub async fn remove_role_reaction(
        &self,
        rr_info: RoleReactionInfo,
    ) -> Result<(), mongodb::error::Error> {
        let collection = self.get_collection(ROLE_REACTION_COLLECTION);
        let filter = doc! {
            "guild_id": rr_info.guild_id.to_string(),
            "channel_id": rr_info.channel_id.to_string(),
            "emoji_id": rr_info.emoji_id.to_string(),
            "role_id": rr_info.role_id.to_string(),
        };

        let delete_filter = filter.clone();

        let existing = collection.find_one(filter, None).await?;
        match existing {
            None => {}
            Some(_) => {
                collection.delete_one(delete_filter, None).await?;
            }
        }

        Ok(())
    }

    fn get_collection(&self, name: &str) -> Collection<RoleReactionInfo> {
        self.db.collection::<RoleReactionInfo>(name)
    }
}
