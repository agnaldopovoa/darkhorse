import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import fs from 'fs'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  
  const certPath = env.SSL_CERT_PATH;
  const keyPath = env.SSL_KEY_PATH;
  
  const httpsConfig = (certPath && keyPath && fs.existsSync(certPath) && fs.existsSync(keyPath))
    ? {
        key: fs.readFileSync(keyPath),
        cert: fs.readFileSync(certPath),
      }
    : undefined;

  return {
    plugins: [react()],
    server: {
      https: httpsConfig
    }
  }
})
