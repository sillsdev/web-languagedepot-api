import { apiv2 as api } from './testsetup.js';

export async function mochaGlobalSetup() {
    try {
        const result = await api('roles', { retry: 0 });
        return result;
    } catch (error) {
        console.log('API seems to not be running; tests are probably going to fail. Try "npm run dev" in another console tab');
        console.log('error was', error);
        // throw 'Exit now please';
        process.exit(1);
    }
}
