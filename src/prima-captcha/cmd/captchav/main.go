package main

import (
	"log"

	"github.com/gofiber/fiber/v2"

	"github.com/PrimaShouji/captcha-verification/pkg/captchagen"
)

type VerifyResult struct {
	Result bool `json:"result"`
}

func main() {
	// Initialize API
	app := fiber.New()

	app.Get("/generate/:id", func(c *fiber.Ctx) error {
		id := c.Params("id")
		r, err := captchagen.Generate(id)
		if err != nil {
			log.Printf("Failed to generate CAPTCHA image for ID %s: %v\n", id, err)
			return c.SendStatus(500)
		}

		log.Printf("Generated CAPTCHA image for ID %s\n", id)

		c.Context().SetContentType("image/png")
		return c.SendStream(r)
	})

	app.Get("/verify/:id/:test", func(c *fiber.Ctx) error {
		id := c.Params("id")
		test := c.Params("test")

		verified := captchagen.Verify(id, test)

		log.Printf("Got verification request for CAPTCHA image for ID %s: %s - Result: %t\n", id, test, verified)

		return c.JSON(&VerifyResult{
			Result: verified,
		})
	})

	// Listen for requests
	log.Println("Application started")
	app.Listen(":2539")
}
