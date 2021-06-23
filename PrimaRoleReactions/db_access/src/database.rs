use mongodb::{bson::doc, options::ClientOptions, Client, Database, FindOptions};

use crate::role_reaction_info::RoleReactionInfo;
use futures::TryStreamExt;

enum DatabaseError {
    CollectionNotFound,
}

pub struct RoleReactionsDatabase {
    client: Client,
    db: Database,
}

impl RoleReactionsDatabase {
    pub async fn new(application_name: String, db_name: String) -> Self {
        let mut client_options = ClientOptions::parse("mongodb://localhost").await.unwrap();
        client_options.app_name = Some(application_name);

        let client =
            Client::with_options(client_options).expect("Expected successful Client instantiation");

        client
            .database("admin")
            .run_command(doc! {"ping": 1}, None)
            .await
            .expect("Expected successful connection to the local MongoDB server");

        let db = client.database(&db_name);

        Self { client, db }
    }

    pub async fn get_role_reactions(&self, guild_id: u64) -> Result<Vec<RoleReactionInfo>, ()> {
        let collection = self.db.collection::<RoleReactionInfo>("RoleReactions");
        let filter = doc! { "guild_id": guild_id };
        let options = FindOptions::builder().build();

        let mut cursor = collection.find(filter, options)
            .await
            .unwrap();
        
        let results = vec![];
        while let Some(role_reaction) = cursor.try_next().await? {
            results.add(role_reaction);
        }

        Ok(results)
    }
}
