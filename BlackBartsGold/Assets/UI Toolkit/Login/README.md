# UI Toolkit Login Screen – Black Bart's Gold

This folder contains the **UI Toolkit** login screen (email + password) that talks to the **dashboard API** (Supabase Auth).

## Files

- **LoginScreen.uxml** – Structure (container, email/password fields, Sign In button, message label, Create Account button).
- **LoginScreen.uss** – Styles (brand colors: gold, saddle brown, parchment).
- **LoginControllerUIToolkit.cs** – In `Assets/Scripts/UI/`. Binds the form to `AuthService` and scene navigation.

A copy of the UXML and USS is in **Resources/UI Toolkit/Login/** so the controller can load them at runtime if not assigned in the Inspector.

## Setup in the Login scene (one-time)

The **Login** scene uses the **old uGUI** login by default so the APK builds without manual setup. To use the **UI Toolkit** login:

1. Open the **Login** scene.
2. In the Hierarchy: **Right‑click** → **Create Empty**. Name it **LoginUIToolkit**.
3. With **LoginUIToolkit** selected: **Add Component** → search **Login Controller UIToolkit** → add it.
4. Enter Play. The script adds **UIDocument** at runtime, loads the form from `Resources/UI Toolkit/Login/LoginScreen`, and hides the old **LoginCanvas**. Sign In uses the dashboard API.

**(If the UI does not appear)** Create **Panel Settings** (Create → UI Toolkit → Panel Settings), add a **UI Document** component to LoginUIToolkit, assign Panel Settings and **LoginScreen.uxml** as Source Asset, then Play.

## Dashboard API

- **Editor:** Default is **Development** (`http://localhost:3000/api/v1`). Run the admin-dashboard locally (`npm run dev`) and log in from Unity.
- **Device builds:** Use **Production** (see `ApiConfig.cs`). No extra setup.

To use **Mock** in the Editor, set `ApiConfig.CurrentEnvironment = ApiEnvironment.Mock` (e.g. from a debug menu or code).
