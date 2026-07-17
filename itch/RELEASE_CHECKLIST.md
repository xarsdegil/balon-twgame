# Chat Balloon 0.1.0 — itch.io Release Checklist

## Page setup

- [ ] Create the project at `https://itch.io/game/new`.
- [ ] Use the title **Chat Balloon** and URL slug `chat-balloon`.
- [ ] Select **Game**, **Downloadable**, **Windows**, **English**, and **In development**.
- [ ] Paste the short and full descriptions from `ITCH_PAGE.md`.
- [ ] Choose **Free / Name your own price**, unless a different price has been decided.
- [ ] Apply only relevant tags that exist in itch.io's suggested tag picker.
- [ ] Complete itch.io's AI Disclosure section using the disclosure in `ITCH_PAGE.md`.

## Images

- [ ] Upload a 630×500 cover image (315:250 aspect ratio).
- [ ] Upload 3–5 screenshots showing the balloon, player name, pop effect, Twitch sign-in, and settings panel.
- [ ] Ensure screenshots contain no OAuth tokens, browser secrets, personal account data, or Unity Editor chrome.
- [ ] Keep the cover simple and readable at thumbnail size.

## Windows build

- [ ] Make a clean Windows x86_64 build named `Chat Balloon`.
- [ ] Confirm the build contains `Chat Balloon.exe`, `Chat Balloon_Data`, `UnityPlayer.dll`, and required runtime files.
- [ ] Start the build from outside the Unity project folder.
- [ ] Test first-time Twitch sign-in and browser callback.
- [ ] Test automatic login with a saved session.
- [ ] Test `!inflate`, queue behavior, balloon pop, and the outgoing Twitch chat message.
- [ ] Test settings persistence after restarting the game.
- [ ] Test transparent or green-screen capture in OBS.
- [ ] Delete local test data and verify a clean first-run experience.
- [ ] Scan the final archive with Windows Security before upload.

## Upload

- [ ] Upload the complete build directly to itch.io as a `.zip`, or use butler.
- [ ] Mark the upload as **Windows**.
- [ ] Use the butler channel `windows-beta` while the project is in development.
- [ ] Set the itch.io page to **Draft** or **Restricted** for the first end-to-end download test.
- [ ] Download the uploaded build from itch.io and test it before making the page public.

## Suggested butler command

Replace `<build-folder>` and `<itch-username>`:

```powershell
butler push "<build-folder>" <itch-username>/chat-balloon:windows-beta --userversion 0.1.0
```

The itch.io project page must exist before running this command. Keep butler credentials and API keys out of the repository.
