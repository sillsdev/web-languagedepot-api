import type { PlaywrightTestConfig } from '@playwright/test';

function getenv(key: string, defaultVal?: string) {
	const result = process.env[key];
	if (result) {
		return result;
	} else {
		return defaultVal ?? '';
	}
}

function getBaseUrl() {
	const hostname = getenv('API_HOST', 'localhost');
	const port = getenv('API_PORT', '4173');
	const host = port ? `${hostname}:${port}` : hostname;
	const path = getenv('API_BASE_PATH', '/api/v2/');
	return `http://${host}${path}`;
}

const config: PlaywrightTestConfig = {
	webServer: {
		reuseExistingServer: !process.env.CI,
		command: 'npm run build && npm run preview',
		port: +getenv('API_PORT', '4173')
	},
	use: {
		baseURL: getBaseUrl() // 'http://localhost:4173/api/v2/'
	}
};

export default config;
