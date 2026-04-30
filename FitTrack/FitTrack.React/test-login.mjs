import { chromium } from '@playwright/test';

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  const page = await context.newPage();

  const errors = [];
  page.on('console', msg => {
    if (msg.type() === 'error') {
      errors.push(msg.text());
    }
  });
  page.on('pageerror', err => {
    errors.push(err.message);
  });

  console.log('Navigating to http://localhost:3000/login...');
  await page.goto('http://localhost:3000/login', { waitUntil: 'networkidle' });

  console.log('Page title:', await page.title());
  console.log('Page URL:', page.url());

  // Take screenshot of the page
  await page.screenshot({ path: 'login-page.png' });
  console.log('Screenshot saved to login-page.png');

  // Check for visible content
  const bodyText = await page.textContent('body');
  console.log('Page content preview:', bodyText?.slice(0, 500));

  if (errors.length > 0) {
    console.log('\n--- Console Errors ---');
    errors.forEach(e => console.log('ERROR:', e));
  } else {
    console.log('\nNo console errors detected.');
  }

  // Try to fill in login form with test credentials
  console.log('\nAttempting to login with test credentials...');
  await page.fill('input[type="email"], input[id="email"], input[placeholder*="email" i]', 'test@example.com').catch(() => console.log('Email field not found'));
  await page.fill('input[type="password"]', 'Test123!').catch(() => console.log('Password field not found'));

  // Click login button
  await page.click('button[type="submit"]').catch(() => console.log('Submit button not found'));

  // Wait a bit for any response
  await page.waitForTimeout(3000);

  console.log('After login attempt URL:', page.url());
  await page.screenshot({ path: 'after-login.png' });
  console.log('Screenshot saved to after-login.png');

  if (errors.length > 0) {
    console.log('\n--- Errors after login attempt ---');
    errors.forEach(e => console.log('ERROR:', e));
  }

  await browser.close();
})();
