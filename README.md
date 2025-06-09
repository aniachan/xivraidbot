# xivraidbot
My discord bot for organising the chaos of FFXIV raiding...

## Features
- Create and manage raid events for FFXIV
- Track player attendance and roles
- Manage raid compositions with different jobs
- Support for timezone conversions
- Automatic reminders for upcoming raids

## How to Run with Docker

### Prerequisites
- [Docker](https://www.docker.com/) installed
- [Docker Compose](https://docs.docker.com/compose/) installed
- A Discord Bot token (create one at [Discord Developer Portal](https://discord.com/developers/applications))

### Setup
1. Clone this repository
   ```
   git clone https://github.com/yourusername/xivraidbot.git
   cd xivraidbot
   ```

2. Create a `.env` file from the example
   ```
   cp .env.example .env
   ```

3. Edit the `.env` file with your Discord Bot credentials
   ```
   DISCORD_BOT_TOKEN=your_discord_bot_token_here
   DISCORD_DEV_GUILD_ID=your_development_guild_id_here
   ```

4. Build and start the containers
   ```
   docker-compose up -d
   ```

5. Check the logs to ensure everything is running correctly
   ```
   docker-compose logs -f
   ```

### Stopping the Bot
```
docker-compose down
```

### Updating the Bot
```
git pull
docker-compose build
docker-compose up -d
```

## Development Setup

### Prerequisites
- .NET 9.0 SDK
- PostgreSQL database

### Running Locally
1. Clone the repository
2. Update appsettings.json with your database connection and Discord bot token
3. Run the application:
   ```
   cd XIVRaidBot
   dotnet run
   ```

## License
See the [LICENSE](LICENSE) file for details.
