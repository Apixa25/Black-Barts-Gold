# 🤠 Black Bart's Gold - Admin Dashboard Build Guide

This document provides a **step-by-step guide** for building the Admin Dashboard web application. Follow these sections sequentially to build out each feature.

> **🎨 IMPORTANT**: Read `brand-guide.md` before starting! Use the Western Gold & Brown color palette, NOT pirate themes.

---

## 📋 Table of Contents

1. [Tech Stack Overview](#tech-stack-overview)
2. [Phase 0: Environment Setup](#phase-0-environment-setup)
3. [Phase 1: Project Foundation](#phase-1-project-foundation)
4. [Phase 2: Authentication & User Management](#phase-2-authentication--user-management)
5. [Phase 3: Coin Management](#phase-3-coin-management)
6. [Phase 4: Financial Dashboard](#phase-4-financial-dashboard)
7. [Phase 5: Sponsor Management](#phase-5-sponsor-management)
8. [Phase 6: Security & Monitoring](#phase-6-security--monitoring)
9. [Phase 7: Deployment](#phase-7-deployment)
10. [Reference Documents](#reference-documents)

---

## 🛠️ Tech Stack Overview

```
┌─────────────────────────────────────────────────────────────┐
│                  ADMIN DASHBOARD STACK                       │
├─────────────────────────────────────────────────────────────┤
│  Framework:   Next.js 14+ (App Router)                      │
│  Language:    TypeScript                                     │
│  Styling:     Tailwind CSS + shadcn/ui                      │
│  Database:    Supabase (PostgreSQL)                         │
│  Auth:        Supabase Auth (role-based)                    │
│  Hosting:     Vercel (can migrate to AWS later)             │
├─────────────────────────────────────────────────────────────┤
│  Expected Users: ~300 (admins, team, sponsors)              │
│  Theme: Western Gold (#FFD700) + Saddle Brown (#8B4513)     │
└─────────────────────────────────────────────────────────────┘
```

### Why This Stack?

| Choice | Reason |
|--------|--------|
| **Next.js 14** | Full-stack React, App Router, API routes, market standard |
| **TypeScript** | Type safety, better DX, fewer bugs |
| **Tailwind + shadcn/ui** | Rapid development, fully customizable components |
| **Supabase** | PostgreSQL + Auth + Realtime in one, generous free tier |
| **Vercel** | Zero-config Next.js deployment, preview deployments |

---

## 🚀 Phase 0: Environment Setup

### Prerequisites

Before starting, ensure you have:

- [ ] Node.js 18+ installed (`node --version`)
- [ ] npm or pnpm installed
- [ ] Git configured
- [ ] VS Code with extensions: ESLint, Prettier, Tailwind CSS IntelliSense
- [ ] A Supabase account (free at supabase.com)
- [ ] A Vercel account (free at vercel.com)

### Step 0.1: Create Project Directory

```bash
# From the Black-Barts-Gold root directory
cd c:\Users\Admin\Black-Barts-Gold

# Create the admin dashboard folder
mkdir admin-dashboard
cd admin-dashboard
```

### Step 0.2: Initialize Next.js Project

```bash
# Create Next.js app with TypeScript, Tailwind, ESLint, App Router
npx create-next-app@latest . --typescript --tailwind --eslint --app --src-dir --import-alias "@/*"
```

When prompted:
- Would you like to use TypeScript? **Yes**
- Would you like to use ESLint? **Yes**
- Would you like to use Tailwind CSS? **Yes**
- Would you like to use `src/` directory? **Yes**
- Would you like to use App Router? **Yes**
- Would you like to customize the default import alias? **Yes** (@/*)

### Step 0.3: Install Core Dependencies

```bash
# UI Components (shadcn/ui)
npx shadcn-ui@latest init

# When prompted:
# - Style: Default
# - Base color: Slate (we'll customize later)
# - CSS variables: Yes

# Install essential shadcn components
npx shadcn-ui@latest add button card input label form table tabs dialog dropdown-menu avatar badge toast sheet sidebar

# Supabase
npm install @supabase/supabase-js @supabase/ssr

# Additional utilities
npm install date-fns zod react-hook-form @hookform/resolvers lucide-react recharts
```

### Step 0.4: Create Supabase Project

1. Go to [supabase.com](https://supabase.com) and sign in
2. Click "New Project"
3. Name: `black-barts-gold-admin`
4. Database Password: Generate a strong password (save it!)
5. Region: Choose closest to you
6. Wait for project to be created (~2 minutes)

### Step 0.5: Get Supabase Credentials

1. In Supabase dashboard, go to Settings → API
2. Copy these values:
   - Project URL
   - anon/public key
   - service_role key (keep secret!)

### Step 0.6: Create Environment File

Create `admin-dashboard/.env.local`:

```env
# Supabase
NEXT_PUBLIC_SUPABASE_URL=your_project_url_here
NEXT_PUBLIC_SUPABASE_ANON_KEY=your_anon_key_here
SUPABASE_SERVICE_ROLE_KEY=your_service_role_key_here

# App
NEXT_PUBLIC_APP_NAME="Black Bart's Gold Admin"
NEXT_PUBLIC_APP_URL=http://localhost:3000
```

**⚠️ IMPORTANT**: Add `.env.local` to `.gitignore`!

### Step 0.7: Verify Setup

```bash
npm run dev
```

Visit `http://localhost:3000` - you should see the Next.js welcome page.

---

## 🏗️ Phase 1: Project Foundation

### Step 1.1: Project Structure

Create this folder structure:

```
admin-dashboard/
├── src/
│   ├── app/                    # Next.js App Router pages
│   │   ├── (auth)/            # Auth routes (login, register)
│   │   │   ├── login/
│   │   │   └── register/
│   │   ├── (dashboard)/       # Protected dashboard routes
│   │   │   ├── layout.tsx     # Dashboard layout with sidebar
│   │   │   ├── page.tsx       # Dashboard home
│   │   │   ├── users/         # User management
│   │   │   ├── coins/         # Coin management
│   │   │   ├── finances/      # Financial dashboard
│   │   │   ├── sponsors/      # Sponsor management
│   │   │   └── security/      # Security monitoring
│   │   ├── api/               # API routes
│   │   ├── layout.tsx         # Root layout
│   │   └── page.tsx           # Landing/redirect
│   ├── components/            # React components
│   │   ├── ui/               # shadcn/ui components
│   │   ├── layout/           # Layout components (sidebar, header)
│   │   ├── forms/            # Form components
│   │   └── dashboard/        # Dashboard-specific components
│   ├── lib/                   # Utility functions
│   │   ├── supabase/         # Supabase client setup
│   │   ├── utils.ts          # General utilities
│   │   └── validations.ts    # Zod schemas
│   ├── hooks/                 # Custom React hooks
│   ├── types/                 # TypeScript types
│   └── styles/               # Additional styles
├── public/                    # Static assets
│   └── images/
├── .env.local                 # Environment variables
└── package.json
```

### Step 1.2: Configure Western Theme Colors

Update `tailwind.config.ts`:

```typescript
import type { Config } from "tailwindcss"

const config: Config = {
  darkMode: ["class"],
  content: [
    "./src/pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/components/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/app/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  theme: {
    extend: {
      colors: {
        // Western Gold & Brown Theme
        gold: {
          DEFAULT: "#FFD700",
          50: "#FFFEF0",
          100: "#FFF9C4",
          200: "#FFF176",
          300: "#FFEB3B",
          400: "#FFD700",
          500: "#FFC107",
          600: "#FFB300",
          700: "#FFA000",
          800: "#FF8F00",
          900: "#FF6F00",
        },
        saddle: {
          DEFAULT: "#8B4513",
          50: "#FDF8F4",
          100: "#F5E6D8",
          200: "#E6C9AC",
          300: "#D4A574",
          400: "#B8763D",
          500: "#8B4513",
          600: "#723A10",
          700: "#5A2E0D",
          800: "#42220A",
          900: "#2A1606",
        },
        leather: {
          DEFAULT: "#3D2914",
          light: "#5C4023",
          dark: "#2A1C0E",
        },
        parchment: {
          DEFAULT: "#F5E6D3",
          light: "#FFF8E7",
          dark: "#E8D4BC",
        },
        fire: {
          DEFAULT: "#E25822",
          light: "#FF7043",
          dark: "#BF360C",
        },
        brass: {
          DEFAULT: "#B87333",
          light: "#D4954A",
          dark: "#8B5A2B",
        },
        // Keep default shadcn colors for compatibility
        border: "hsl(var(--border))",
        input: "hsl(var(--input))",
        ring: "hsl(var(--ring))",
        background: "hsl(var(--background))",
        foreground: "hsl(var(--foreground))",
        primary: {
          DEFAULT: "#FFD700", // Gold
          foreground: "#3D2914", // Dark leather
        },
        secondary: {
          DEFAULT: "#8B4513", // Saddle brown
          foreground: "#FFF8E7", // Light parchment
        },
        destructive: {
          DEFAULT: "#8B0000", // Warning red
          foreground: "#FFFFFF",
        },
        muted: {
          DEFAULT: "#F5E6D3", // Parchment
          foreground: "#5C4023", // Light leather
        },
        accent: {
          DEFAULT: "#E25822", // Fire orange
          foreground: "#FFFFFF",
        },
        card: {
          DEFAULT: "#FFF8E7", // Light parchment
          foreground: "#3D2914", // Dark leather
        },
      },
      fontFamily: {
        western: ["var(--font-western)", "serif"],
      },
      backgroundImage: {
        "wood-texture": "url('/images/wood-texture.png')",
        "parchment-texture": "url('/images/parchment-texture.png')",
      },
    },
  },
  plugins: [require("tailwindcss-animate")],
}
export default config
```

### Step 1.3: Setup Supabase Client

Create `src/lib/supabase/client.ts`:

```typescript
import { createBrowserClient } from '@supabase/ssr'

export function createClient() {
  return createBrowserClient(
    process.env.NEXT_PUBLIC_SUPABASE_URL!,
    process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY!
  )
}
```

Create `src/lib/supabase/server.ts`:

```typescript
import { createServerClient, type CookieOptions } from '@supabase/ssr'
import { cookies } from 'next/headers'

export async function createClient() {
  const cookieStore = await cookies()

  return createServerClient(
    process.env.NEXT_PUBLIC_SUPABASE_URL!,
    process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY!,
    {
      cookies: {
        get(name: string) {
          return cookieStore.get(name)?.value
        },
        set(name: string, value: string, options: CookieOptions) {
          try {
            cookieStore.set({ name, value, ...options })
          } catch (error) {
            // Handle cookies in Server Components
          }
        },
        remove(name: string, options: CookieOptions) {
          try {
            cookieStore.set({ name, value: '', ...options })
          } catch (error) {
            // Handle cookies in Server Components
          }
        },
      },
    }
  )
}
```

Create `src/lib/supabase/middleware.ts`:

```typescript
import { createServerClient, type CookieOptions } from '@supabase/ssr'
import { NextResponse, type NextRequest } from 'next/server'

export async function updateSession(request: NextRequest) {
  let response = NextResponse.next({
    request: {
      headers: request.headers,
    },
  })

  const supabase = createServerClient(
    process.env.NEXT_PUBLIC_SUPABASE_URL!,
    process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY!,
    {
      cookies: {
        get(name: string) {
          return request.cookies.get(name)?.value
        },
        set(name: string, value: string, options: CookieOptions) {
          request.cookies.set({ name, value, ...options })
          response = NextResponse.next({
            request: { headers: request.headers },
          })
          response.cookies.set({ name, value, ...options })
        },
        remove(name: string, options: CookieOptions) {
          request.cookies.set({ name, value: '', ...options })
          response = NextResponse.next({
            request: { headers: request.headers },
          })
          response.cookies.set({ name, value: '', ...options })
        },
      },
    }
  )

  await supabase.auth.getUser()

  return response
}
```

Create `src/middleware.ts`:

```typescript
import { type NextRequest } from 'next/server'
import { updateSession } from '@/lib/supabase/middleware'

export async function middleware(request: NextRequest) {
  return await updateSession(request)
}

export const config = {
  matcher: [
    '/((?!_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)',
  ],
}
```

### Step 1.4: Create TypeScript Types

Create `src/types/database.ts`:

```typescript
// User roles for the admin dashboard
export type UserRole = 'super_admin' | 'sponsor_admin' | 'user'

// User profile from Supabase
export interface UserProfile {
  id: string
  email: string
  full_name: string | null
  role: UserRole
  avatar_url: string | null
  created_at: string
  updated_at: string
}

// Coin types
export type CoinType = 'fixed' | 'pool'
export type CoinStatus = 'hidden' | 'visible' | 'collected' | 'expired'
export type CoinTier = 'gold' | 'silver' | 'bronze'

export interface Coin {
  id: string
  coin_type: CoinType
  value: number
  latitude: number
  longitude: number
  hider_id: string
  hidden_at: string
  collected_by: string | null
  collected_at: string | null
  status: CoinStatus
  tier: CoinTier
  is_mythical: boolean
  sponsor_id: string | null
  logo_url: string | null
  created_at: string
  updated_at: string
}

// Transaction types
export type TransactionType = 'found' | 'hidden' | 'gas_consumed' | 'purchased' | 'transfer'
export type TransactionStatus = 'pending' | 'confirmed' | 'failed'

export interface Transaction {
  id: string
  user_id: string
  transaction_type: TransactionType
  amount: number
  coin_id: string | null
  status: TransactionStatus
  created_at: string
  confirmed_at: string | null
}

// Sponsor
export interface Sponsor {
  id: string
  company_name: string
  contact_email: string
  logo_url: string | null
  coins_purchased: number
  coins_found: number
  is_active: boolean
  created_at: string
}

// Dashboard stats
export interface DashboardStats {
  total_users: number
  active_users_today: number
  total_coins_in_system: number
  coins_found_today: number
  total_deposits: number
  total_payouts: number
  gas_revenue_today: number
}
```

### Step 1.5: Root Layout with Theme

Update `src/app/layout.tsx`:

```typescript
import type { Metadata } from "next"
import { Inter } from "next/font/google"
import "./globals.css"
import { Toaster } from "@/components/ui/toaster"

const inter = Inter({ subsets: ["latin"] })

export const metadata: Metadata = {
  title: "Black Bart's Gold - Admin Dashboard",
  description: "Admin dashboard for managing the Black Bart's Gold treasure hunting game",
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en">
      <body className={`${inter.className} bg-parchment-light text-leather`}>
        {children}
        <Toaster />
      </body>
    </html>
  )
}
```

---

## 🔐 Phase 2: Authentication & User Management

### Step 2.1: Create Database Schema

In Supabase SQL Editor, run:

```sql
-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- User profiles table (extends Supabase auth.users)
CREATE TABLE public.profiles (
  id UUID REFERENCES auth.users(id) ON DELETE CASCADE PRIMARY KEY,
  email TEXT UNIQUE NOT NULL,
  full_name TEXT,
  role TEXT DEFAULT 'user' CHECK (role IN ('super_admin', 'sponsor_admin', 'user')),
  avatar_url TEXT,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Enable Row Level Security
ALTER TABLE public.profiles ENABLE ROW LEVEL SECURITY;

-- Policies for profiles
CREATE POLICY "Users can view own profile" ON public.profiles
  FOR SELECT USING (auth.uid() = id);

CREATE POLICY "Users can update own profile" ON public.profiles
  FOR UPDATE USING (auth.uid() = id);

CREATE POLICY "Admins can view all profiles" ON public.profiles
  FOR SELECT USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role = 'super_admin'
    )
  );

CREATE POLICY "Admins can update all profiles" ON public.profiles
  FOR UPDATE USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role = 'super_admin'
    )
  );

-- Function to handle new user signup
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER AS $$
BEGIN
  INSERT INTO public.profiles (id, email, full_name)
  VALUES (NEW.id, NEW.email, NEW.raw_user_meta_data->>'full_name');
  RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Trigger to create profile on signup
CREATE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();

-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION public.update_updated_at()
RETURNS TRIGGER AS $$
BEGIN
  NEW.updated_at = NOW();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger for profiles updated_at
CREATE TRIGGER profiles_updated_at
  BEFORE UPDATE ON public.profiles
  FOR EACH ROW EXECUTE FUNCTION public.update_updated_at();
```

### Step 2.2: Create Login Page

Create `src/app/(auth)/login/page.tsx`:

```typescript
import { LoginForm } from "@/components/forms/login-form"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"

export default function LoginPage() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-saddle-100 to-parchment p-4">
      <Card className="w-full max-w-md border-saddle-300 shadow-xl">
        <CardHeader className="text-center">
          <div className="mx-auto mb-4 text-6xl">🤠</div>
          <CardTitle className="text-2xl font-bold text-saddle-700">
            Black Bart's Gold
          </CardTitle>
          <CardDescription className="text-leather-light">
            Admin Dashboard - Sign in to continue
          </CardDescription>
        </CardHeader>
        <CardContent>
          <LoginForm />
        </CardContent>
      </Card>
    </div>
  )
}
```

Create `src/components/forms/login-form.tsx`:

```typescript
"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { zodResolver } from "@hookform/resolvers/zod"
import { useForm } from "react-hook-form"
import * as z from "zod"
import { createClient } from "@/lib/supabase/client"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useToast } from "@/components/ui/use-toast"

const loginSchema = z.object({
  email: z.string().email("Please enter a valid email"),
  password: z.string().min(8, "Password must be at least 8 characters"),
})

type LoginFormData = z.infer<typeof loginSchema>

export function LoginForm() {
  const [isLoading, setIsLoading] = useState(false)
  const router = useRouter()
  const { toast } = useToast()
  const supabase = createClient()

  const form = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: "",
      password: "",
    },
  })

  async function onSubmit(data: LoginFormData) {
    setIsLoading(true)

    const { error } = await supabase.auth.signInWithPassword({
      email: data.email,
      password: data.password,
    })

    if (error) {
      toast({
        title: "Error signing in",
        description: error.message,
        variant: "destructive",
      })
      setIsLoading(false)
      return
    }

    toast({
      title: "Welcome back, partner! 🤠",
      description: "Successfully signed in.",
    })

    router.push("/")
    router.refresh()
  }

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="email">Email</Label>
        <Input
          id="email"
          type="email"
          placeholder="partner@blackbartsgold.com"
          {...form.register("email")}
          className="border-saddle-300 focus:border-gold focus:ring-gold"
        />
        {form.formState.errors.email && (
          <p className="text-sm text-fire">{form.formState.errors.email.message}</p>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="password">Password</Label>
        <Input
          id="password"
          type="password"
          placeholder="••••••••"
          {...form.register("password")}
          className="border-saddle-300 focus:border-gold focus:ring-gold"
        />
        {form.formState.errors.password && (
          <p className="text-sm text-fire">{form.formState.errors.password.message}</p>
        )}
      </div>

      <Button
        type="submit"
        className="w-full bg-gold hover:bg-gold-600 text-leather font-semibold"
        disabled={isLoading}
      >
        {isLoading ? "Signing in..." : "Sign In"}
      </Button>
    </form>
  )
}
```

### Step 2.3: Create Dashboard Layout with Sidebar

Create `src/app/(dashboard)/layout.tsx`:

```typescript
import { redirect } from "next/navigation"
import { createClient } from "@/lib/supabase/server"
import { DashboardSidebar } from "@/components/layout/dashboard-sidebar"
import { DashboardHeader } from "@/components/layout/dashboard-header"

export default async function DashboardLayout({
  children,
}: {
  children: React.ReactNode
}) {
  const supabase = await createClient()
  const { data: { user } } = await supabase.auth.getUser()

  if (!user) {
    redirect("/login")
  }

  // Get user profile with role
  const { data: profile } = await supabase
    .from("profiles")
    .select("*")
    .eq("id", user.id)
    .single()

  return (
    <div className="min-h-screen bg-parchment-light">
      <DashboardSidebar user={profile} />
      <div className="lg:pl-64">
        <DashboardHeader user={profile} />
        <main className="p-6">
          {children}
        </main>
      </div>
    </div>
  )
}
```

Create `src/components/layout/dashboard-sidebar.tsx`:

```typescript
"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"
import { cn } from "@/lib/utils"
import { UserProfile } from "@/types/database"
import {
  LayoutDashboard,
  Users,
  Coins,
  DollarSign,
  Building2,
  Shield,
  Settings,
} from "lucide-react"

interface DashboardSidebarProps {
  user: UserProfile | null
}

const navigation = [
  { name: "Dashboard", href: "/", icon: LayoutDashboard },
  { name: "Users", href: "/users", icon: Users },
  { name: "Coins", href: "/coins", icon: Coins },
  { name: "Finances", href: "/finances", icon: DollarSign },
  { name: "Sponsors", href: "/sponsors", icon: Building2 },
  { name: "Security", href: "/security", icon: Shield },
  { name: "Settings", href: "/settings", icon: Settings },
]

export function DashboardSidebar({ user }: DashboardSidebarProps) {
  const pathname = usePathname()

  return (
    <div className="hidden lg:fixed lg:inset-y-0 lg:z-50 lg:flex lg:w-64 lg:flex-col">
      <div className="flex grow flex-col gap-y-5 overflow-y-auto bg-saddle-700 px-6 pb-4">
        {/* Logo */}
        <div className="flex h-16 shrink-0 items-center gap-2">
          <span className="text-3xl">🤠</span>
          <span className="text-gold font-bold text-lg">BB Admin</span>
        </div>

        {/* Navigation */}
        <nav className="flex flex-1 flex-col">
          <ul role="list" className="flex flex-1 flex-col gap-y-7">
            <li>
              <ul role="list" className="-mx-2 space-y-1">
                {navigation.map((item) => {
                  const isActive = pathname === item.href || 
                    (item.href !== "/" && pathname.startsWith(item.href))
                  
                  return (
                    <li key={item.name}>
                      <Link
                        href={item.href}
                        className={cn(
                          "group flex gap-x-3 rounded-md p-2 text-sm font-semibold leading-6 transition-colors",
                          isActive
                            ? "bg-gold text-leather"
                            : "text-parchment hover:bg-saddle-600 hover:text-gold"
                        )}
                      >
                        <item.icon className="h-6 w-6 shrink-0" />
                        {item.name}
                      </Link>
                    </li>
                  )
                })}
              </ul>
            </li>

            {/* User info at bottom */}
            <li className="mt-auto">
              <div className="flex items-center gap-x-4 px-2 py-3 text-sm font-semibold text-parchment">
                <div className="h-8 w-8 rounded-full bg-gold flex items-center justify-center text-leather">
                  {user?.full_name?.[0] || user?.email?.[0] || "?"}
                </div>
                <div className="flex-1 truncate">
                  <p className="truncate">{user?.full_name || "Partner"}</p>
                  <p className="text-xs text-parchment/60 capitalize">{user?.role}</p>
                </div>
              </div>
            </li>
          </ul>
        </nav>
      </div>
    </div>
  )
}
```

Create `src/components/layout/dashboard-header.tsx`:

```typescript
"use client"

import { useRouter } from "next/navigation"
import { createClient } from "@/lib/supabase/client"
import { UserProfile } from "@/types/database"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { LogOut, Settings, User } from "lucide-react"

interface DashboardHeaderProps {
  user: UserProfile | null
}

export function DashboardHeader({ user }: DashboardHeaderProps) {
  const router = useRouter()
  const supabase = createClient()

  async function handleSignOut() {
    await supabase.auth.signOut()
    router.push("/login")
    router.refresh()
  }

  return (
    <header className="sticky top-0 z-40 flex h-16 shrink-0 items-center gap-x-4 border-b border-saddle-200 bg-parchment px-4 shadow-sm sm:gap-x-6 sm:px-6 lg:px-8">
      <div className="flex flex-1 gap-x-4 self-stretch lg:gap-x-6">
        <div className="flex flex-1 items-center">
          <h1 className="text-xl font-semibold text-saddle-700">
            Welcome back, {user?.full_name?.split(" ")[0] || "Partner"}! 🤠
          </h1>
        </div>

        <div className="flex items-center gap-x-4 lg:gap-x-6">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" className="relative h-10 w-10 rounded-full">
                <Avatar className="h-10 w-10 border-2 border-gold">
                  <AvatarImage src={user?.avatar_url || undefined} />
                  <AvatarFallback className="bg-gold text-leather">
                    {user?.full_name?.[0] || user?.email?.[0] || "?"}
                  </AvatarFallback>
                </Avatar>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent className="w-56" align="end">
              <DropdownMenuLabel>
                <div className="flex flex-col space-y-1">
                  <p className="text-sm font-medium">{user?.full_name || "Partner"}</p>
                  <p className="text-xs text-muted-foreground">{user?.email}</p>
                </div>
              </DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem>
                <User className="mr-2 h-4 w-4" />
                Profile
              </DropdownMenuItem>
              <DropdownMenuItem>
                <Settings className="mr-2 h-4 w-4" />
                Settings
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={handleSignOut} className="text-fire">
                <LogOut className="mr-2 h-4 w-4" />
                Sign out
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>
    </header>
  )
}
```

### Step 2.4: Create Dashboard Home Page

Create `src/app/(dashboard)/page.tsx`:

```typescript
import { createClient } from "@/lib/supabase/server"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Users, Coins, DollarSign, TrendingUp } from "lucide-react"

export default async function DashboardPage() {
  const supabase = await createClient()

  // Fetch basic stats (we'll expand this later)
  const { count: userCount } = await supabase
    .from("profiles")
    .select("*", { count: "exact", head: true })

  const stats = [
    {
      name: "Total Users",
      value: userCount || 0,
      icon: Users,
      change: "+12%",
      changeType: "positive",
    },
    {
      name: "Active Coins",
      value: "—",
      icon: Coins,
      change: "Coming soon",
      changeType: "neutral",
    },
    {
      name: "Total Deposits",
      value: "—",
      icon: DollarSign,
      change: "Coming soon",
      changeType: "neutral",
    },
    {
      name: "Daily Revenue",
      value: "—",
      icon: TrendingUp,
      change: "Coming soon",
      changeType: "neutral",
    },
  ]

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-saddle-700">Dashboard</h2>
        <p className="text-leather-light">
          Overview of Black Bart's Gold treasure hunting operations
        </p>
      </div>

      {/* Stats Grid */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {stats.map((stat) => (
          <Card key={stat.name} className="border-saddle-200">
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium text-leather-light">
                {stat.name}
              </CardTitle>
              <stat.icon className="h-4 w-4 text-saddle-500" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-saddle-700">{stat.value}</div>
              <p className={`text-xs ${
                stat.changeType === "positive" ? "text-green-600" :
                stat.changeType === "negative" ? "text-fire" :
                "text-leather-light"
              }`}>
                {stat.change}
              </p>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Placeholder for charts and more content */}
      <div className="grid gap-4 md:grid-cols-2">
        <Card className="border-saddle-200">
          <CardHeader>
            <CardTitle className="text-saddle-700">Recent Activity</CardTitle>
            <CardDescription>Latest treasure hunting activity</CardDescription>
          </CardHeader>
          <CardContent>
            <p className="text-leather-light text-sm">
              Activity feed will be implemented in Phase 3.
            </p>
          </CardContent>
        </Card>

        <Card className="border-saddle-200">
          <CardHeader>
            <CardTitle className="text-saddle-700">Quick Actions</CardTitle>
            <CardDescription>Common administrative tasks</CardDescription>
          </CardHeader>
          <CardContent className="space-y-2">
            <p className="text-leather-light text-sm">
              Quick actions will be implemented in Phase 3.
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
```

### Step 2.5: Users Management Page

Create `src/app/(dashboard)/users/page.tsx`:

```typescript
import { createClient } from "@/lib/supabase/server"
import { UsersTable } from "@/components/dashboard/users-table"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"

export default async function UsersPage() {
  const supabase = await createClient()

  const { data: users, error } = await supabase
    .from("profiles")
    .select("*")
    .order("created_at", { ascending: false })

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-saddle-700">User Management</h2>
        <p className="text-leather-light">
          Manage users, roles, and permissions
        </p>
      </div>

      <Card className="border-saddle-200">
        <CardHeader>
          <CardTitle className="text-saddle-700">All Users</CardTitle>
          <CardDescription>
            {users?.length || 0} total users in the system
          </CardDescription>
        </CardHeader>
        <CardContent>
          <UsersTable users={users || []} />
        </CardContent>
      </Card>
    </div>
  )
}
```

Create `src/components/dashboard/users-table.tsx`:

```typescript
"use client"

import { UserProfile } from "@/types/database"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Badge } from "@/components/ui/badge"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { formatDistanceToNow } from "date-fns"

interface UsersTableProps {
  users: UserProfile[]
}

const roleColors = {
  super_admin: "bg-gold text-leather",
  sponsor_admin: "bg-brass text-white",
  user: "bg-saddle-200 text-saddle-700",
}

export function UsersTable({ users }: UsersTableProps) {
  if (users.length === 0) {
    return (
      <div className="text-center py-8 text-leather-light">
        No users found. Users will appear here once they sign up.
      </div>
    )
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>User</TableHead>
          <TableHead>Role</TableHead>
          <TableHead>Joined</TableHead>
          <TableHead className="text-right">Actions</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {users.map((user) => (
          <TableRow key={user.id}>
            <TableCell>
              <div className="flex items-center gap-3">
                <Avatar className="h-8 w-8">
                  <AvatarImage src={user.avatar_url || undefined} />
                  <AvatarFallback className="bg-gold text-leather text-xs">
                    {user.full_name?.[0] || user.email[0]}
                  </AvatarFallback>
                </Avatar>
                <div>
                  <p className="font-medium text-saddle-700">
                    {user.full_name || "No name"}
                  </p>
                  <p className="text-sm text-leather-light">{user.email}</p>
                </div>
              </div>
            </TableCell>
            <TableCell>
              <Badge className={roleColors[user.role]}>
                {user.role.replace("_", " ")}
              </Badge>
            </TableCell>
            <TableCell className="text-leather-light">
              {formatDistanceToNow(new Date(user.created_at), { addSuffix: true })}
            </TableCell>
            <TableCell className="text-right">
              <span className="text-sm text-leather-light">Edit (coming soon)</span>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
```

---

## 💰 Phase 3: Coin Management

*(To be implemented after Phase 2 is complete)*

### Overview

- View all coins (with filters: status, tier, age, value)
- Map visualization of coin locations
- Create/edit/delete coins
- Bulk operations (retrieve stale coins, relocate)
- Mythical coin management

### Database Schema (run in Supabase SQL Editor)

```sql
-- Coins table
CREATE TABLE public.coins (
  id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
  coin_type TEXT DEFAULT 'fixed' CHECK (coin_type IN ('fixed', 'pool')),
  value DECIMAL(10, 2) NOT NULL,
  latitude DOUBLE PRECISION NOT NULL,
  longitude DOUBLE PRECISION NOT NULL,
  hider_id UUID REFERENCES public.profiles(id),
  hidden_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  collected_by UUID REFERENCES public.profiles(id),
  collected_at TIMESTAMP WITH TIME ZONE,
  status TEXT DEFAULT 'hidden' CHECK (status IN ('hidden', 'visible', 'collected', 'expired')),
  tier TEXT DEFAULT 'gold' CHECK (tier IN ('gold', 'silver', 'bronze')),
  is_mythical BOOLEAN DEFAULT FALSE,
  sponsor_id UUID REFERENCES public.sponsors(id),
  logo_front_url TEXT,
  logo_back_url TEXT,
  hunt_type TEXT DEFAULT 'standard',
  multi_find BOOLEAN DEFAULT FALSE,
  finds_remaining INTEGER DEFAULT 1,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Enable RLS
ALTER TABLE public.coins ENABLE ROW LEVEL SECURITY;

-- Admins can do everything with coins
CREATE POLICY "Admins can manage coins" ON public.coins
  FOR ALL USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role IN ('super_admin', 'sponsor_admin')
    )
  );

-- Index for geo queries
CREATE INDEX coins_location_idx ON public.coins (latitude, longitude);
CREATE INDEX coins_status_idx ON public.coins (status);
CREATE INDEX coins_hidden_at_idx ON public.coins (hidden_at);
```

---

## 📊 Phase 4: Financial Dashboard

*(To be implemented after Phase 3 is complete)*

### Overview

- Total deposits, payouts, profit margins
- Gas revenue tracking
- Transaction history
- Integrity checks (deposits vs coins in system)
- Charts and visualizations

### Database Schema

```sql
-- Transactions table
CREATE TABLE public.transactions (
  id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
  user_id UUID REFERENCES public.profiles(id) NOT NULL,
  transaction_type TEXT NOT NULL CHECK (
    transaction_type IN ('found', 'hidden', 'gas_consumed', 'purchased', 'transfer', 'payout')
  ),
  amount DECIMAL(10, 2) NOT NULL,
  coin_id UUID REFERENCES public.coins(id),
  status TEXT DEFAULT 'pending' CHECK (status IN ('pending', 'confirmed', 'failed')),
  metadata JSONB,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  confirmed_at TIMESTAMP WITH TIME ZONE
);

-- Enable RLS
ALTER TABLE public.transactions ENABLE ROW LEVEL SECURITY;

-- Admins can view all transactions
CREATE POLICY "Admins can view transactions" ON public.transactions
  FOR SELECT USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role = 'super_admin'
    )
  );

-- Index for reporting
CREATE INDEX transactions_user_id_idx ON public.transactions (user_id);
CREATE INDEX transactions_type_idx ON public.transactions (transaction_type);
CREATE INDEX transactions_created_at_idx ON public.transactions (created_at);
```

---

## 🏢 Phase 5: Sponsor Management

*(To be implemented after Phase 4 is complete)*

### Overview

- Sponsor profiles and onboarding
- Sponsor coin tracking
- Analytics per sponsor
- Logo management

---

## 🔒 Phase 6: Security & Monitoring

*(To be implemented after Phase 5 is complete)*

### Overview

- Cheater detection alerts
- Velocity anomaly tracking
- User suspension/ban tools
- Audit logs

---

## 🚀 Phase 7: Deployment

### Step 7.1: Deploy to Vercel

1. Push code to GitHub
2. Go to [vercel.com](https://vercel.com)
3. Import your repository
4. Add environment variables:
   - `NEXT_PUBLIC_SUPABASE_URL`
   - `NEXT_PUBLIC_SUPABASE_ANON_KEY`
   - `SUPABASE_SERVICE_ROLE_KEY`
5. Deploy!

### Step 7.2: Configure Custom Domain (Optional)

1. In Vercel, go to project Settings → Domains
2. Add your custom domain
3. Update DNS records as instructed

### Step 7.3: Set Up Preview Deployments

Vercel automatically creates preview deployments for pull requests - great for testing changes before merging!

---

## 📚 Reference Documents

> **🤠 IMPORTANT**: Read **brand-guide.md** before any UI work!

| Document | Use For |
|----------|---------|
| [brand-guide.md](./brand-guide.md) | 🤠 **READ FIRST** - Colors, theme, character |
| [admin-dashboard.md](./admin-dashboard.md) | Feature requirements |
| [project-vision.md](./project-vision.md) | Overall project context |
| [economy-and-currency.md](./economy-and-currency.md) | BBG, gas, find limits |
| [user-accounts-security.md](./user-accounts-security.md) | Auth, roles, anti-cheat |

---

## 🧪 Testing with Browser Tools

The AI assistant has browser tools to test the dashboard as we build:

1. Navigate to pages
2. Take snapshots of interactive elements
3. Fill forms and click buttons
4. Verify layouts and styling
5. Check for errors in console

This enables rapid iteration - build, test, refine!

---

## 📝 Version History

| Date | Changes |
|------|---------|
| Jan 21, 2026 | Initial admin dashboard build guide created |

---

**Ready to build? Start with Phase 0!** 🤠
