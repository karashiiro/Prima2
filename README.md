# Prima2

[![Codacy Badge](https://api.codacy.com/project/badge/Grade/217eae50312a4b2783c57b60d0379c29)](https://app.codacy.com/gh/PrimaShouji/Prima2?utm_source=github.com&utm_medium=referral&utm_content=PrimaShouji/Prima2&utm_campaign=Badge_Grade_Settings)

The Discord bot.

## Dev Environment Setup
Most of the configuration should work out of the box, but the following environment vars will need to be set up to allow the bot to run on your dev environment

| Environment Variable | Required | Description                                                                                                                                                     |
| --- | --- |-----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| PRIMA_BOT_TOKEN | Yes | The login token for your dev environment bot. This will only work correctly with the bot after it is added and given permissions on your dev env discord server |
| PRIMA_GOOGLE_SECRET | No | Path to the Google API secret file                                                                                                                              |
| PRIMA_SESSION_STORE | No | The directory storing the session secret for the Google API client                                                                                              |
| PRIMA_CALENDAR_CONFIG | No | The configuration file for the calendar service                                                                                                                 |
| FFXIV_HOME | No | The FFXIV local installation path if the local client is not installed in the default directory                                                                 |

### Calendar configuration
Calendar configuration is not required to run the bot, only to use the scheduling commands.
The Google API client needs an OAuth2 secret, which [this](https://developers.google.com/api-client-library/dotnet/get_started) guide shows how to create.
Running the bot for the first time after updating the secret will prompt you to grant access to
your calendars to the bot.

The calendars the bot will use are defined in the configuration file at `PRIMA_CALENDAR_CONFIG`.
This is a YAML file with the following schema:
```yaml
calendars:
  drs: calendarid1@group.calendar.google.com
  dr: calendarid2@group.calendar.google.com
  cll: calendarid3@group.calendar.google.com
  bcf: calendarid4@group.calendar.google.com
  ba: calendarid5@group.calendar.google.com
  social: calendarid6@group.calendar.google.com
  zad: calendarid7@group.calendar.google.com
```