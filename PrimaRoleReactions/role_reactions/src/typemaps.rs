use db_access::database::RoleReactionsDatabase;
use serenity::prelude::TypeMapKey;

pub struct RoleReactionsDatabaseContainer;

impl TypeMapKey for RoleReactionsDatabaseContainer {
    type Value = RoleReactionsDatabase;
}