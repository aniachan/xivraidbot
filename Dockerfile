FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy csproj and restore as distinct layers
COPY XIVRaidBot/*.csproj .
RUN dotnet restore

# Copy everything else and build
COPY XIVRaidBot/ .
RUN dotnet publish -c Release -o /app --no-restore

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app .
COPY XIVRaidBot/Resources ./Resources

# Set environment variables (these will be overridden by docker-compose or command line)
ENV ConnectionStrings__DefaultConnection="Server=db;Port=5432;Database=xivraidbot;User Id=xivraidbot;Password=xivraidbot_password;"
ENV DiscordBot__Token="YOUR_BOT_TOKEN_HERE"
ENV DiscordBot__Prefix="!"
ENV DiscordBot__DevGuildId=""

# Set the timezone to UTC by default (can be overridden)
ENV TZ=UTC
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

# Create a non-root user to run the application
RUN adduser --disabled-password --gecos "" appuser
USER appuser

ENTRYPOINT ["dotnet", "XIVRaidBot.dll"]
