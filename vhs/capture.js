const { chromium } = require('playwright');
const fs = require('fs');

const URLS = [
  "http://localhost:5287",
  "http://localhost:5287/cli",
  "http://localhost:5287/yaml",
  "http://localhost:5287/hardware/tree",
  "http://localhost:5287/servers/list",
  "http://localhost:5287/resources/hardware/proxmox-node01",
  "http://localhost:5287/systems/list",
  "http://localhost:5287/services/list"
];

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({
    viewport: { width: 1366, height: 768 }
  });

  if (!fs.existsSync("./webui_screenshots"))
    fs.mkdirSync("./webui_screenshots");

  for (const url of URLS) {
    const filename = url.replace(/^https?:\/\//, '').replace(/\//g, '_') + ".png";
    console.log("Capturing", url);

    await page.goto(url, {
      waitUntil: "networkidle",
      timeout: 30000
    });

    // extra settle time for SPA hydration
    await page.waitForTimeout(2000);

    await page.screenshot({
      path: `webui_screenshots/${filename}`,
      fullPage: false
    });
  }

  await browser.close();
})();
