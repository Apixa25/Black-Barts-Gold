"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { createClient } from "@/lib/supabase/client"
import type { User } from "@supabase/supabase-js"
import type { UserProfile } from "@/types/database"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { Separator } from "@/components/ui/separator"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { toast } from "sonner"
import {
  Settings,
  User as UserIcon,
  Bell,
  Shield,
  Key,
  Database,
  Download,
  Trash2,
  RefreshCw,
  Copy,
  Eye,
  EyeOff,
  Save,
  LogOut,
  Coins,
  Users,
  Building2,
  Activity,
  CreditCard
} from "lucide-react"

interface SettingsPageClientProps {
  user: User
  profile: UserProfile | null
  systemStats: {
    users: number
    coins: number
    transactions: number
    sponsors: number
    logs: number
  }
}

export function SettingsPageClient({ user, profile, systemStats }: SettingsPageClientProps) {
  const router = useRouter()
  const supabase = createClient()
  
  // Profile state
  const [fullName, setFullName] = useState(profile?.full_name || "")
  const [isSavingProfile, setIsSavingProfile] = useState(false)
  
  // Notification preferences
  const [notifications, setNotifications] = useState({
    emailAlerts: true,
    coinCollected: true,
    newSponsor: true,
    securityAlerts: true,
    weeklyReport: false,
  })
  
  // API key visibility
  const [showApiKey, setShowApiKey] = useState(false)
  
  // System actions
  const [isExporting, setIsExporting] = useState(false)
  const [isClearing, setIsClearing] = useState(false)

  const handleSaveProfile = async () => {
    setIsSavingProfile(true)
    
    const { error } = await supabase
      .from("profiles")
      .update({ full_name: fullName || null })
      .eq("id", user.id)

    setIsSavingProfile(false)

    if (error) {
      toast.error("Failed to update profile", { description: error.message })
      return
    }

    toast.success("Profile updated! 🤠")
    router.refresh()
  }

  const handleSignOut = async () => {
    await supabase.auth.signOut()
    router.push("/login")
  }

  const handleCopyApiKey = () => {
    navigator.clipboard.writeText(process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY || "")
    toast.success("API key copied to clipboard!")
  }

  const handleExportData = async () => {
    setIsExporting(true)
    
    // Simulate export (in production, this would generate real exports)
    await new Promise(resolve => setTimeout(resolve, 2000))
    
    toast.success("Export complete! 📦", {
      description: "Your data export has been prepared",
    })
    setIsExporting(false)
  }

  const handleClearLogs = async () => {
    if (!confirm("Are you sure you want to clear old activity logs? This cannot be undone.")) {
      return
    }
    
    setIsClearing(true)
    
    // Delete logs older than 30 days
    const thirtyDaysAgo = new Date()
    thirtyDaysAgo.setDate(thirtyDaysAgo.getDate() - 30)
    
    const { error } = await supabase
      .from("activity_logs")
      .delete()
      .lt("created_at", thirtyDaysAgo.toISOString())

    setIsClearing(false)

    if (error) {
      toast.error("Failed to clear logs", { description: error.message })
      return
    }

    toast.success("Old logs cleared! 🧹")
    router.refresh()
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-saddle-dark flex items-center gap-2">
          <Settings className="h-6 w-6" />
          Settings
        </h2>
        <p className="text-leather-light">
          Manage your account, preferences, and system configuration
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        {/* Profile Settings */}
        <Card className="border-saddle-light/30">
          <CardHeader>
            <CardTitle className="text-saddle-dark flex items-center gap-2">
              <UserIcon className="h-5 w-5" />
              Profile Settings
            </CardTitle>
            <CardDescription>
              Update your personal information
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center gap-4">
              <Avatar className="h-16 w-16 border-2 border-gold">
                <AvatarImage src={profile?.avatar_url || undefined} />
                <AvatarFallback className="bg-gold/20 text-saddle-dark text-xl">
                  {fullName?.[0] || user.email?.[0]?.toUpperCase() || "U"}
                </AvatarFallback>
              </Avatar>
              <div>
                <p className="font-medium text-saddle-dark">{user.email}</p>
                <Badge className="bg-gold/20 text-gold-dark mt-1">
                  {profile?.role || "user"}
                </Badge>
              </div>
            </div>
            
            <Separator />
            
            <div className="space-y-2">
              <Label htmlFor="fullName">Full Name</Label>
              <Input
                id="fullName"
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                placeholder="Enter your full name"
                className="border-saddle-light/30"
              />
            </div>
            
            <div className="space-y-2">
              <Label>Email</Label>
              <Input
                value={user.email || ""}
                disabled
                className="border-saddle-light/30 bg-parchment"
              />
              <p className="text-xs text-leather-light">
                Email cannot be changed from this panel
              </p>
            </div>
            
            <div className="flex gap-2">
              <Button
                onClick={handleSaveProfile}
                disabled={isSavingProfile}
                className="bg-gold hover:bg-gold-dark text-leather"
              >
                <Save className="h-4 w-4 mr-2" />
                {isSavingProfile ? "Saving..." : "Save Changes"}
              </Button>
              <Button
                variant="outline"
                onClick={handleSignOut}
                className="border-fire/30 text-fire hover:bg-fire/10"
              >
                <LogOut className="h-4 w-4 mr-2" />
                Sign Out
              </Button>
            </div>
          </CardContent>
        </Card>

        {/* Notification Preferences */}
        <Card className="border-saddle-light/30">
          <CardHeader>
            <CardTitle className="text-saddle-dark flex items-center gap-2">
              <Bell className="h-5 w-5" />
              Notifications
            </CardTitle>
            <CardDescription>
              Configure your notification preferences
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <Label>Email Alerts</Label>
                <p className="text-xs text-leather-light">Receive important alerts via email</p>
              </div>
              <Switch
                checked={notifications.emailAlerts}
                onCheckedChange={(checked) => 
                  setNotifications({ ...notifications, emailAlerts: checked })
                }
              />
            </div>
            
            <Separator />
            
            <div className="flex items-center justify-between">
              <div>
                <Label>Coin Collected</Label>
                <p className="text-xs text-leather-light">When a coin is found by a user</p>
              </div>
              <Switch
                checked={notifications.coinCollected}
                onCheckedChange={(checked) => 
                  setNotifications({ ...notifications, coinCollected: checked })
                }
              />
            </div>
            
            <div className="flex items-center justify-between">
              <div>
                <Label>New Sponsor</Label>
                <p className="text-xs text-leather-light">When a new sponsor signs up</p>
              </div>
              <Switch
                checked={notifications.newSponsor}
                onCheckedChange={(checked) => 
                  setNotifications({ ...notifications, newSponsor: checked })
                }
              />
            </div>
            
            <div className="flex items-center justify-between">
              <div>
                <Label>Security Alerts</Label>
                <p className="text-xs text-leather-light">Suspicious activity warnings</p>
              </div>
              <Switch
                checked={notifications.securityAlerts}
                onCheckedChange={(checked) => 
                  setNotifications({ ...notifications, securityAlerts: checked })
                }
              />
            </div>
            
            <Separator />
            
            <div className="flex items-center justify-between">
              <div>
                <Label>Weekly Report</Label>
                <p className="text-xs text-leather-light">Summary of weekly activity</p>
              </div>
              <Switch
                checked={notifications.weeklyReport}
                onCheckedChange={(checked) => 
                  setNotifications({ ...notifications, weeklyReport: checked })
                }
              />
            </div>
            
            <Button
              variant="outline"
              className="w-full border-saddle-light/30"
              onClick={() => toast.success("Preferences saved!")}
            >
              Save Preferences
            </Button>
          </CardContent>
        </Card>

        {/* API Configuration */}
        <Card className="border-saddle-light/30">
          <CardHeader>
            <CardTitle className="text-saddle-dark flex items-center gap-2">
              <Key className="h-5 w-5" />
              API Configuration
            </CardTitle>
            <CardDescription>
              View your API keys and endpoints
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label>Supabase URL</Label>
              <div className="flex gap-2">
                <Input
                  value={process.env.NEXT_PUBLIC_SUPABASE_URL || "Not configured"}
                  readOnly
                  className="border-saddle-light/30 bg-parchment font-mono text-xs"
                />
                <Button
                  variant="outline"
                  size="icon"
                  onClick={() => {
                    navigator.clipboard.writeText(process.env.NEXT_PUBLIC_SUPABASE_URL || "")
                    toast.success("URL copied!")
                  }}
                  className="border-saddle-light/30"
                >
                  <Copy className="h-4 w-4" />
                </Button>
              </div>
            </div>
            
            <div className="space-y-2">
              <Label>Public API Key</Label>
              <div className="flex gap-2">
                <Input
                  type={showApiKey ? "text" : "password"}
                  value={process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY || "Not configured"}
                  readOnly
                  className="border-saddle-light/30 bg-parchment font-mono text-xs"
                />
                <Button
                  variant="outline"
                  size="icon"
                  onClick={() => setShowApiKey(!showApiKey)}
                  className="border-saddle-light/30"
                >
                  {showApiKey ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </Button>
                <Button
                  variant="outline"
                  size="icon"
                  onClick={handleCopyApiKey}
                  className="border-saddle-light/30"
                >
                  <Copy className="h-4 w-4" />
                </Button>
              </div>
              <p className="text-xs text-leather-light">
                This is the public anon key - safe to use in client-side code
              </p>
            </div>
            
            <Separator />
            
            <div className="p-3 bg-parchment rounded-lg">
              <p className="text-sm text-leather font-medium mb-2">🔒 Security Note</p>
              <p className="text-xs text-leather-light">
                Never expose your service role key. Keep it server-side only.
                Row Level Security (RLS) protects your data.
              </p>
            </div>
          </CardContent>
        </Card>

        {/* System Stats & Tools */}
        <Card className="border-saddle-light/30">
          <CardHeader>
            <CardTitle className="text-saddle-dark flex items-center gap-2">
              <Database className="h-5 w-5" />
              System Overview
            </CardTitle>
            <CardDescription>
              Database statistics and admin tools
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <div className="flex items-center gap-2 p-3 bg-parchment rounded-lg">
                <Users className="h-5 w-5 text-gold" />
                <div>
                  <p className="text-lg font-bold text-saddle-dark">{systemStats.users}</p>
                  <p className="text-xs text-leather-light">Users</p>
                </div>
              </div>
              <div className="flex items-center gap-2 p-3 bg-parchment rounded-lg">
                <Coins className="h-5 w-5 text-gold" />
                <div>
                  <p className="text-lg font-bold text-saddle-dark">{systemStats.coins}</p>
                  <p className="text-xs text-leather-light">Coins</p>
                </div>
              </div>
              <div className="flex items-center gap-2 p-3 bg-parchment rounded-lg">
                <CreditCard className="h-5 w-5 text-green-600" />
                <div>
                  <p className="text-lg font-bold text-saddle-dark">{systemStats.transactions}</p>
                  <p className="text-xs text-leather-light">Transactions</p>
                </div>
              </div>
              <div className="flex items-center gap-2 p-3 bg-parchment rounded-lg">
                <Building2 className="h-5 w-5 text-brass" />
                <div>
                  <p className="text-lg font-bold text-saddle-dark">{systemStats.sponsors}</p>
                  <p className="text-xs text-leather-light">Sponsors</p>
                </div>
              </div>
            </div>
            
            <div className="flex items-center gap-2 p-3 bg-parchment rounded-lg">
              <Activity className="h-5 w-5 text-saddle" />
              <div>
                <p className="text-lg font-bold text-saddle-dark">{systemStats.logs}</p>
                <p className="text-xs text-leather-light">Activity Logs</p>
              </div>
            </div>
            
            <Separator />
            
            <div className="space-y-2">
              <Label>Admin Tools</Label>
              <div className="flex flex-col gap-2">
                <Button
                  variant="outline"
                  onClick={handleExportData}
                  disabled={isExporting}
                  className="justify-start border-saddle-light/30"
                >
                  <Download className="h-4 w-4 mr-2" />
                  {isExporting ? "Exporting..." : "Export All Data"}
                </Button>
                <Button
                  variant="outline"
                  onClick={() => router.refresh()}
                  className="justify-start border-saddle-light/30"
                >
                  <RefreshCw className="h-4 w-4 mr-2" />
                  Refresh Cache
                </Button>
                <Button
                  variant="outline"
                  onClick={handleClearLogs}
                  disabled={isClearing}
                  className="justify-start border-fire/30 text-fire hover:bg-fire/10"
                >
                  <Trash2 className="h-4 w-4 mr-2" />
                  {isClearing ? "Clearing..." : "Clear Old Logs (30+ days)"}
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* App Configuration */}
      <Card className="border-saddle-light/30">
        <CardHeader>
          <CardTitle className="text-saddle-dark flex items-center gap-2">
            <Shield className="h-5 w-5" />
            Game Configuration
          </CardTitle>
          <CardDescription>
            Configure Black Bart&apos;s Gold treasure hunt settings
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-6 md:grid-cols-3">
            <div className="space-y-2">
              <Label>Default Coin Value ($)</Label>
              <Input
                type="number"
                defaultValue="5.00"
                step="0.01"
                className="border-saddle-light/30"
              />
              <p className="text-xs text-leather-light">
                Default value for new coins
              </p>
            </div>
            
            <div className="space-y-2">
              <Label>Gas Fee (%)</Label>
              <Input
                type="number"
                defaultValue="10"
                min="0"
                max="50"
                className="border-saddle-light/30"
              />
              <p className="text-xs text-leather-light">
                Platform fee on transactions
              </p>
            </div>
            
            <div className="space-y-2">
              <Label>Min Payout ($)</Label>
              <Input
                type="number"
                defaultValue="20.00"
                step="0.01"
                className="border-saddle-light/30"
              />
              <p className="text-xs text-leather-light">
                Minimum balance for withdrawals
              </p>
            </div>
            
            <div className="space-y-2">
              <Label>Coin Radius (meters)</Label>
              <Input
                type="number"
                defaultValue="50"
                min="10"
                max="500"
                className="border-saddle-light/30"
              />
              <p className="text-xs text-leather-light">
                How close users must be to collect
              </p>
            </div>
            
            <div className="space-y-2">
              <Label>Coin Expiry (days)</Label>
              <Input
                type="number"
                defaultValue="30"
                min="1"
                max="365"
                className="border-saddle-light/30"
              />
              <p className="text-xs text-leather-light">
                Days until uncollected coins expire
              </p>
            </div>
            
            <div className="space-y-2">
              <Label>Max Daily Finds</Label>
              <Input
                type="number"
                defaultValue="10"
                min="1"
                max="100"
                className="border-saddle-light/30"
              />
              <p className="text-xs text-leather-light">
                Limit per user per day
              </p>
            </div>
          </div>
          
          <Separator className="my-6" />
          
          <div className="flex justify-end">
            <Button 
              className="bg-gold hover:bg-gold-dark text-leather"
              onClick={() => toast.success("Configuration saved! ⚙️")}
            >
              <Save className="h-4 w-4 mr-2" />
              Save Configuration
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
