# Balloon Twitch Auth Worker

Cloudflare Worker backend for Twitch Authorization Code OAuth. Twitch client secrets never enter the Unity project or Git history.

## Endpoints

- `POST /oauth/start`: creates a ten-minute authorization transaction.
- `GET /oauth/twitch/callback`: registered Twitch OAuth redirect URL.
- `POST /oauth/status`: one-time token handoff to the Unity client.
- `POST /oauth/refresh`: refreshes a Twitch user access token without exposing the client secret.
- `GET /health`: deployment health check.

## Secret handling

Set `TWITCH_CLIENT_ID` and `TWITCH_CLIENT_SECRET` with `wrangler secret put`. Never put real values in `wrangler.jsonc`, `.dev.vars.example`, Unity assets, or source control.
