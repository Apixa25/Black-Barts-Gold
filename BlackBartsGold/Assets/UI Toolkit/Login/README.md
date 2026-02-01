# UI Toolkit Login Screen – Black Bart's Gold

This folder contains the **UI Toolkit** login screen (email + password) that talks to the **dashboard API** (Supabase Auth).

## Files

- **LoginScreen.uxml** – Structure (container, email/password fields, Sign In button, message label, Create Account button).
- **LoginScreen.uss** – Styles (brand colors: gold, saddle brown, parchment).
- **LoginControllerUIToolkit.cs** – In `Assets/Scripts/UI/`. Binds the form to `AuthService` and scene navigation.

A copy of the UXML and USS is in **Resources/UI Toolkit/Login/** so the controller can load them at runtime if not assigned in the Inspector.

## Setup in the Login scene

The **Login** scene already has a **LoginUIToolkit** GameObject with **LoginControllerUIToolkit**. At runtime it adds **UIDocument** if missing, loads the login UXML from `Resources/UI Toolkit/Login/LoginScreen`, and hides the old uGUI **LoginCanvas**.

1. Open the **Login** scene and enter Play mode to use the UI Toolkit login.
2. **(If the UI does not appear)** Create **Panel Settings**: right‑click in Project → **Create** → **UI Toolkit** → **Panel Settings**. Assign it to the **LoginUIToolkit** GameObject’s **UI Document** component → **Panel Settings** (the controller adds UIDocument at runtime; assign Panel Settings in the Inspector after entering Play once, or add a UIDocument manually and assign before Play).
3. **Optional:** Assign **LoginScreen** (this folder’s UXML) to **UI Document** → **Source Asset** on LoginUIToolkit if you add UIDocument manually. If left empty, the controller loads from Resources.

## Dashboard API

- **Editor:** Default is **Development** (`http://localhost:3000/api/v1`). Run the admin-dashboard locally (`npm run dev`) and log in from Unity.
- **Device builds:** Use **Production** (see `ApiConfig.cs`). No extra setup.

To use **Mock** in the Editor, set `ApiConfig.CurrentEnvironment = ApiEnvironment.Mock` (e.g. from a debug menu or code).
