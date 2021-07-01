use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize)]
pub struct SlashCommandInfo {
    pub name: String,
    pub command_id: String,
}
