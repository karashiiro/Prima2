use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize)]
pub struct RoleReactionInfo {
    pub guild_id: String,
    pub channel_id: String,
    pub emoji_id: String,
    pub role_id: String,
    pub eureka: Option<bool>,
    pub bozja: Option<bool>,
}
