import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import tsconfigPaths from 'vite-tsconfig-paths';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');

  let config = {
    plugins: [react(), tsconfigPaths()],
    server: {
      port: 3000,
      proxy: {
        '/api': {
          target: env.PROXY || 'http://localhost:5000',
          changeOrigin: true,
        },
      },
    },
  };

  // As this was ported from CRA using webpack to vite
  // We need to define global.
  // For dev environment we do it here.
  // For prod environment we do it in vite-env.d.ts
  if (mode === 'development') {
      config = {...config, define: { global: "window"} }
  }

  return config;
});
