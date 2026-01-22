# 🤠 Black Bart's Gold - Admin Dashboard

Web-based administration dashboard for managing the Black Bart's Gold treasure hunting game.

## Tech Stack

- **Framework**: Next.js 14+ (App Router)
- **Language**: TypeScript
- **Styling**: Tailwind CSS + shadcn/ui
- **Database**: Supabase (PostgreSQL)
- **Auth**: Supabase Auth (role-based)
- **Hosting**: Vercel

## Getting Started

See the full build guide: [`../Docs/ADMIN-DASHBOARD-BUILD-GUIDE.md`](../Docs/ADMIN-DASHBOARD-BUILD-GUIDE.md)

### Quick Start

```bash
# Install dependencies
npm install

# Set up environment variables
cp .env.example .env.local
# Edit .env.local with your Supabase credentials

# Run development server
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) to view the dashboard.

## Theme

This dashboard uses the **Western Gold & Brown** color palette:

- **Treasure Gold** (#FFD700) - Primary
- **Saddle Brown** (#8B4513) - Secondary  
- **Dark Leather** (#3D2914) - Text
- **Parchment** (#F5E6D3) - Backgrounds
- **Fire Orange** (#E25822) - Accents

> **Note**: Black Bart was a Wild West stagecoach robber, NOT a pirate. See `../Docs/brand-guide.md` for details.

## Features

- [ ] Authentication & User Management
- [ ] Coin Management (view, create, edit, delete)
- [ ] Financial Dashboard & Reports
- [ ] Sponsor Management
- [ ] Security Monitoring & Cheater Detection

## Project Structure

```
admin-dashboard/
├── src/
│   ├── app/           # Next.js App Router pages
│   ├── components/    # React components
│   ├── lib/           # Utilities and Supabase client
│   ├── hooks/         # Custom React hooks
│   └── types/         # TypeScript types
├── public/            # Static assets
└── package.json
```

## Related Documentation

- [Brand Guide](../Docs/brand-guide.md) - 🤠 **READ FIRST**
- [Admin Dashboard Requirements](../Docs/admin-dashboard.md)
- [Build Guide](../Docs/ADMIN-DASHBOARD-BUILD-GUIDE.md)

---

*"X marks the spot, partner!"* 🤠
