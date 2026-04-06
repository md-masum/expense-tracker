# 🌐 Offline Finance Tracker (PWA)

## 📌 Overview

This is the **Progressive Web App (PWA) version** of the Offline Finance Tracker — the same app as the .NET MAUI Blazor Hybrid version, rebuilt entirely in **HTML, CSS, and vanilla JavaScript**.

It is designed to run in **any modern browser**, works fully **offline**, and can be **installed on any device** (iPhone, Android, PC) directly from the browser — with no app store required.

The app is deployable for free via **GitHub Pages**.

---

## 🎯 Key Features

* ✅ Track **income and expenses** per project
* ✅ Categorize expenses (e.g., sand, labour, brick, materials)
* ✅ Real-time **balance calculation**
* ✅ Generate **financial summaries** on the dashboard
* ✅ Fully **offline** — works without internet after first load
* ✅ **Installable** on iPhone (Safari) and Android (Chrome)
* ✅ **Export JSON backup** — save all data to a file
* ✅ **Export CSV report** — open in Excel, Google Sheets, or Numbers
* ✅ **Restore from backup** — re-import a JSON backup file

---

## 🛠️ Tech Stack

* **Frontend**: HTML5, CSS3, Vanilla JavaScript (no frameworks)
* **UI Framework**: Bootstrap 5.3
* **Icons**: Bootstrap Icons 1.11
* **Storage**: IndexedDB (browser-native local database)
* **Offline**: Service Worker (cache-first strategy)
* **Routing**: Hash-based SPA routing (`#/dashboard`, `#/projects`, etc.)
* **Hosting**: GitHub Pages (free)

---

## 🏗️ Architecture

The project follows a simple **layered single-page app** structure:

```
/pwa
  index.html          → App shell (navbar, modal, toast, SW registration)
  manifest.json       → PWA installable app manifest
  sw.js               → Service Worker (offline caching)
  icon.svg            → App icon (standard)
  icon-maskable.svg   → App icon (maskable, for Android adaptive icons)

  /css
    app.css           → Custom responsive styles (mobile-first)

  /js
    db.js             → IndexedDB data layer (all CRUD + seeding)
    app.js            → Full SPA logic (router, all pages, forms, backup)
```

### 🔹 Principles

* Offline-first design
* No build tools or bundlers required
* Zero backend dependencies
* Data stays entirely on the user's device

---

## 📊 Core Data Model

Data is stored in **IndexedDB** across three object stores:

### Project

* Represents a construction or agricultural project
* Fields: `id`, `name`, `type`, `createdAt`, `isActive`

### Transaction

* Tracks a financial activity within a project
* Fields: `id`, `projectId`, `categoryId`, `type` (Income / Expense), `amount`, `date`, `note`, `createdAt`
* Indexed on: `projectId`, `date`

### Category

* Groups transactions by label
* Fields: `id`, `name`, `type` (Income / Expense)
* Seeded with 6 defaults on first run: `Sand`, `Labour`, `Brick`, `Materials`, `Investment`, `Sale`

---

## 💡 Example Use Case

```
Project: House Construction

Income:
- Investment: 150,000 ৳

Expenses:
- Sand:   20,000 ৳
- Labour:  3,000 ৳

Balance: 127,000 ৳
```

The app automatically calculates totals and keeps your data organized per project — fully offline, directly in your browser.

---

## 🚀 Getting Started

### Prerequisites

* Any modern browser (Chrome, Firefox, Safari, Edge)
* No installs, no build step, no server required for the app itself

---

### Run Locally

A plain HTTP server is required (Service Workers don't work over `file://`).

**Option 1 — Python (recommended, no install needed):**

```bash
cd pwa
python3 -m http.server 8080
```

Then open **http://localhost:8080**

**Option 2 — Node.js:**

```bash
npx serve pwa
```

**Option 3 — VS Code:**  
Install the **Live Server** extension → right-click `index.html` → Open with Live Server

---

### Test on Your Phone (same Wi-Fi)

```bash
ipconfig getifaddr en0   # find your Mac's local IP
```

Open `http://192.168.x.x:8080` in your phone's browser.

* **iOS Safari**: Share → Add to Home Screen
* **Android Chrome**: Install banner appears automatically

---

## 🚀 Deploy to GitHub Pages (Free)

1. Create a new GitHub repository (e.g. `finance-tracker-pwa`)
2. Copy the contents of the `pwa/` folder to the repo root:

```bash
cp -r pwa/. ~/finance-tracker-pwa/
```

3. Push to the `main` branch
4. Go to **Settings → Pages → Source: Deploy from branch → `main` / `/ (root)`**
5. Your app is live at:

```
https://your-username.github.io/finance-tracker-pwa/
```

---

## 💾 Backup & Restore

| Feature | Description |
|---|---|
| Export JSON | Downloads all data as a `.json` file |
| Restore JSON | Re-imports a `.json` backup (replaces all data) |
| Export CSV | Downloads transactions for one project as a `.csv` file |

> ⚠️ Data lives in your browser's IndexedDB. Clearing site data or uninstalling the browser will remove it. Export backups regularly.

---

## 🔒 Design Goals

* 📴 Fully offline functionality
* ⚡ Fast and lightweight — no build step, no framework overhead
* 🧠 Easy to use in real-world field conditions
* 🔧 Minimal setup and maintenance

---

## 📈 Future Improvements

* Charts and visual expense breakdown
* Date range filtering for transactions
* Print-friendly report page
* Multi-currency support
* Cloud sync (optional, user-driven)

---

## 🤖 AI Assistance

This project was built with **GitHub Copilot** assistance following the guidelines in:

```
.github/copilot-instructions.md
```

---

## 📄 License

This project is currently for personal and experimental use.  
License will be defined later.

---

## 🙌 Contribution

Currently a solo project. Contributions may be opened in future.

---

## 📬 Contact

For questions or ideas, feel free to reach out.

---

**Built with simplicity and real-world usage in mind.**
