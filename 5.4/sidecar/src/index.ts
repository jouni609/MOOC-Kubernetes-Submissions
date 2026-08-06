import { writeFile } from "node:fs/promises";

const wwwDir = process.env["WWW_DIR"] ?? "/usr/share/nginx/html";
const wikiUrl =
  process.env["WIKI_URL"] ?? "https://en.wikipedia.org/wiki/Special:Random";

const sleep = (ms: number): Promise<void> =>
  new Promise((resolve) => {
    setTimeout(resolve, ms);
  });

const randomWaitMs = (minMinutes: number, maxMinutes: number): number => {
  const min = minMinutes * 60_000;
  const max = maxMinutes * 60_000;
  return min + Math.floor(Math.random() * (max - min + 1));
};

const fetchAndSave = async (): Promise<void> => {
  const response = await fetch(wikiUrl, {
    headers: { "User-Agent": "wiki-sidecar/5.4" },
    redirect: "follow",
  });
  if (!response.ok) {
    throw new Error(`HTTP ${String(response.status)}`);
  }
  const html = await response.text();
  await writeFile(`${wwwDir}/index.html`, html, "utf8");
  console.log(
    `saved ${response.url} -> ${wwwDir}/index.html (${String(html.length)} bytes)`,
  );
};

const main = async (): Promise<void> => {
  for (;;) {
    const waitMs = randomWaitMs(5, 15);
    console.log(`waiting ${String(Math.round(waitMs / 1000))}s`);
    await sleep(waitMs);
    try {
      await fetchAndSave();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : String(err);
      console.error(`fetch failed: ${message}`);
    }
  }
};

await main();
