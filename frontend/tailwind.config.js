/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        background: '#0a0a0b',
        surface: '#121214',
        surfaceHighlight: '#1e1e24',
        primary: '#3b82f6', // blue-500
        primaryHover: '#2563eb', // blue-600
        success: '#10b981', // emerald-500
        danger: '#ef4444', // red-500
        warning: '#f59e0b', // amber-500
        text: '#f3f4f6', // gray-100
        muted: '#9ca3af', // gray-400
        border: '#27272a', // zinc-800
      },
      fontFamily: {
        sans: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        mono: ['Fira Code', 'ui-monospace', 'monospace'],
      }
    },
  },
  plugins: [],
}
