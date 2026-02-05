"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"
import { cn } from "@/lib/utils"
import type { UserProfile } from "@/types/database"
import {
  LayoutDashboard,
  Users,
  Coins,
  DollarSign,
  Building2,
  Shield,
  Settings,
  MapPinned,
  Navigation,
} from "lucide-react"

interface DashboardSidebarProps {
  user: UserProfile | null
}

const navigation = [
  { name: "Dashboard", href: "/", icon: LayoutDashboard },
  { name: "Live Players", href: "/players", icon: Navigation },
  { name: "Users", href: "/users", icon: Users },
  { name: "Coins", href: "/coins", icon: Coins },
  { name: "Zones", href: "/zones", icon: MapPinned },
  { name: "Finances", href: "/finances", icon: DollarSign },
  { name: "Sponsors", href: "/sponsors", icon: Building2 },
  { name: "Security", href: "/security", icon: Shield },
  { name: "Settings", href: "/settings", icon: Settings },
]

export function DashboardSidebar({ user }: DashboardSidebarProps) {
  const pathname = usePathname()

  return (
    <div className="hidden lg:fixed lg:inset-y-0 lg:z-50 lg:flex lg:w-64 lg:flex-col">
      <div className="flex grow flex-col gap-y-5 overflow-y-auto bg-saddle-dark px-6 pb-4">
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
                            : "text-parchment hover:bg-saddle hover:text-gold"
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
                  <p className="text-xs text-parchment/60 capitalize">{user?.role?.replace("_", " ")}</p>
                </div>
              </div>
            </li>
          </ul>
        </nav>
      </div>
    </div>
  )
}
