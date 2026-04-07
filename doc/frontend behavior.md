## Complete consolidated behavior description

---

### Authentication

The application opens on the **Login page**. The user enters email and password to log in and is redirected to the Dashboard on success.

A **Register** link allows account creation by providing email and password. After submitting, a notification informs the user that an activation link has been sent. Clicking the link takes the user to an "Account successfully activated" page; from there, the user must navigate back to the login page manually.

A **Forgot my password** link prompts the user to confirm their email address. A reset link is sent to that address, which leads to a form with "New password" and "Confirm new password" fields.

A **logout option** is available globally via the left sidebar and returns the user to the login page.

---

### Navigation

A **fixed left sidebar** contains links to Dashboard, Brokers, and Strategies, with the logout option anchored at the bottom.

---

### Dashboard

Displays three cards:

- **Portfolio** — current total asset value across all brokers in USDT.
- **P&L** — single numeric value in USDT filtered by the selected time range. Positive values shown in green with a `+` prefix; negative in red with a `−` prefix.
- **Broker status indicator** — red (all failed), yellow (at least one failed), green (all succeeded). Clicking yellow opens a modal listing the brokers with communication failures.

**Time range controls** — 1d, 7d, 1m, 6m, 1y, and custom. The custom option opens a modal with a "Time unit" combo (day, week, month, year) and a free numeric "Amount" field. These filters apply only to P&L.

Every **60 seconds**, the dashboard polls the backend independently to refresh Portfolio and P&L values. No WebSocket is used for dashboard updates.

---

### Brokers

**List columns:** Name · Maker Fee · Market (spot/futures) · Network (mainnet/testnet) · Status (active/inactive) · Edit icon · Delete icon.

**Create / Edit form fields:** Broker name (fixed list: Binance, Bitfinex, Bybit, Coinbase Advanced, Kraken, KuCoin, OKX) · API Key · API Secret · Maker Fee · Market · Network.

On save, the system **tests the connection**. If the test fails, a modal informs the user and asks for confirmation to proceed. Confirming saves the broker as **inactive**. A successful test saves it as **active**.

A **confirmation modal** is shown before deleting a broker.

---

### Strategies

**List columns:** Name · Broker (with market label) · Asset · IsActive · Status · Last execution · P&L · Largest gain · Largest loss · Creation · Activate/Deactivate button · Run/Stop button · Edit icon · Delete icon.

P&L, largest gain, and largest loss display values in **USDT alongside the percentage**, reflecting only the most recent execution. These columns are **empty** while execution is "Not executed".

---

#### List controls

**Pagination** — the user can choose the number of rows per page.

**Default sort** — newest to oldest by creation date. The user can click any column header to sort by that column and toggle between ascending and descending order.

**Filters:**
- Exchange (Broker)
- IsActive ( Yes / No)
- Status (Not executed / Waiting / Running / Completed / Cancelled / Error)

---

#### IsActive (manual, user-controlled)

| Value | Meaning |
|---|---|
| Yes | Strategy is enabled |
| No | Strategy is disabled |

- A strategy is created as **Yes** by default.
- The Activate/Deactivate button is **disabled** while execution is Waiting or Running.
- When Status is **No**, the Run button is **disabled**.

---

#### Status (system-controlled)

| Value | Meaning | Label style |
|---|---|---|
| Not executed | Set on creation; never run | Default |
| Waiting | Queued; awaiting the background job | Default |
| Running | Currently executing | Default |
| Completed | Backtest finished naturally | Default |
| Cancelled | Interrupted by the user | Default |
| Error | An error occurred | Red |

The **Error** label shows a tooltip with the error message on hover and on click.

---

#### Execution lifecycle

When the user confirms the Run modal, the execution status is set to **Waiting** and the Run button is replaced by the **Stop button**.

A **background job** continuously scans for strategies in Waiting status. When one is found, it starts execution and notifies the frontend via **WebSocket**. The frontend receives this event and updates the execution status to **Running** in place.

If no time range is specified ("Past execution" unchecked), the strategy runs **continuously** until the user stops it. If a time range is specified, it runs as a **backtest** and completes automatically. On backtest completion, the WebSocket delivers a completion event; the frontend updates the execution status to **Completed** and refreshes P&L, largest gain, and largest loss in place.

If the background job fails to start a strategy, the execution status changes to **Error** immediately with no retry.

When a strategy is re-executed after a terminal status (Completed, Cancelled, or Error), the previous execution data is **overwritten** by the new execution.

---

#### Stop button

Visible only while execution is **Running**. Clicking it shows a confirmation modal. On confirm, execution status changes to **Cancelled**.

---

#### Run modal

Opens when the user clicks Run. Contains:
- "Past execution" checkbox.
- "Time unit" combo (day, week, month, year) — enabled only when checkbox is checked.
- "Numeric amount" free numeric field — enabled only when checkbox is checked.

---

#### Row protections

| Action | Condition | Behavior |
|---|---|---|
| Run button | Status is Inactive | Disabled |
| Run button | Strategy has no script | Disabled with warning |
| Activate/Deactivate button | Execution is Waiting or Running | Disabled |
| Edit icon | Execution is Waiting or Running | Blocked with warning |
| Delete icon | Execution is Waiting or Running | Blocked with warning |

---

#### Create / Edit form fields

- **Name** — free text.
- **Broker** — combo displaying broker name with market label. Asset field is disabled until a broker is selected.
- **Asset** — combo populated from the selected broker's assets, auto-filtered from the third character typed. Cleared and reloaded when the broker changes.
- **Monaco Editor** — available on both creation and editing. Configured for Python. Pre-filled on line 1 with:
  ```python
  # Available variables: ohlcv, balance, params, statistics, signal, quantity, reason
  ```