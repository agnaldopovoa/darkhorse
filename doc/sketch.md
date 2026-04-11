# Automated Cryptocurrency Trading Application - Architecture & Ideas

This document organizes your requirements into a structured architectural plan, highlighting potential technology stacks and crucial security considerations.

## 1. High-Level Overview
A web-based platform allowing users to define trading strategies using a scripting language, connect to multiple cryptocurrency brokers (exchanges), test strategies, and deploy live automated trading bots.

## 2. Core Components
- **Web Interface (Frontend):** For broker configuration, strategy scripting, currency selection, and monitoring active orders.
- **Backend/API:** Handles user requests, interacts with the database, and schedules/manages trading bots.
- **Trading Engine (Worker):** Executes trading strategies, fetches market data, and interfaces with broker APIs.   
- **Database:** Stores user data, encrypted broker credentials, strategy scripts, and trade tracking.

## 3. Technology Stack Recommendations
- **Frontend**
  - *React* or *Vue.js* with styling frameworks like TailwindCSS or a custom modern UI to provide a reactive, dashboard-style interface.
- **Backend & Trading Engine**
  - **Python (FastAPI or Django):** Highly recommended for this use case. Python has the richest ecosystem for financial data analysis (Pandas, NumPy) and robust trading libraries.
  - **Node.js (TypeScript):** A great alternative if you want the frontend and backend in the same language.        
- **Database**
  - *PostgreSQL* for relational data (users, strategies, connection metadata).
  - *Redis* for caching market data and managing the task queue (Celery/BullMQ) for trading bots.
- **Broker Integration**
  - **CCXT library:** An open-source library available in Python and JavaScript that standardizes the APIs of over 100+ cryptocurrency exchanges, saving you from writing custom API clients for every broker.

## 4. Addressing Specific Requirements

### Strategy Scripting Language
To let you script strategies, you need to execute user-defined code securely.
- **If using Python:** You can write strategies in Python and execute them in a restricted environment.
- **If using Node.js:** You can write strategies in JavaScript and execute them using sandbox libraries like `isolated-vm` or `vm2`.
- **Custom DSL:** For maximum security, you could parse a simplified custom language specific to your app (e.g., `buy 10% when RSI < 30`).

### Broker Configuration & Data
The app will require an interface where you select the exchange, provide the **API Key** and **API Secret** (generated from the exchange), and save it. The backend will use these keys via CCXT to fetch currency pairs and submit orders.

### Free Cloud Deployment
- **Docker:** Containerize your frontend, backend, and workers. This makes the app cloud-agnostic.
- **Hosting Options:**
  - **Railway:** Excellent for frictionless deployment of Docker containers directly from GitHub. Offers a free tier/low cost.
  - **Oracle Cloud (Always Free):** Unmatched free tier. You get up to 4 ARM Ampere A1 Compute instances with 24GB RAM. This is powerful enough to run your entire stack (DB, API, frontend, and workers).
  - **Google Cloud Run:** Great for the web endpoints (scale to zero logic), though background workers running 24/7 on GCP might incur costs.

### Security (HTTPS, Passwords, and Secrets)
There is an important distinction to make regarding hashing and encryption:

*   **HTTPS:** Critical for protecting data in transit. Services like Railway and Google Cloud provide automatic SSL/HTTPS certificates. For Oracle Cloud, you can use Nginx with Let's Encrypt (Certbot).
*   **User Passwords (Argon2):** As you noted, Argon2 is the gold standard for hashing user login passwords. It is memory-hard and GPU-resistant.
*   > [!WARNING]
    > **Broker API Secrets Storage:** You **cannot** use Argon2 (or any hash) for the broker API secrets. Hashing is a one-way function, but your Trading Engine needs the original, plain-text API keys to sign requests to the broker.

    > **Solution:** Use symmetric encryption (like **AES-256-GCM**). You encrypt the API keys in your application before storing them in the database. When the bot needs to trade, it decrypts the keys in memory using a master encryption key (stored as an environment variable on your server).

## 5. User Journey
1. **Login:** Authenticate securely (Argon2 comparison).  
2. **Broker Setup:** Add Binance/Kraken/etc. Input keys, which are immediately encrypted via AES before being saved.
3. **Strategy Studio:** Write/paste a custom script defining parameters (e.g., standard Moving Average crossover) and save the strategy.
4. **Launchpad:** Select a broker connection, a strategy, a trading pair (BTC/USDT), and a capital allocation limit. Click "Start".
5. **Dashboard:** Monitor open positions, PnL, and live logs from your script container.
