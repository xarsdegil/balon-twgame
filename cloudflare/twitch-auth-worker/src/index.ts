interface Env {
  OAUTH_SESSIONS: KVNamespace;
  TWITCH_CLIENT_ID: string;
  TWITCH_CLIENT_SECRET: string;
}

interface OAuthRecord {
  state: string;
  pollSecretHash: string;
  status: "pending" | "complete" | "failed";
  createdAt: number;
  callbackCopy: CallbackCopy;
  token?: TwitchTokenResponse;
  error?: string;
}

interface CallbackCopy {
  successTitle: string;
  successMessage: string;
  failureTitle: string;
  failureMessage: string;
}

interface TwitchTokenResponse {
  access_token: string;
  refresh_token: string;
  expires_in: number;
  scope: string[];
  token_type: string;
}

const TWITCH_AUTHORIZE_URL = "https://id.twitch.tv/oauth2/authorize";
const TWITCH_TOKEN_URL = "https://id.twitch.tv/oauth2/token";
const SESSION_TTL_SECONDS = 600;
const REQUIRED_SCOPES = "chat:read chat:edit";

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);

    if (request.method === "GET" && url.pathname === "/health")
      return json({ ok: true });
    if (request.method === "POST" && url.pathname === "/oauth/start")
      return startAuthorization(request, env);
    if (request.method === "GET" && url.pathname === "/oauth/twitch/callback")
      return finishAuthorization(request, env);
    if (request.method === "POST" && url.pathname === "/oauth/status")
      return readAuthorizationResult(request, env);
    if (request.method === "POST" && url.pathname === "/oauth/refresh")
      return refreshAccessToken(request, env);

    return json({ error: "not_found" }, 404);
  }
} satisfies ExportedHandler<Env>;

async function startAuthorization(request: Request, env: Env): Promise<Response> {
  const clientId = cleanCredential(env.TWITCH_CLIENT_ID);
  const submittedCopy = await request.json<Partial<CallbackCopy>>().catch(() => ({}));
  const transactionId = randomToken(24);
  const pollSecret = randomToken(32);
  const state = `${transactionId}.${randomToken(24)}`;
  const redirectUri = `${new URL(request.url).origin}/oauth/twitch/callback`;
  const record: OAuthRecord = {
    state,
    pollSecretHash: await sha256(pollSecret),
    status: "pending",
    createdAt: Date.now(),
    callbackCopy: normalizeCallbackCopy(submittedCopy)
  };

  await env.OAUTH_SESSIONS.put(sessionKey(transactionId), JSON.stringify(record), {
    expirationTtl: SESSION_TTL_SECONDS
  });

  const authorizeUrl = new URL(TWITCH_AUTHORIZE_URL);
  authorizeUrl.searchParams.set("response_type", "code");
  authorizeUrl.searchParams.set("client_id", clientId);
  authorizeUrl.searchParams.set("redirect_uri", redirectUri);
  authorizeUrl.searchParams.set("scope", REQUIRED_SCOPES);
  authorizeUrl.searchParams.set("state", state);

  return json({ transactionId, pollSecret, authorizationUrl: authorizeUrl.toString() });
}

async function finishAuthorization(request: Request, env: Env): Promise<Response> {
  const url = new URL(request.url);
  const state = url.searchParams.get("state") ?? "";
  const transactionId = state.split(".", 1)[0];
  const record = await readRecord(env, transactionId);
  if (!record || !state || !timingSafeEqual(state, record.state))
    return callbackResponse("", "", 400);
  const callbackCopy = record.callbackCopy ?? normalizeCallbackCopy({});

  const error = url.searchParams.get("error");
  if (error) {
    record.status = "failed";
    record.error = error;
    await writeRecord(env, transactionId, record);
    return callbackResponse(
      callbackCopy.failureTitle,
      callbackCopy.failureMessage,
      200
    );
  }

  const code = url.searchParams.get("code");
  if (!code)
    return callbackResponse(
      callbackCopy.failureTitle,
      callbackCopy.failureMessage,
      400
    );

  const redirectUri = `${url.origin}/oauth/twitch/callback`;
  const clientId = cleanCredential(env.TWITCH_CLIENT_ID);
  const clientSecret = cleanCredential(env.TWITCH_CLIENT_SECRET);
  const tokenResponse = await fetch(TWITCH_TOKEN_URL, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams({
      client_id: clientId,
      client_secret: clientSecret,
      code,
      grant_type: "authorization_code",
      redirect_uri: redirectUri
    })
  });

  if (!tokenResponse.ok) {
    record.status = "failed";
    record.error = "token_exchange_failed";
  } else {
    record.status = "complete";
    record.token = await tokenResponse.json<TwitchTokenResponse>();
  }
  await writeRecord(env, transactionId, record);
  const copy = record.status === "complete"
    ? [callbackCopy.successTitle, callbackCopy.successMessage]
    : [callbackCopy.failureTitle, callbackCopy.failureMessage];
  return callbackResponse(copy[0], copy[1], 200);
}

async function readAuthorizationResult(request: Request, env: Env): Promise<Response> {
  const body = await request.json<{ transactionId?: string; pollSecret?: string }>();
  const transactionId = body.transactionId ?? "";
  const record = await readRecord(env, transactionId);
  if (!record || !body.pollSecret)
    return json({ error: "invalid_session" }, 401);
  if (!timingSafeEqual(await sha256(body.pollSecret), record.pollSecretHash))
    return json({ error: "invalid_session" }, 401);
  if (record.status === "pending")
    return json({ status: "pending" }, 202);

  await env.OAUTH_SESSIONS.delete(sessionKey(transactionId));
  if (record.status === "failed")
    return json({ status: "failed", error: record.error ?? "authorization_failed" }, 400);
  return json({ status: "complete", token: record.token });
}

async function refreshAccessToken(request: Request, env: Env): Promise<Response> {
  const body = await request.json<{ refreshToken?: string }>();
  if (!body.refreshToken)
    return json({ error: "refresh_token_required" }, 400);

  const response = await fetch(TWITCH_TOKEN_URL, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams({
      client_id: cleanCredential(env.TWITCH_CLIENT_ID),
      client_secret: cleanCredential(env.TWITCH_CLIENT_SECRET),
      grant_type: "refresh_token",
      refresh_token: body.refreshToken
    })
  });
  return new Response(response.body, {
    status: response.status,
    headers: { "Content-Type": "application/json", "Cache-Control": "no-store" }
  });
}

async function readRecord(env: Env, transactionId: string): Promise<OAuthRecord | null> {
  if (!transactionId)
    return null;
  return env.OAUTH_SESSIONS.get<OAuthRecord>(sessionKey(transactionId), "json");
}

async function writeRecord(env: Env, transactionId: string, record: OAuthRecord): Promise<void> {
  await env.OAUTH_SESSIONS.put(sessionKey(transactionId), JSON.stringify(record), {
    expirationTtl: SESSION_TTL_SECONDS
  });
}

function sessionKey(transactionId: string): string {
  return `oauth:${transactionId}`;
}

function cleanCredential(value: string): string {
  return value.replace(/^\uFEFF/, "").trim();
}

function randomToken(byteLength: number): string {
  const bytes = crypto.getRandomValues(new Uint8Array(byteLength));
  return base64Url(bytes);
}

async function sha256(value: string): Promise<string> {
  const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(value));
  return base64Url(new Uint8Array(digest));
}

function base64Url(bytes: Uint8Array): string {
  let binary = "";
  for (const byte of bytes)
    binary += String.fromCharCode(byte);
  return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/g, "");
}

function timingSafeEqual(left: string, right: string): boolean {
  if (left.length !== right.length)
    return false;
  let difference = 0;
  for (let index = 0; index < left.length; index++)
    difference |= left.charCodeAt(index) ^ right.charCodeAt(index);
  return difference === 0;
}

function callbackResponse(title: string, message: string, status: number): Response {
  const safeTitle = escapeHtml(title);
  const safeMessage = escapeHtml(message);
  const html = `<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width,initial-scale=1">
  <title>${safeTitle}</title>
  <style>
    :root{color-scheme:dark;font-family:Inter,system-ui,-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif}
    *{box-sizing:border-box}
    body{margin:0;min-height:100vh;display:grid;place-items:center;background:#0e1225;color:#f7f4ff;padding:24px}
    main{width:min(560px,100%);padding:48px 38px;text-align:center;border:1px solid #2b3255;background:#1b1f36;box-shadow:0 24px 70px #05071180}
    .mark{width:56px;height:56px;margin:0 auto 24px;display:grid;place-items:center;background:#9146ff;font-size:30px;font-weight:800}
    h1{margin:0 0 14px;font-size:clamp(26px,6vw,38px);letter-spacing:.04em}
    p{margin:0;color:#a4a9c2;font-size:18px;line-height:1.6}
  </style>
</head>
<body><main><div class="mark">✓</div><h1>${safeTitle}</h1><p>${safeMessage}</p></main></body>
</html>`;
  return new Response(html, {
    status,
    headers: {
      "Content-Type": "text/html; charset=utf-8",
      "Cache-Control": "no-store",
      "Referrer-Policy": "no-referrer"
    }
  });
}

function normalizeCallbackCopy(value: Partial<CallbackCopy>): CallbackCopy {
  return {
    successTitle: normalizeText(value.successTitle, 120),
    successMessage: normalizeText(value.successMessage, 300),
    failureTitle: normalizeText(value.failureTitle, 120),
    failureMessage: normalizeText(value.failureMessage, 300)
  };
}

function normalizeText(value: unknown, maximumLength: number): string {
  return typeof value === "string" ? value.trim().slice(0, maximumLength) : "";
}

function escapeHtml(value: string): string {
  return value
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/\"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

function json(value: unknown, status = 200): Response {
  return Response.json(value, {
    status,
    headers: { "Cache-Control": "no-store" }
  });
}
