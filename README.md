TrafToGoogleSync
=================

This small console app demonstrates creating events in Google Calendar using OAuth2 desktop flow.

Setup
-----
1. Create or select a Google Cloud project at https://console.cloud.google.com
2. Enable the Google Calendar API for the project.
3. Create OAuth 2.0 Client ID -> Application Type: Desktop app.
4. Download the JSON credentials and place it at one of the following locations:
   - %APPDATA%\TrafToGoogleSync\credentials.json
   - (project root) Source/TrafToGoogleSync/credentials.json

Do not commit credentials.json to source control. It's included in `.gitignore`.

Run
---
From a PowerShell prompt:

    cd Source\TrafToGoogleSync
    dotnet restore; dotnet run

The first run opens a browser window to consent. Tokens are stored in a local token-store folder under LocalAppData.

Notes
-----
- The app uses Google.Apis.Auth and Google.Apis.Calendar.v3.
- Tokens are persisted with FileDataStore and refreshed automatically by the client library.

