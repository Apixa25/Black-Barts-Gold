# 🤠 Docs Policy — Black Bart's Gold

> **Purpose**: Define which docs carry the most weight, how docs should be organized, and how to handle drift between docs and the real codebase.
> **Why this exists**: The repo has grown to the point where not every `.md` file should be treated as equally authoritative. This policy keeps codebase reviews fast, accurate, and grounded in the right sources.

---

## 🎯 Core Principle

Not every document in `Docs/` serves the same purpose.

Some docs define:

- project intent
- collaboration style
- current implementation state
- active subsystem behavior

Other docs are still valuable, but they are better treated as:

- historical progress notes
- proposals
- feature exploration
- future design notebooks

This policy exists to make that distinction explicit.

---

## 🧭 Source Of Truth Hierarchy

When understanding the repo, use this order of authority:

1. **Operational policy**
   - `.cursor/rules/proactive-support-defaults.mdc`
   - This is the canonical operational policy for how work should be performed.

2. **Project intent and collaboration context**
   - `Docs/project-vision.md`
   - Use this for project goals, AI-first philosophy, collaboration preferences, and the additive/low-risk working style.

3. **Current-state tactical context**
   - `Docs/session-handoff.md`
   - Use this for what is currently built, what is deployed, what is in progress, and the latest session continuity.

4. **Codebase reality**
   - `BlackBartsGold/`
   - `admin-dashboard/`
   - `mcp-server/`
   - If docs and code disagree, the codebase is the final verifier of reality.

5. **Focused subsystem docs**
   - These explain active systems in more detail and should be used after the orientation docs above.

---

## 📚 Canonical Orientation Docs

These are the first docs an AI assistant or human contributor should trust when orienting to the repo:

- `Docs/project-vision.md`
- `Docs/session-handoff.md`
- `Docs/brand-guide.md`

### How to use them

- Read `Docs/brand-guide.md` first when character, tone, naming, or visual identity matters.
- Read `Docs/project-vision.md` for the big-picture product and collaboration model.
- Read `Docs/session-handoff.md` for tactical current-state truth before making implementation decisions.

### Important note

`Docs/project-vision.md` carries strong intent and collaboration weight, even if some implementation details become outdated over time.

---

## 🛠️ Active Reference Docs

These docs support implemented or actively developed systems. They are useful reference material, but they are not higher authority than the codebase.

Examples include:

- `Docs/AI-integration.md`
- `Docs/AI-INTEGRATION-SPEC.md`
- `Docs/BUILD-GUIDE.md`
- `Docs/DEVELOPMENT-LOG.md`
- `Docs/economy-and-currency.md`
- `Docs/coins-and-collection.md`
- `Docs/prize-finder-details.md`
- `Docs/treasure-hunt-types.md`
- `Docs/user-accounts-security.md`
- `Docs/dynamic-coin-distribution.md`
- `Docs/AR-COIN-DISPLAY-SPEC.md`
- `Docs/MAPBOX-SETUP.md`
- `Docs/safety-and-legal-research.md`

### Rule

If a doc is actively referenced by code comments, current implementation, or active build workflows, it should usually remain in top-level `Docs/`.

---

## 🗃️ Archive Docs

Some docs are still useful, but they should not compete with the orientation docs for authority.

These usually include:

- proposals
- historical progress trackers
- superseded implementation plans
- speculative feature design docs
- older product requirement snapshots

These should live in:

- `Docs/archive/`

### Archive rule

Archive a doc when it is still worth preserving for context, but should no longer be treated as primary current guidance.

### Do not delete by default

If a doc may still contain useful design history, move it to `Docs/archive/` instead of deleting it outright.

This matches the additive, low-risk collaboration approach described in `Docs/project-vision.md` and `Docs/session-handoff.md`.

---

## ⚖️ When Docs And Code Disagree

Use this rule:

> **Code wins. Docs should then be updated, clarified, or archived.**

### Practical meaning

- If a doc says a system is not built, but the repo clearly contains the implementation, the doc is stale.
- If a doc describes a proposal and the code later evolves differently, the proposal doc becomes historical context, not current truth.
- If code comments point to a doc that no longer exists or no longer matches the code, that doc reference should be fixed.

---

## 🧹 Top-Level Docs Standard

Top-level `Docs/` should stay focused on:

- orientation
- current tactical context
- active reference material
- current build guidance

Top-level `Docs/` should avoid becoming a mixed pile of:

- active truth
- old plans
- stale progress logs
- future-feature brainstorming

That mix creates unnecessary ambiguity during codebase reviews.

---

## ✍️ Updating Rules

When working in this repo:

- Update `Docs/session-handoff.md` when a productive session materially changes the current state or next steps.
- Update or archive docs that are clearly contradicted by the codebase.
- Prefer small, additive doc updates over large rewrites.
- Preserve useful historical thinking by archiving instead of deleting whenever possible.

### Broken references

If a file in `Docs/` is renamed, archived, or removed:

- update repo references to it
- update relevant doc indexes
- avoid leaving broken links in `README.md`, code comments, or other docs

---

## ✅ Simple Decision Test

When deciding where a doc belongs, ask:

1. Does this help a new contributor understand the repo right now?
2. Does this reflect active implementation or current workflow?
3. Is this mainly historical, speculative, or superseded?

If the answer is:

- mostly `1` or `2` → keep it in top-level `Docs/`
- mostly `3` → move it to `Docs/archive/`

---

## 🤠 Final Guidance

The goal of this policy is not to create paperwork.

The goal is to make it easier for humans and AI assistants to:

- orient quickly
- trust the right docs
- avoid stale guidance
- preserve useful history without letting it masquerade as current truth

Build the docs system the same way we build the product:

- practical
- additive
- clear
- grounded in reality
