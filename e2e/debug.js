const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch();
    const page = await browser.newPage();
    
    page.on('console', msg => console.log('LOG:', msg.text()));
    page.on('pageerror', error => console.log('ERROR:', error.message));
    page.on('response', response => {
        console.log('RESPONSE:', response.status(), response.url());
    });
    
    console.log('Navigating...');
    await page.goto('https://canonflowfoundation.github.io/FSharpAssay/');
    
    try {
        await page.waitForSelector('text=F# Code', { timeout: 30000 });
    } catch (e) {
        console.log('Timeout. Current content:');
        console.log(await page.content());
    }
    
    await browser.close();
})();
