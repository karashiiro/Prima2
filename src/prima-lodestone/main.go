package main

import (
	"encoding/json"
	"flag"
	"strconv"

	fiber "github.com/gofiber/fiber/v2"
	"github.com/karashiiro/bingode"
	godestone "github.com/xivapi/godestone/v2"
)

func main() {
	port := flag.Uint("port", 7652, "server binding port")
	flag.Parse()

	bin := bingode.New()
	scraper := godestone.NewScraper(bin, godestone.EN)

	app := fiber.New()
	character := app.Group("/character")

	character.Get("/search/:world/:name", func(ctx *fiber.Ctx) error {
		results := scraper.SearchCharacters(godestone.CharacterOptions{
			Name:  ctx.Params("name"),
			World: ctx.Params("world"),
		})

		arr := make([]godestone.CharacterSearchResult, 0)
		for c := range results {
			if c.Error != nil {
				return ctx.SendStatus(500)
			}

			arr = append(arr, *c)
		}

		rawData, err := json.Marshal(arr)
		if err != nil {
			return ctx.SendStatus(500)
		}

		ctx.Type("json")
		return ctx.Send(rawData)
	})

	character.Get("/:id", func(ctx *fiber.Ctx) error {
		id, err := strconv.ParseUint(ctx.Params("id"), 10, 32)
		if err != nil {
			return ctx.SendStatus(404)
		}

		c, err := scraper.FetchCharacter(uint32(id))
		if err != nil {
			return ctx.SendStatus(404)
		}

		rawData, err := json.Marshal(c)
		if err != nil {
			return ctx.SendStatus(500)
		}

		ctx.Type("json")
		return ctx.Send(rawData)
	})

	character.Get("/:id/achievements", func(ctx *fiber.Ctx) error {
		id, err := strconv.ParseUint(ctx.Params("id"), 10, 32)
		if err != nil {
			return ctx.SendStatus(404)
		}

		a, aai, err := scraper.FetchCharacterAchievements(uint32(id))
		if err != nil {
			return ctx.SendStatus(500)
		}

		if aai.Private {
			return ctx.SendStatus(403)
		}

		rawData, err := json.Marshal(a)
		if err != nil {
			return ctx.SendStatus(500)
		}

		ctx.Type("json")
		return ctx.Send(rawData)
	})

	character.Get("/:id/mounts", func(ctx *fiber.Ctx) error {
		id, err := strconv.ParseUint(ctx.Params("id"), 10, 32)
		if err != nil {
			return ctx.SendStatus(404)
		}

		mounts, err := scraper.FetchCharacterMounts(uint32(id))
		if err != nil {
			return ctx.SendStatus(404)
		}

		rawData, err := json.Marshal(mounts)
		if err != nil {
			return ctx.SendStatus(500)
		}

		ctx.Type("json")
		return ctx.Send(rawData)
	})

	app.Listen(":" + strconv.Itoa(int(*port)))
}
