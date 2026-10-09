import { cp, mkdir, rm } from 'node:fs/promises';
import { resolve } from 'node:path';

const root = resolve(import.meta.dirname, '..');
const output = resolve(root, 'dist-pages');

await rm(output, { recursive: true, force: true });
await mkdir(output, { recursive: true });
await cp(resolve(root, 'apps/captcha-demo/dist'), output, { recursive: true });
await cp(resolve(root, 'apps/admin-panel/dist'), resolve(output, 'admin'), { recursive: true });

console.log(`GitHub Pages paketi hazır: ${output}`);
