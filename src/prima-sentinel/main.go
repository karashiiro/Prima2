package main

import (
	"bytes"
	_ "embed"
	"fmt"
	"html/template"
	"io/ioutil"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"

	"github.com/bwmarrin/discordgo"
	"github.com/h2non/filetype"
	"github.com/karashiiro/prima-sentinel/config"
	"github.com/karashiiro/prima-sentinel/detection/h264"
	"github.com/karashiiro/prima-sentinel/detection/img"
	"github.com/karashiiro/prima-sentinel/senutils"
)

type maliciousMessageInfo struct {
	Author      string
	ChannelID   string
	ChannelName string
	Filetype    string
	Filename    string
	Problem     string
}

//go:embed deletion_embed.md
var deletionEmbed string
var deletionEmbedTemplate *template.Template

var problemKinds = map[string]string{
	"resolution":       "This video might crash Discord clients with hardware acceleration enabled due to mid-video resizes.",
	"color_space":      "This video will crash Discord clients with hardware acceleration enabled due to mid-video color space changes.",
	"windows_defender": "This file will be flagged by Windows Defender after it is cached.",
}

func deleteMessage(s *discordgo.Session, m *discordgo.MessageCreate, problemKind string, filename string, data []byte) {
	if err := s.ChannelMessageDelete(m.ChannelID, m.ID); err != nil {
		log.Println(err)
	}

	log.Println("Deleted malicious message " + fmt.Sprint(m.ID))

	if channelID, ok := config.ReportChannelIDs[m.GuildID]; ok {
		// Parse MIME type
		fts := "(Unable to retrieve)"
		ft, err := filetype.Match(data)
		if err == nil {
			fts = ft.MIME.Value
		}

		// Copy data to reader to send as an attachment
		var dataReader bytes.Buffer
		dataReader.Write(data)

		// Make embed
		channelName := "(Unable to retrieve)"
		c, err := s.Channel(m.ChannelID)
		if err == nil {
			channelName = c.Name
		}

		mmi := &maliciousMessageInfo{
			Author:      senutils.GetUsername(m.Author),
			ChannelID:   m.ChannelID,
			ChannelName: channelName,
			Filetype:    fts,
			Filename:    filename,
			Problem:     problemKinds[problemKind],
		}

		var templateResult bytes.Buffer
		err = deletionEmbedTemplate.Execute(&templateResult, mmi)
		if err != nil {
			log.Println(err)
		}

		embed := &discordgo.MessageEmbed{
			Title:       "Deleted potentially malicious message",
			Description: templateResult.String(),
			Color:       0xA81C1C,
			Footer: &discordgo.MessageEmbedFooter{
				Text: "Deleted message ID: " + fmt.Sprint(m.ID),
			},
		}

		// Send embed and raw attachment
		_, err = s.ChannelMessageSendComplex(channelID, &discordgo.MessageSend{
			Embed: embed,
			Files: []*discordgo.File{
				{
					Name:        "FILE_RAW",
					ContentType: "application/octet-stream",
					Reader:      &dataReader,
				},
			},
		})
		if err != nil {
			log.Println(err)
		}
	}
}

func heuristic(s *discordgo.Session, m *discordgo.MessageCreate, filename string, url string) {
	res, err := http.Get(url)
	if err != nil {
		log.Println(err)
		return
	}
	defer res.Body.Close()

	if res.StatusCode != http.StatusOK {
		log.Println("Failed to download attachment, skipping.")
		return
	}

	data, err := ioutil.ReadAll(res.Body)
	if err != nil {
		log.Println(err)
		return
	}

	if filetype.IsVideo(data) {
		changesColorSpace, err := h264.DoesVideoColorSpaceChange(data)
		if err != nil {
			log.Println(err)
		}

		changesResolution, err := h264.DoesVideoResolutionChange(data)
		if err != nil {
			log.Println(err)
		}

		if changesColorSpace {
			deleteMessage(s, m, "color_space", filename, data)
		} else if changesResolution {
			deleteMessage(s, m, "resolution", filename, data)
		} else {
			log.Println("No issues found.")
		}
	} else if filetype.IsImage(data) {
		willFlagWindowsDefender, err := img.DoesImageTriggerWindowsDefender(data)
		if err != nil {
			log.Println(err)
		}

		if willFlagWindowsDefender {
			deleteMessage(s, m, "windows_defender", filename, data)
		} else {
			log.Println("No issues found.")
		}
	} else {
		log.Println("No issues found.")
	}
}

func main() {
	det, err := template.New("deletion_embed").Parse(deletionEmbed)
	if err != nil {
		log.Fatalln(err)
	}
	deletionEmbedTemplate = det

	discord, err := discordgo.New("Bot " + os.Getenv("PRIMA_BOT_TOKEN"))
	if err != nil {
		log.Fatalln(err)
	}

	discord.AddHandler(func(s *discordgo.Session, m *discordgo.MessageCreate) {
		if m.Author.ID == s.State.User.ID {
			return
		}

		if !senutils.Contains(config.AcceptedGuildIDs, m.GuildID) {
			return
		}

		// Gfycat links, Twitter posts, etc.
		for _, embed := range m.Embeds {
			if embed.Video != nil {
				go heuristic(s, m, "(Unable to retrieve)", embed.Video.URL)
			}
		}

		// Regular message attachments
		for _, attachment := range m.Attachments {
			log.Println("Scanning attachment.")
			go heuristic(s, m, attachment.Filename, attachment.ProxyURL)
		}
	})

	discord.Identify.Intents = discordgo.IntentsGuildMessages
	if err := discord.Open(); err != nil {
		log.Fatalln(err)
	}
	defer discord.Close()

	log.Println("Logged in as " + senutils.GetUsername(discord.State.User) + "!")

	sc := make(chan os.Signal, 1)
	signal.Notify(sc, syscall.SIGINT, syscall.SIGTERM)
	<-sc
}
