# Project Working Rules

- Keep every visible scene element easy to edit by hand in the Unity Inspector and Hierarchy.
- Prefer small, clearly named GameObjects and components over opaque runtime-generated visuals.
- Expose gameplay timing, colors, sizes, limits, copy, and integration settings through serialized fields or ScriptableObject assets.
- Keep Twitch/network code independent from the balloon game loop so future chat-driven mini games can reuse it.
- Keep local keyboard testing available without requiring a Twitch connection.
- Add code and components when they improve clarity, editability, or safe iteration; minimizing file count is not a goal.
- Do not require paid assets or third-party tween packages for the core prototype.
- Never store Twitch OAuth tokens in version-controlled assets.

