package senutils

import "github.com/bwmarrin/discordgo"

func Contains(arr []string, element string) bool {
	for _, guildID := range arr {
		if guildID == element {
			return true
		}
	}
	return false
}

func GetUsername(a *discordgo.User) string {
	return a.Username + "#" + a.Discriminator
}
