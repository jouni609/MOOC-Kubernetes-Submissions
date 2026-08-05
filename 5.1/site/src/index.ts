import http from "node:http";

const port = Number(process.env["PORT"] ?? "8080");
const websiteUrl = process.env["WEBSITE_URL"] ?? "";

let cachedHtml =
  "<html><body><h1>DummySite</h1><p>No content yet.</p></body></html>";

async function fetchWebsite(): Promise<void> {
  if (!websiteUrl) {
    cachedHtml =
      "<html><body><h1>DummySite</h1><p>WEBSITE_URL is not set.</p></body></html>";
    console.log("[WARN] WEBSITE_URL is empty");
    return;
  }

  console.log(`[INFO] Fetching ${websiteUrl}`);
  try {
    const response = await fetch(websiteUrl, {
      headers: { "User-Agent": "dummysite-app/5.1" },
      redirect: "follow",
    });
    const text = await response.text();
    cachedHtml = text;
    console.log(
      `[INFO] Fetched ${websiteUrl} status=${response.status} bytes=${text.length}`,
    );
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    cachedHtml = `<html><body><h1>DummySite fetch failed</h1><p>${message}</p><p>URL: ${websiteUrl}</p></body></html>`;
    console.error(`[ERROR] Failed to fetch ${websiteUrl}: ${message}`);
  }
}

const server = http.createServer((req, res) => {
  if (req.url === "/healthz") {
    res.writeHead(200, { "Content-Type": "text/plain; charset=utf-8" });
    res.end("ok");
    return;
  }

  res.writeHead(200, { "Content-Type": "text/html; charset=utf-8" });
  res.end(cachedHtml);
});

async function main(): Promise<void> {
  await fetchWebsite();
  server.listen(port, "0.0.0.0", () => {
    console.log(`[INFO] DummySite serving on :${port} url=${websiteUrl}`);
  });
}

main().catch((err: unknown) => {
  console.error("[FATAL]", err);
  process.exit(1);
});
