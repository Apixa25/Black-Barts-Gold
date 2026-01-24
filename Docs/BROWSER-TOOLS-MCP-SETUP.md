# 🌐 BrowserTools MCP Setup Guide

> **Goal**: Enable Cursor AI to see browser console logs, errors, and network requests automatically - no more copy/pasting! 🎯

This guide will set up **BrowserTools MCP** by AgentDesk, which gives Cursor direct access to:
- ✅ Console logs and errors
- ✅ XHR network requests/responses  
- ✅ Screenshot capture
- ✅ DOM element inspection
- ✅ Debugger mode for automated bug fixing

---

## 📋 Requirements

- ✅ Node.js installed (you have this)
- ✅ Google Chrome or Chromium-based browser
- ✅ Cursor IDE
- ✅ **Important**: Use **Claude 3.5 Sonnet** model in Composer (MCP is specific to Anthropic models)

---

## 🚀 Installation Steps

### ⚠️ Not the Anthropic Claude Extension!

The **Anthropic Claude** Chrome extension (chat/Claude in browser) is **different**.  
You need **BrowserTools MCP** by AgentDesk – it captures console logs and sends them to Cursor.

### Step 1: BrowserTools Extension (Already in This Repo)

The extension is in your project:

- **Folder**: `Black-Barts-Gold\BrowserTools-MCP-Extension\chrome-extension`
- **GitHub**: https://github.com/AgentDeskAI/browser-tools-mcp  
- **Direct ZIP**: https://github.com/AgentDeskAI/browser-tools-mcp/releases/download/v1.2.0/BrowserTools-1.2.0-extension.zip

### Step 2: Install Chrome Extension

1. Open Chrome and go to: `chrome://extensions/`
2. **Enable "Developer Mode"** (toggle in top-right)
3. Click **"Load unpacked"**
4. Navigate to and select:
   ```
   c:\Users\Admin\Black-Barts-Gold\BrowserTools-MCP-Extension\chrome-extension
   ```
5. Click **"Select"**
6. You should see **"BrowserToolsMCP"** in your extensions list! ✅

### Step 3: Configure MCP Server in Cursor

1. Open **Cursor Settings** (Ctrl+, or Cmd+,)
2. Go to **Features** → Scroll to **MCP Servers**
3. Click **"+ Add New MCP Server"**
4. Fill in:
   - **Name**: `browser-tools` (or any name you like)
   - **Type**: `command` (or `stdio`)
   - **Command**: 
     ```bash
     npx @agentdeskai/browser-tools-mcp@latest
     ```
   
   **For Windows (if path issues):**
   - First, find your npx path: `where npx` (in PowerShell)
   - Use full path: `C:\path\to\npx.cmd @agentdeskai/browser-tools-mcp@latest`

5. Click **Save**
6. Wait a few seconds or click **Refresh** - you should see:
   - ✅ Green circle next to the server name
   - ✅ List of tools: `get_console_logs`, `get_console_errors`, `get_xhr_network_logs`, etc.

### Step 4: Run the BrowserTools Server

**Two pieces:**

- **MCP server** = configured in Cursor (Step 3). Cursor runs it.
- **Node server** = you run this in a terminal and **keep it running**:

```powershell
npx @agentdeskai/browser-tools-server@latest
```

This server gathers logs from the Chrome extension and talks to the MCP server.  
**Note**: Uses port 3025. If you get a conflict, close whatever is using that port.

### Step 5: Open Chrome Dev Tools

1. Open your admin dashboard in Chrome: `http://localhost:3000`
2. **Right-click** anywhere on the page → **"Inspect"** (or press F12)
3. This opens Chrome Dev Tools - **keep it open!**
4. Navigate to the **"BrowserTools"** tab in Dev Tools (you should see it)

**Important**: Logs are only captured when Dev Tools is open!

---

## 🎯 How to Use

Once set up, you can ask me things like:

- **"Check console and network logs to see what went wrong"**
  - I'll automatically use all four log tools to debug

- **"Take a screenshot of the dashboard"**
  - Screenshots save to `Downloads/mcp-screenshots/` by default

- **"Enter debugger mode"**
  - I'll use multiple tools + prompts to fix bugs automatically

- **"Can you edit the currently selected element?"**
  - Select an element in Chrome Dev Tools, then ask me to edit it

---

## 🔧 Troubleshooting

### Issue: MCP server not showing tools
- ✅ Make sure you're using **Claude 3.5 Sonnet** in Composer (not Auto)
- ✅ Check the command is correct: `npx @agentdeskai/browser-tools-mcp@latest`
- ✅ Click the refresh button in Cursor MCP settings
- ✅ Restart Cursor

### Issue: Not seeing any logs
- ✅ Make sure Chrome Dev Tools is **open** (F12)
- ✅ Make sure you're on the tab you want to monitor
- ✅ Check the BrowserTools panel in Dev Tools

### Issue: Screenshot tool failing
- ✅ Make sure you're using `@latest` in the command
- ✅ Check that port 3025 is available

### Issue: Logs keep disappearing
- ✅ Logs are wiped when you refresh the page
- ✅ Logs are wiped when you restart the browser-tools server
- ✅ Use "Wipe Logs" button in BrowserTools panel to manually clear

### Issue: "Failed to send log to browser-connector"
- ✅ Close Chrome Dev Tools in other tabs
- ✅ Refresh the tab you're working on

---

## 📝 Next Steps

After setup:
1. ✅ Keep the browser-tools server running (`npx @agentdeskai/browser-tools-mcp@latest`)
2. ✅ Open Chrome Dev Tools when testing
3. ✅ Use **Claude 3.5 Sonnet** in Composer (not Auto)
4. ✅ Ask me to check logs/errors - I'll see them automatically! 🎉

---

## 🔗 Resources

- **Official Docs**: https://browsertools.agentdesk.ai/
- **GitHub Repo**: https://github.com/AgentDeskAI/browser-tools-mcp
- **Quickstart Guide**: https://browsertools.agentdesk.ai/quickstart
- **Support**: Contact [@tedx_ai](https://x.com/tedx_ai) on X

---

**Once this is set up, I'll be able to see your console errors in real-time - no more copy/pasting! 🚀**
