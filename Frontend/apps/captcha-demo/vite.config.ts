import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), 'VITE_');
  return {
    plugins: [react()],
    base: env.VITE_BASE_PATH || '/netcha/',
    test: { environment: 'jsdom', globals: true, setupFiles: './src/test/setup.ts' },
  };
});
