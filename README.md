# Accord
<p align="center">
    <img alt='Accord Logo' src='branding/readme-banner.png'/>
</p>

## Reach accord, prevent discord, on Discord.

A C# .NET Discord bot with moderation, XP/guild participation and utilities aimed at all users within a guild. This is not a hosted bot, it is intended to be self hosted.

## Fictional AI subscriptions

`/ai` and the existing `!ai` text command return one of the same 200 hardcoded AI pushback quotes through the same subscription workflow. Your first invocation of either automatically enrolls you in **Free Trial of Thought** for that server. Both commands share your daily allowance; switching between them does not bypass the quota. `/ai work` is replaced by `/ai`.

| Plan | Daily quotes | Availability |
|---|---:|---|
| Free Trial of Thought | 5 | Automatically assigned |
| Load-Bearing Plus | 20 | Permanently coming soon |
| Unnecessarily Pro | 100 | Imaginary billing department unavailable |

Before delivering a quote, both `/ai` and `!ai` wait a random 2–5 seconds. For `/ai`, Discord's thinking indicator sells the illusion. The delay is cancellable and does not apply to quota-exhaustion responses.

Each successfully delivered quote costs one credit. Your fifth quote succeeds; further requests receive a public exhaustion message until midnight UTC or until your usage is reset. Failed or cancelled deliveries consume no credits. Concurrent requests share the same allowance; a delivery crossing midnight is charged to its completion day.

Use `/ai-subscription usage` to see your plan, usage, remaining allowance, and next UTC reset; it does not enroll you. `/ai-subscription plans` lists the tiers. `/ai-subscription upgrade` always returns a joke rejection and changes nothing. These three slash commands reply privately; quotes and exhaustion messages are public. Higher plans are display-only and cannot be activated.

Subscriptions and usage live only in process memory, separately for each server and user. Usage rolls over on the next operation after midnight UTC; restarting the process clears everything. Multiple bot processes do not share allowances. This feature performs no database operations and uses no payment service, AI provider, or external AI API key. Other bot features retain their existing dependencies.

Users start with **zero banked resets**. An administrator can grant one with `/ai-subscription grant-reset user:@someone`, after that user has enrolled through `/ai` or `!ai`. Grants add to the reserve without resetting daily usage. Only administrators can grant resets; there is no automatic refill.

`/ai-subscription usage` shows the banked balance. Once the daily quota is exhausted, `/ai-subscription use-reset` spends one banked reset and restores the full daily allowance for both `/ai` and `!ai`, without changing the plan or next midnight reset. It does not send a quote automatically. Trying it with quota remaining, no banked resets, or no subscription spends nothing. Midnight restores daily quota for free, so a banked reset is not spent if the day has already rolled over. Concurrent use attempts cannot spend multiple resets for the same exhausted quota.

Banked resets carry across days without expiring, but disappear with subscriptions on process restart. They are scoped to each server and user. The grant and use slash commands reply privately; text equivalents are `!ai-subscription grant-reset @someone` and `!ai-subscription use-reset`, with the same authorization and accounting rules.

Administrators can still run `/ai-subscription reset user:@someone` to reset that user's daily usage immediately for testing. This does not consume or grant banked resets. The reply is private, the plan stays the same, and both `/ai` and `!ai` receive the restored allowance. Reset waits for any quote currently being delivered to that user before clearing usage. Users without a subscription are left unenrolled. The text equivalent, `!ai-subscription reset @someone`, requires the same Administrator permission.

## How to self host

You can build from source or host via the published Docker image. An example `docker-compose.yml` is below.

```yml
services:
  postgres:
    image: postgres
    restart: always
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}

  bot:
    image: ghcr.io/patrickklaeren/accord:main
    depends_on:
      - postgres
    restart: always
    environment:
      ConnectionStrings__accord: ${CONNECTIONSTRINGS_DATABASE}
      AppBaseUrl: ${APPBASEURL}
      ReplBaseUrl: ${REPLBASEURL}
      PasteBaseUrl: ${PASTEBASEURL}
      Discord__ClientSecret: ${DISCORD_CLIENTID}
      Discord__ClientId: ${DISCORD_CLIENTID}
      Discord__GuildId: ${DISCORD_GUILDID}
      Discord__BotToken: ${DISCORD_BOTTOKEN}
      Shlink__BaseUrl: ${SHLINK_BASEURL}
      Shlink__APIKey: ${SHLINK_APIKEY}
```

Accord can optionally connect to a C# REPL service, via the REPL module. You can self-host this:

```yml
  repl:
    image: ghcr.io/discord-csharp/csharprepl:latest
    environment:
      - ASPNETCORE_URLS=http://+:31337
```

When using the REPL service, Accord also makes use of a "paste service", in this case: https://github.com/matze/wastebin. You can self-host this:

```yml
  wastebin:
    image: quxfoo/wastebin:latest
    ports:
      - "8088:8088"
```

Accord also has a link shortening service via https://github.com/shlinkio/shlink. You can self-host this:

```yml
  shlink:
    image: "ghcr.io/shlinkio/shlink:latest"
    ports:
      - "8080:8080"
```

| Variable                    | Description                                                                                                                                                                                                                                                                                           |
|-----------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ConnectionStrings__accord` | Connection string to the PostgreSQL database.                                                                                                                                                                                                                                                         |
| `AppBaseUrl`                | Base URL to the Accord website. If you do not intend to use the web frontend, set the value to `http://`. Otherwise, configure it to be your main domain that will point to the service.                                                                                                              |
| `ReplBaseUrl`               | Base URL to the REPL service used by the REPL module. If you do not intend to use this service, set the value to `http://`. Otherwise, configure it to point to the appropriate service endpoint.                                                                                                     |
| `PasteBaseUrl`              | Base URL to the paste service. If you do not intend to use this service, set the value to `http://`. Otherwise, configure it to point to the appropriate service endpoint. If the REPL service is configured, but no paste service is provided, the REPL service may fail to post results in Discord. |
| `Discord__ClientId`         | Client ID of your Discord application.                                                                                                                                                                                                                                                                |
| `Discord__ClientSecret`     | Client secret of your Discord application.                                                                                                                                                                                                                                                            |
| `Discord__GuildId`          | Guild (server) snowflake ID that the bot will connect to.                                                                                                                                                                                                                                             |
| `Discord__BotToken`         | Bot token of your Discord application.                                                                                                                                                                                                                                                                |
| `Shlink__BaseUrl`           | Base URL to the Shlink instance for link shortening. If you do not intend to use this service, set the value to `http://`. Otherwise, configure it to point to the appropriate service endpoint.                                                                                                      |
| `Shlink__ApiKey`            | API Key generated in your Shlink instance in order for the Rest API to authenticate requests.                                                                                                                                                                                                         |

This bot is intended for **single-guild** usage.

## Timed requested roles

Administrators can enable a role with `/permission add-role` by selecting the role and the `CanBeRequested` permission, or through the website's role permissions settings. The bot needs permission to manage that role.

Users run `/get-role` and select a role, then type the displayed C# keyword exactly in the modal. The role ID and expected keyword are carried in the modal's custom ID; pending challenges are not stored. An incorrect answer requires running `/get-role` again. Only roles with a `CanBeRequested` role permission can be granted; user permissions do not enable a role. Roles the user already owns are rejected.

Successful grants are stored in `TimedRoles` with the Discord user ID, role ID, and UTC expiry. The bot checks for expired grants every five seconds, including after a restart, and attempts to remove roles after 30 minutes. It logs revocation success or failure and deletes the grant record after either outcome, including when the user has left the server.

Create an Entity Framework migration for the new `TimedRoles` table before deploying this feature. No migration is included.

## Credits

Notable dependencies for this project include:
- [Remora](https://github.com/Nihlus/Remora.Discord)
- [MediatR](https://github.com/jbogard/MediatR)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)
